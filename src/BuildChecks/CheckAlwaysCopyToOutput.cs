// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Execution;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Checks for any items with CopyToOutputDirectory=Always.
    /// </summary>
    internal class CheckAlwaysCopyToOutput : IBuildCheck
    {
        private readonly HashSet<string> _itemTypesForUpToDateCheckInput;

        public CheckAlwaysCopyToOutput(HashSet<string> itemTypesForUpToDateCheckInput)
        {
            _itemTypesForUpToDateCheckInput = itemTypesForUpToDateCheckInput ?? throw new ArgumentNullException(nameof(itemTypesForUpToDateCheckInput));
        }

        public bool Check(ProjectBuildCheckContext context, out string failureMessage)
        {
            context.Logger.LogVerbose(string.Empty);
            context.Logger.LogVerbose("CheckAlwaysCopyToOutput:");

            IEnumerable<ProjectItemInstance> itemsUpToDateCheckInput = context.Instance.Items.Where(i => _itemTypesForUpToDateCheckInput.Contains(i.ItemType));

            foreach (ProjectItemInstance a in itemsUpToDateCheckInput)
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
