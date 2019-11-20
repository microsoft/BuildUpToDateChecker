// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.IO;

namespace BuildUpToDateChecker
{
    public sealed class BuildCheckResult
    {
        public string FullProjectPath { get; set; }
        public bool IsUpToDate { get; set; }
        public DateTime ScanStart { get; set; }
        public TimeSpan ScanDuration { get; set; }
        public string FailureMessage { get; set; }
    }

    public interface IResultsReporter
    {
        void Initialize();
        void ReportProjectAnalysisResult(BuildCheckResult result);
        void TearDown();
    }

    internal class ResultsReporter : IResultsReporter
    {
        private readonly StreamWriter _sw;

        public ResultsReporter(string outputFilePath)
        {
            if (outputFilePath == null)
            {
                throw new ArgumentNullException(nameof(outputFilePath));
            }

            outputFilePath = Path.GetFullPath(outputFilePath); // Convert relative to absolute, if necessary.

            string outputFileDir = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputFileDir))
            {
                Directory.CreateDirectory(outputFileDir);
            }

            // Delete any previous file.
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            _sw = File.CreateText(outputFilePath);
        }

        public void Initialize()
        {
            // Start it off:
            _sw.WriteLine("{[");
        }

        public void ReportProjectAnalysisResult(BuildCheckResult result)
        {
            // Just going to do quick and dirty JSON creation. In the future (if needed) we can look into perf improvements with queues/async/etc.
            _sw.WriteLine($"{{ \"Path\":\"{result.FullProjectPath}\", \"UpToDate\":{(result.IsUpToDate ? "true" : "false" )}, \"AnalysisStartTime\":\"{result.ScanStart:O}\", \"AnalysisEndTime\":\"{result.ScanDuration:G}\", \"FailureMessage\":\"{result.FailureMessage.Replace("\"", "\\\"")}\" }},");
        }

        public void TearDown()
        {
            _sw.WriteLine("]}");
            _sw.Flush();
            _sw.Close();
        }
    }
}
