// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace BuildUpToDateChecker
{
    internal class DesignTimeBuildRunner : IDesignTimeBuildRunner
    {
        private const int DefaultBuildRetryCount = 3;

        private readonly ILogger _logger;
        private readonly IDictionary<string, string> _globalProperties;
        private readonly int _numRetries;
        private readonly bool _alwaysLogBuildLog;

        public DesignTimeBuildRunner() : this(null, null) { }

        public DesignTimeBuildRunner(ILogger logger, IDictionary<string, string> additionalGlobalProperties, int numRetries = DefaultBuildRetryCount, bool alwaysLogBuildLog = false)
        {
            _logger = logger;

            _globalProperties = additionalGlobalProperties ?? new Dictionary<string, string>();
            _globalProperties["DesignTimeBuild"] = "true";
            _globalProperties["SkipCompilerExecution"] = "true";
            _globalProperties["ProvideCommandLineArgs"] = "true";
            _globalProperties["BuildingInsideVisualStudio"] = "true";
            _globalProperties["ShouldUnsetParentConfigurationAndPlatform"] = "false";
            _globalProperties["GenerateTargetFrameworkMonikerAttribute"] = "false";

            _numRetries = numRetries;
            _alwaysLogBuildLog = alwaysLogBuildLog;
        }

        public ProjectInstance Execute(Project project)
        {
            _logger.LogVerbose($"Beginning design-time build of project {project.FullPath}.");

            _logger.LogVerbose("Setting the following global properties:");
            foreach (KeyValuePair<string, string> kvp in _globalProperties)
            {
                project.SetGlobalProperty(kvp.Key, kvp.Value);
                _logger.LogVerbose($"    {kvp.Key}={kvp.Value}");
            }

            ProjectInstance projectInstance = project.CreateProjectInstance();

            var designTimeBuildTargets = new string[] {
                "CollectResolvedSDKReferencesDesignTime",
                "CollectPackageReferences",
                "ResolveComReferencesDesignTime",
                "ResolveProjectReferencesDesignTime",
                "BuiltProjectOutputGroup",
                "ResolveAssemblyReferencesDesignTime",
                "CollectSDKReferencesDesignTime",
                "ResolvePackageDependenciesDesignTime",
                "CompileDesignTime",
                "CollectFrameworkReferences",
                "CollectUpToDateCheckBuiltDesignTime",
                "CollectPackageDownloads",
                "CollectAnalyzersDesignTime",
                "CollectUpToDateCheckInputDesignTime",
                "CollectUpToDateCheckOutputDesignTime",
                "CollectResolvedCompilationReferencesDesignTime" };

            SimpleMsBuildLogger buildLogger;
            bool result = false;
            int retries = 0;

            // Retrying here as there are some odd cases where a file will be in use by another process
            // long enough for this to fail, but will work on a subsequent attempt.
            do
            {
                buildLogger = new SimpleMsBuildLogger();

                _logger.LogVerbose($"Attempting design-time build # {retries + 1}...");
                result = projectInstance.Build(designTimeBuildTargets, new Microsoft.Build.Framework.ILogger[] { buildLogger });
            }
            while (!result && (++retries < _numRetries));

            if (!result || _alwaysLogBuildLog)
            {
                _logger.LogVerbose("Design time build log:");
                _logger.LogVerbose(buildLogger.LogText);
                _logger.LogVerbose(string.Empty);

            }

            if (!result)
            {
                throw new Exception("Failed to build project.\r\n" + buildLogger.ErrorText);
            }

            return projectInstance;
        }
    }
}
