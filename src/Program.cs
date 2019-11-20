// Copyright (c) Microsoft Corporation. All rights reserved.

using BuildUpToDateChecker.BuildChecks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;

namespace BuildUpToDateChecker
{
    [Command(Name = "BuildUpToDateChecker", Description = "Analyzes a build tree to determine if the build is up-to-date.")]
    [HelpOption("-?")]
    internal sealed class Program
    {
        private readonly Options _options;

        public Program(Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        internal static int Main(string[] args) => CommandLineApplication.Execute<Options>(args);

        internal bool Run()
        {
            WaitForDebugger();

            ILogger logger = new ConsoleLogger(_options.Verbose);
            OutputHeader(logger);
            DumpEnvironmentVariables(logger);

            IResultsReporter resultsReporter = null;
            try
            {
                logger.Log(
                    $"Registered MSBuild from '{SetUpMsBuildAssemblyResolution(logger, _options.MsBuildPath)}'.\r\n");

                resultsReporter = new ResultsReporter(_options.OutputReportFile);
                resultsReporter.Initialize();

                GraphAnalyzer graphAnalyzer = new GraphAnalyzer(
                    logger,
                    new ProjectAnalyzer(
                        logger,
                        new DesignTimeBuildRunner(
                            logger,
                            GetAdditionalMsBuildProperties(logger, _options.AdditionalMsBuildProperties), alwaysLogBuildLog: _options.AlwaysDumpBuildLogOnVerbose),
                        new BuildCheckProvider(),
                        _options.Verbose),
                    resultsReporter,
                    _options.FailOnFirstError);

                return graphAnalyzer.AnalyzeGraph(_options.InputProjectFile);
            }
            catch (Exception ex)
            {
                logger.Log(ex.ToString());
                throw;
            }
            finally
            {
                resultsReporter?.TearDown();
            }
        }

        [ExcludeFromCodeCoverage]
        internal static string SetUpMsBuildAssemblyResolution(ILogger logger, string msBuildPathArgument)
        {
            // First, see if they've manually specified the path to MSBuild.
            if (!string.IsNullOrEmpty(msBuildPathArgument))
            {
                if (File.Exists(msBuildPathArgument))
                {
                    string msBuildDirectory = Path.GetDirectoryName(msBuildPathArgument);
                    MSBuildLocator.RegisterMSBuildPath(msBuildDirectory);
                    return msBuildDirectory;
                }
                else
                {
                    logger.Log($"Unable to find MSBuild at specified location '{msBuildPathArgument}'. Will attempt to find it automatically.");
                }
            }

            // Second, see if we're running in CoreXT.
            var coreXtBuildTools = Environment.GetEnvironmentVariable("MSBuildToolsPath_160");
            if (!string.IsNullOrEmpty(coreXtBuildTools) && Directory.Exists(coreXtBuildTools))
            {
                MSBuildLocator.RegisterMSBuildPath(coreXtBuildTools);
                return coreXtBuildTools;
            }

            // Lastly, see if we can find a VisualStudio instance.
            var vsInstance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
            if (vsInstance != null)
            {
                MSBuildLocator.RegisterInstance(vsInstance);
                return vsInstance.MSBuildPath;
            }

            throw new Exception("Unable to find an MSBuild instance. Aborting.");
        }

        internal static IDictionary<string, string> GetAdditionalMsBuildProperties(ILogger logger, string[] msbuildPropertiesArgument)
        {
            var additionalMsBuildProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Default to a Debug|AnyCPU configuration. This is because this is what a simple "msbuild.exe" call does.
                    // This can be manually overridden.
                    { "Configuration", "Debug" },
                    { "Platform", "AnyCPU" }
                };

            // Add any additional specified properties.
            if (msbuildPropertiesArgument != null && msbuildPropertiesArgument.Length > 0)
            {
                foreach (string prop in msbuildPropertiesArgument)
                {
                    string[] parts = prop.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2) continue;

                    additionalMsBuildProperties[parts[0]] = parts[1];
                }

                // Output properties in use:
                logger.Log("Using the following MsBuild properties:");
                foreach (KeyValuePair<string, string> kvp in additionalMsBuildProperties)
                {
                    logger.Log($"    {kvp.Key}={kvp.Value}");
                }
                logger.Log(string.Empty);
            }

            return additionalMsBuildProperties;
        }

        [ExcludeFromCodeCoverage]
        internal void DumpEnvironmentVariables(ILogger logger)
        {
            // Only dump env vars in debug mode.
            if (!_options.Verbose) return;

            logger.LogVerbose("Starting Environment Variable Values:");
            IDictionary envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry item in envVars.Cast<DictionaryEntry>().OrderBy(de => de.Key))
            {
                logger.LogVerbose($"    {item.Key}: {item.Value}");
            }

            logger.LogVerbose(string.Empty);
        }

        [ExcludeFromCodeCoverage]
        internal void OutputHeader(ILogger logger)
        {
            logger.Log("Build Up To Date Checker");
            logger.Log(string.Empty);
            logger.Log("Arguments:");
            logger.Log($"    Root project to analyze: {_options.InputProjectFile}");
            logger.Log($"    Report output file: {_options.OutputReportFile}");
            logger.Log($"    MSBuild specified: { _options.MsBuildPath ?? "[None]" }");
            logger.Log($"    Additional MSBuild properties: { (_options.AdditionalMsBuildProperties == null ? "[None]" : string.Join(", ", _options.AdditionalMsBuildProperties)) }");
            logger.Log($"    Verbose output: {_options.Verbose}");
            logger.Log($"    Attach debugger: {_options.AttachDebugger}");
            logger.Log($"    Fail on first up-to-date check failure: {_options.FailOnFirstError}");
            logger.Log(string.Empty);
        }

        [ExcludeFromCodeCoverage]
        private void WaitForDebugger()
        {
            if (!_options.AttachDebugger)
            {
                return;
            }

            Process p = Process.GetCurrentProcess();
            Console.WriteLine($"Waiting for debugger to attach to {p.ProcessName} ({p.Id})..."); // Directly use Console.WriteLine. Logger isn't created yet.

            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }

            Debugger.Break();
        }
    }
}
