// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Execution;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Checks that items with CopyToOutputDirectory=PreserveNewest are up to date.
    /// </summary>
    internal class CheckAreCopyToOutputDirectoryFilesValid : IBuildCheck
    {
        private readonly HashSet<string> _itemTypesForUpToDateCheckInput;

        public CheckAreCopyToOutputDirectoryFilesValid(HashSet<string> itemTypesForUpToDateCheckInput)
        {
            _itemTypesForUpToDateCheckInput = itemTypesForUpToDateCheckInput ?? throw new ArgumentNullException(nameof(itemTypesForUpToDateCheckInput));
        }

        public bool Check(ProjectBuildCheckContext context, out string failureMessage)
        {
            context.Logger.LogVerbose(string.Empty);
            context.Logger.LogVerbose("CheckAreCopyToOutputDirectoryFilesValid:");

            IEnumerable<ProjectItemInstance> items = context.Instance.Items.Where(i => i.HasMetadata("CopyToOutputDirectory") && i.GetMetadataValue("CopyToOutputDirectory").Equals("PreserveNewest", StringComparison.OrdinalIgnoreCase));

            IEnumerable<ProjectItemInstance> itemsUpToDateCheckInput = items.Where(i => _itemTypesForUpToDateCheckInput.Contains(i.ItemType));

            foreach (ProjectItemInstance item in itemsUpToDateCheckInput)
            {
                var rootedPath = item.GetMetadataValue("FullPath");
                var link = item.GetMetadataValue("Link");

                string filename = rootedPath;

                if (string.IsNullOrEmpty(filename))
                {
                    continue;
                }

                context.Logger.LogVerbose($"    Checking PreserveNewest file '{rootedPath}':");

                DateTime? itemTime = Utilities.GetTimestampUtc(filename, context.TimeStampCache);

                if (itemTime != null)
                {
                    context.Logger.LogVerbose($"        Source {itemTime}: '{filename}'.");
                }
                else
                {
                    failureMessage = $"Source '{filename}' does not exist, not up to date.";
                    context.Logger.LogVerbose($"        {failureMessage}");
                    return false;
                }

                string outputFullPath = GetOutputFolder(context.Instance);
                string outputFileItem = string.IsNullOrEmpty(link) ? Path.Combine(outputFullPath, filename.Replace(context.Instance.Directory, string.Empty).Trim('\\')) : Path.Combine(outputFullPath, link);
                DateTime? outputItemTime = Utilities.GetTimestampUtc(outputFileItem, context.TimeStampCache);

                if (outputItemTime != null)
                {
                    context.Logger.LogVerbose($"        Destination {outputItemTime}: '{filename}'.");
                }
                else
                {
                    failureMessage = $"Destination '{outputFileItem}' does not exist, not up to date.";
                    context.Logger.LogVerbose($"        {failureMessage}");
                    return false;
                }

                if (outputItemTime < itemTime)
                {
                    failureMessage = "PreserveNewest source is newer than destination, not up to date.";
                    context.Logger.LogVerbose($"        {failureMessage}");
                    return false;
                }
            }

            context.Logger.LogVerbose("    Up to date.");

            failureMessage = string.Empty;
            return true;
        }

        private static string GetOutputFolder(ProjectInstance projectInstance)
        {
            return Path.Combine(projectInstance.Directory, projectInstance.GetPropertyValue("OutDir"));
        }
    }
}
