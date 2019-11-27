// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

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
        private List<BuildCheckResult> _results = new List<BuildCheckResult>();
        private string _outputFilePath;

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

            _outputFilePath = outputFilePath;
        }

        public void Initialize()
        {
        }

        public void ReportProjectAnalysisResult(BuildCheckResult result)
        {
            _results.Add(result);
        }

        public void TearDown()
        {
            string json = JsonConvert.SerializeObject(_results.ToArray());
            File.WriteAllText(_outputFilePath, json);
        }
    }
}
