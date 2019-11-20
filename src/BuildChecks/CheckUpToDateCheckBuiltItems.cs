// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Execution;
using System;
using System.IO;
using System.Linq;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Checks the status of the project's UpToDateCheckBuilt items.
    /// </summary>
    internal class CheckUpToDateCheckBuiltItems : IBuildCheck
    {
        public bool Check(ProjectBuildCheckContext context, out string failureMessage)
        {
            context.Logger.LogVerbose(string.Empty);
            context.Logger.LogVerbose("CheckUpToDateCheckBuiltItems:");

            foreach (ProjectItemInstance copiedOutputFiles in context.Instance.GetItems("UpToDateCheckBuilt").Where(i => i.HasMetadata("Original") && !string.IsNullOrEmpty(i.GetMetadataValue("Original"))))
            {
                var source = ConvertToAbsolutePath(copiedOutputFiles.GetMetadataValue("Original"), Path.GetDirectoryName(context.Instance.FullPath));
                var destination = copiedOutputFiles.GetMetadataValue("FullPath");

                context.Logger.LogVerbose($"    Checking copied output (UpToDateCheckBuilt with Original property) file '{source}':");

                DateTime? sourceTime = Utilities.GetTimestampUtc(source, context.TimeStampCache);

                if (sourceTime != null)
                {
                    context.Logger.LogVerbose($"        Source {sourceTime}: '{source}'.");
                }
                else
                {
                    failureMessage = $"Source '{source}' does not exist, not up to date.";
                    context.Logger.LogVerbose($"    {failureMessage}");
                    return false;
                }

                DateTime? destinationTime = Utilities.GetTimestampUtc(destination, context.TimeStampCache);

                if (destinationTime != null)
                {
                    context.Logger.LogVerbose($"        Destination {destinationTime}: '{destination}'.");
                }
                else
                {
                    failureMessage = "Destination '{destination}' does not exist, not up to date.";
                    context.Logger.LogVerbose($"    {failureMessage}");
                    return false;
                }

                if (destinationTime < sourceTime)
                {
                    failureMessage = "Source is newer than build output destination, not up to date.";
                    context.Logger.LogVerbose($"    {failureMessage}");
                    return false;
                }
            }

            context.Logger.LogVerbose("    Up to date.");

            failureMessage = string.Empty;
            return true;
        }

        private static string ConvertToAbsolutePath(string path, string projectPath)
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(projectPath, path);
        }
    }
}
