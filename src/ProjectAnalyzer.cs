// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Prediction;
using Microsoft.Build.Prediction.Predictors;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using BuildUpToDateChecker.BuildChecks;

namespace BuildUpToDateChecker
{
    /// <summary>
    /// Analyzes a project to determine if it is up to date and shouldn't need to be rebuilt.
    /// </summary>
    internal sealed class ProjectAnalyzer : IProjectAnalyzer
    {
        private readonly ILogger _logger;
        private readonly IDesignTimeBuildRunner _designTimeBuilder;
        private readonly IBuildCheckProvider _checkProvider;
        private readonly bool _verboseOutput;

        /// <summary>
        /// Create an instance of <see cref="ProjectAnalyzer"/>.
        /// </summary>
        public ProjectAnalyzer(ILogger logger, IDesignTimeBuildRunner designTimeBuilder, IBuildCheckProvider checkProvider, bool verboseOutput = false) 
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _designTimeBuilder = designTimeBuilder ?? throw new ArgumentNullException(nameof(designTimeBuilder));
            _checkProvider = checkProvider ?? throw new ArgumentNullException(nameof(checkProvider));
            _verboseOutput = verboseOutput;
        }

        /// <summary>
        /// Performs analysis on the given project to determine if the project is up to date or not.
        /// </summary>
        /// <param name="fullPathToProjectFile">The full path to the project file.</param>
        /// <returns>True, if all project checks pass and the project is up to date. Else, false.</returns>
        public (bool, string) IsBuildUpToDate(string fullPathToProjectFile)
        {
            _logger.Log($"Checking if project '{ fullPathToProjectFile }' is up to date.");

            // Get project objects
            (Project project, ProjectInstance projectInstance) = GetProjectObjects(fullPathToProjectFile);

            // Get predictions for project
            ProjectPredictions predictions = GetProjectPredictions(project);

            (bool projectBuildIsUpToDate, string failureMessage) = IsBuildUpToDate(projectInstance, predictions);

            _logger.Log(projectBuildIsUpToDate ? "Build is up to date." : "Build is not up to date.");

            return (projectBuildIsUpToDate, failureMessage);
        }

        private (bool, string) IsBuildUpToDate(ProjectInstance projectInstance, ProjectPredictions predictions) 
        {
            HashSet<string> inputs = GetAllFiles(predictions.InputFiles, predictions.InputDirectories);
            var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddCustomInputs(inputs, projectInstance);
            AddCustomOutputs(outputs, projectInstance);

            // The prediction library can sometimes flag intermediate files as inputs.
            // For example, if a file was copied to $(OutDir) and then copied elsewhere,
            // it'll see that file as an input because it was the input to the 2nd copy.
            // To avoid this, we'll prune the input list, removing everything from $(OutDir).
            string outDir = projectInstance.GetPropertyValue("OutDir");
            inputs.RemoveWhere((input) => input.StartsWith(outDir, StringComparison.OrdinalIgnoreCase));
            _logger.LogVerbose($"Removing inputs residing in OutDir ({outDir})...");

            var context = new ProjectBuildCheckContext()
            {
                Logger = _logger,
                Instance = projectInstance,
                Predictions = predictions,
                Inputs = inputs,
                Outputs = outputs,
                TimeStampCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase),
                VerboseOutput = _verboseOutput
            };

            // Log predicted inputs and outputs.
            _logger.LogVerbose(string.Empty);
            _logger.LogVerbose("Predicted Inputs:");
            foreach (string input in inputs.OrderBy(i => i))
            {
                _logger.LogVerbose($"    {input}");
            }

            _logger.LogVerbose(string.Empty);
            _logger.LogVerbose("Predicted Outputs:");
            foreach (string output in outputs.OrderBy(i => i))
            {
                _logger.LogVerbose($"    {output}");
            }

