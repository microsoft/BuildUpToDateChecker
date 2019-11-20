// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Execution;
using System;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Checks for any items with CopyToOutputDirectory=Always.
    /// </summary>
    internal class CheckAlwaysCopyToOutput : IBuildCheck
    {
        public bool Check(ProjectBuildCheckContext context, out string failureMessage)
        {
            context.Logger.LogVerbose(string.Empty);
            context.Logger.LogVerbose("CheckAlwaysCopyToOutput:");

            foreach (ProjectItemInstance a in context.Instance.Items)
            {
                if (a.HasMetadata("CopyToOutputDirectory") && a.GetMetadataValue("CopyToOutputDirectory").Equals("Always", StringComparison.OrdinalIgnoreCase))
                {
                    failureMessage = $"Item '{a.GetMetadataValue("FullPath")}' has CopyToOutputDirectory set to 'Always', not up to date.";
                    context.Logger.LogVerbose($"    {failureMessage}");
                    return false;
                }
            }

            context.Logger.LogVerbose("    Up to date.");

            failureMessage = string.Empty;
            return true;
        }
    }
}
