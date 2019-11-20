// Copyright (c) Microsoft Corporation. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using McMaster.Extensions.CommandLineUtils;

namespace BuildUpToDateChecker
{
    [ExcludeFromCodeCoverage]
    [Command(Name = "BuildUpToDateChecker", Description = "Analyzes a build tree to determine if the build is up-to-date.")]
    [HelpOption("-?")]
    public class Options
    {
        [Argument(0, "Path to project (or traversal project) to load.")]
        [Required]
        [FileExists]
        public string InputProjectFile { get; set; }

        [Option("--out:<file>", Description = "Full path to output report file.")]
        public string OutputReportFile { get; set; }

        [Option("--prop:<name>=<value>", "An additional global MsBuild property to set. By default the following are set: Configuration=Verbose and Platform=AnyCPU", CommandOptionType.MultipleValue)]
        public string[] AdditionalMsBuildProperties { get; set; }

        [Option("--msbuild <file>", Description = "Path to MSBuild.exe.")]
        [FileExists]
        public string MsBuildPath { get; set; }

        [Option("-v|--verbose", Description = "Outputs additional debugging information to standard output.")]
        public bool Verbose { get; set; }

        [Option("-d|--debug", Description = "Breaks waiting for a debugger to attach.")]
        public bool AttachDebugger { get; set; }

        [Option("-ff|--failfast", Description = "Analysis will stop after hitting the first up-to-date check failure for a project.")]
        public bool FailOnFirstError { get; set; }

        [Option("-sbl|--showbuildlogs", Description = "When specifying --verbose, always write out the design-time build's log. By default, this is only written if there is a design-time build failure.")]
        public bool AlwaysDumpBuildLogOnVerbose { get; set; }

        public Options()
        {
            // Set defaults here.
            OutputReportFile = ".\\project-results.json";
        }

        public int OnExecute()
        {
            return new Program(this).Run() ? 0 : 1;
        }
    }
}