            // All checks must pass for the build to be up to date.
            string failureMessage = string.Empty;
            return (_checkProvider.GetBuildChecks().All(buildCheck => buildCheck.Check(context, out failureMessage)), failureMessage);
        }

        private (Project, ProjectInstance) GetProjectObjects(string projectFilePath)
        {
            var buildLogger = new SimpleMsBuildLogger();

            Project project;
            try
            {
                project = new Project(
                    projectFilePath,
                    globalProperties: null,
                    toolsVersion: null,
                    new ProjectCollection(
                        null,
                        new Microsoft.Build.Framework.ILogger[] { buildLogger },
                        ToolsetDefinitionLocations.Default));
            }
            catch(Exception ex)
            {
                this._logger.Log($"Error attempting to load project '{projectFilePath}'.");
                this._logger.Log(ex.Message);

                if (_verboseOutput)
                {
                    this._logger.LogVerbose(buildLogger.LogText);
                }

                throw;
            }

            ProjectInstance projectInstance = _designTimeBuilder.Execute(project);

            return (project, projectInstance);
        }

        private static ProjectPredictions GetProjectPredictions(Project project)
        {
            // None and Content items are checked more carefully with other mechanisms.
            // Thus, we're excluding these items from the build predictor here.
            IEnumerable<IProjectPredictor> buildUpToDatePredictors = ProjectPredictors.AllPredictors.Where(p => !(p is NoneItemsPredictor) && !(p is ContentItemsPredictor));
            var predictionExecutor = new ProjectPredictionExecutor(buildUpToDatePredictors);
            return predictionExecutor.PredictInputsAndOutputs(project);
        }

        private static HashSet<string> GetAllFiles(IEnumerable<PredictedItem> files, IEnumerable<PredictedItem> folders)
        {
            var distinctFileSet = new HashSet<string>(files.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

            foreach (string file in folders.Where(d => Directory.Exists(d.Path)).SelectMany(d => Directory.EnumerateFiles(d.Path, "*", SearchOption.TopDirectoryOnly)))
            {
                distinctFileSet.Add(file);
            }

            return distinctFileSet;
        }

        private static void AddCustomOutputs(ISet<string> outputs, ProjectInstance projectInstance)
        {
            foreach (ProjectItemInstance customOutput in projectInstance.GetItems("UpToDateCheckOutput"))
            {
                outputs.Add(customOutput.GetMetadataValue("FullPath"));
            }

            foreach (ProjectItemInstance buildOutput in projectInstance.GetItems("UpToDateCheckBuilt").Where(i => string.IsNullOrEmpty(i.GetMetadataValue("Original"))))
            {
                outputs.Add(buildOutput.GetMetadataValue("FullPath"));
            }

            // See if this is a NoTarget SDK project. If so, skip the outputs.
            string usingNoTargets = projectInstance.GetPropertyValue("UsingMicrosoftNoTargetsSdk");
            bool isNoTargetsProject = !string.IsNullOrEmpty(usingNoTargets) && usingNoTargets.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (!isNoTargetsProject) return;

            // This IS a NoTarget SDK project, so we have to do some further adjusting. Because of: 
            // Target "CollectUpToDateCheckBuiltDesignTime" in file "C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Microsoft\VisualStudio\Managed\Microsoft.Managed.DesignTime.targets"
            RemoveNoTargetsOutputs(outputs, projectInstance);
        }

        private static void AddCustomInputs(ISet<string> inputs, ProjectInstance projectInstance)
        {
            // See for context: https://github.com/dotnet/project-system/pull/2416
            // "In the old project system, a project file can specify UpToDateCheckInput
            // and UpToDateCheckOutput items that add items into inputs or outputs that
            // the fast up-to-date check uses. It's a kind of escape hatch for special
            // project extensions that the project system is not aware of."
            foreach (ProjectItemInstance customInput in projectInstance.GetItems("UpToDateCheckInput"))
            {
                inputs.Add(customInput.GetMetadataValue("FullPath"));
            }
        }

        private static void RemoveNoTargetsOutputs(ISet<string> outputs, ProjectInstance projectInstance)
        {
            // Remove binaries and debug symbols in obj and bin.
            string targetPath = projectInstance.GetPropertyValue("TargetPath");
            if (outputs.Contains(targetPath)) { outputs.Remove(targetPath); }

            RemoveMatchingItemsFromSet(outputs, projectInstance, "IntermediateAssembly");
            RemoveMatchingItemsFromSet(outputs, projectInstance, "_DebugSymbolsIntermediatePath");
            RemoveMatchingItemsFromSet(outputs, projectInstance, "_DebugSymbolsOutputPath");
        }

        private static void RemoveMatchingItemsFromSet(ISet<string> outputs, ProjectInstance projectInstance, string itemName)
        {
            var items = projectInstance.GetItems(itemName);
            foreach (var item in items)
            {
                string fullPath = item.GetMetadataValue("FullPath");
                if (string.IsNullOrEmpty(fullPath))
                {
                    continue;
                }

                if (outputs.Contains(fullPath))
                {
                    outputs.Remove(fullPath);
                }
            }
        }
    }
}
