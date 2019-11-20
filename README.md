# Overview

The BuildUpToDateChecker is a tool that will help catch common incremental build problems. The tool will run against an MSBuild project (or project tree) and analyze it. Currently, it looks for five common issues:
1. Items with CopyToOutputDirectory set to 'Always'.
2. Items with CopyToOutputDirectory set to 'PreserveNewest' where the source file is newer (based on file timestamp) than the copied output file.
3. Reference items that are newer than the generated CopyUpToDateMarker (*.copycomplete) file.
4. Any input files that are newer than the newest output file.
5. Any output files that do not exist.

This code is based on the [Visual Studio project system](https://github.com/dotnet/project-system/blob/c2c17ed3423a797fda4bab9fa71442006ace373e/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/UpToDate/BuildUpToDateCheck.cs) code to closely mimic Visual Studio's behavior in determining the "up-to-date" status of a project, but in a stand alone tool.

**Note that this code is provided as-is as a proof-of-concept! No guarantees are given.**

# Usage

NOTE: The BuildUpToDateChecker tool uses a feature (project graph generation) requiring MSBuild 16.4 which is currently in preview. You can download this preview [here](https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-preview). If you have multiple installations (or aren't using a Visual Studio Command Prompt), be sure to use the `--msbuild` argument to point to this version.

In order for the BuildUpToDateChecker tool to be accurate, the first step is to do a full build with MSBuild:

    cd c:\my_code\
    msbuild my_root_project.proj

Once built, use this tool as follows:

    .\BuildUpToDateChecker.exe my_root_project.proj

Output will be generated to the console.
Project results will (by default) be output to file ".\project-results.json".

## Arguments

The following arguments will customize the tool's behavior:
* `--out`. Specify an alternate, full path for the report file. By default, the file will be named "project-results.json" and be written to the current directory.
* `--prop:name=value`, Add (or overwrite) additional msbuild properties that should be specified so that the tool's design-time build of a project will match the build you previously did. By default, property Configuration is set to "Debug", and property Platform is set to "AnyCPU". You can add to these properties or overwrite them. Again, set any properties necessary to match your previous build. E.g., if you did an x64 platform build, add `--prop:Platform=x64` to the command line.
* `--msbuild`, Specify the full path to a specific msbuild.exe to use. By default, the tool uses logic to find installed Visual Studio instances.
* `-v` or `--verbose`, This will output extra debugging information via the console. Note that this could be a LOT of output!
* `-d` or `--debug`, This will cause the tool to break and wait for a debugger to attach. Note: the tool WILL HANG until a debugger attaches.
* `-ff` or `--failfast`, This causes the tool to stop execution after the first project that fails any check. This can help speed up resolution as you don't have to wait for the analysis of every project in the dependency graph.
* `-sbl` or `--showbuildlogs`, By default, the tool will only output the MSBuild logs of the design-time build if that build fails. If you need to debug an issue and need this MSBuild log information, set this switch (in conjunction with `-v`) and the tool will output it. Note: this will also create a LOT of output!


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
