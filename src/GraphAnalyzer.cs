// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Graph;

namespace BuildUpToDateChecker
{
    internal interface IGraphAnalyzer
    {
        bool AnalyzeGraph(string rootProject);
    }

    internal class GraphAnalyzer : IGraphAnalyzer
    {
        private readonly ILogger _logger;
        private readonly IProjectAnalyzer _projectAnalyzer;
        private readonly bool _failFast;
        private readonly IResultsReporter _resultsReporter;

        public GraphAnalyzer(ILogger logger, IProjectAnalyzer projectAnalyzer, IResultsReporter resultsReporter, bool failFast)
        {
             _logger = logger ?? throw new ArgumentNullException(nameof(logger));
             _projectAnalyzer = projectAnalyzer ?? throw new ArgumentNullException(nameof(projectAnalyzer));
             _resultsReporter = resultsReporter ?? throw new ArgumentNullException(nameof(resultsReporter));
             _failFast = failFast;
        }

        public bool AnalyzeGraph(string rootProject)
        {
            // First get the nodes in our project graph
            _logger.Log("Calculating project graph...");
            var sw = new Stopwatch();
            sw.Start();
            ProjectGraphNode[] nodes = GetProjectNodesForProjectGraph(rootProject);
            sw.Stop();
            _logger.Log($"Graph generation took {sw.ElapsedMilliseconds}ms.\r\nProcessing {nodes.Length} project(s) in the tree.\r\n");

            // Next, analyze them!
            sw.Reset();
            sw.Start();
            bool ultimateResult = AnalyzeProjectNodes(nodes);
            sw.Stop();
            _logger.Log($"Graph analysis took {sw.ElapsedMilliseconds/1000/60}m, {sw.ElapsedMilliseconds / 1000}s.");

            return ultimateResult;
        }

        internal ProjectGraphNode[] GetProjectNodesForProjectGraph(string rootProjectFilePath)
        {
            if (!File.Exists(rootProjectFilePath)) throw new FileNotFoundException(rootProjectFilePath);

            var graph = new ProjectGraph(rootProjectFilePath);

            return graph
                .ProjectNodesTopologicallySorted
                .Where(n => !n.ProjectInstance.FullPath.Contains(".proj")) // Really anything that ends in .proj probably shouldn't be loaded in VS (file copy projects, etc.)
                .ToArray();
        }

        internal bool AnalyzeProjectNodes(ProjectGraphNode[] nodes)
        {
            bool ultimateResult = true;

            foreach (var node in nodes)
            {
                _logger.Log($"Starting analysis of project '{node.ProjectInstance.FullPath}'.");

                DateTime scanStart = DateTime.Now;
                (bool result, string failureMessage) = _projectAnalyzer.IsBuildUpToDate(node.ProjectInstance.FullPath);
                DateTime scanStop = DateTime.Now;
                TimeSpan diff = scanStop - scanStart;

                _logger.Log($"Project build check took {diff.TotalSeconds:F2}s.");
                _logger.Log(string.Empty);

                _resultsReporter.ReportProjectAnalysisResult(new BuildCheckResult() { FullProjectPath = node.ProjectInstance.FullPath, IsUpToDate = result, ScanStart = scanStart, ScanDuration = (scanStop - scanStart), FailureMessage = failureMessage});

                ultimateResult = ultimateResult && result;

                // If we have a failed up to date check, stop processing.
                if (_failFast && !ultimateResult)
                {
                    break;
                }
            }

            return ultimateResult;
        }
    }
}
