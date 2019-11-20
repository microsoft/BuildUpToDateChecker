// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Execution;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Checks that the copy references marker is up to date. (Only applies to SDK-style projects.)
    /// </summary>
    internal class CheckCopyUpToDateMarkersValid : IBuildCheck
    {
        public bool Check(ProjectBuildCheckContext context, out string failureMessage)
        {
            failureMessage = string.Empty;

            context.Logger.LogVerbose(string.Empty);
            context.Logger.LogVerbose("CheckCopyUpToDateMarkersValid:");

            ProjectItemInstance markerFileItem = context.Instance.GetItems("CopyUpToDateMarker").FirstOrDefault();

            if (string.IsNullOrWhiteSpace(markerFileItem?.EvaluatedInclude))
            {
                // This should happen on non-SDK Projects
                context.Logger.LogVerbose("    Not an SDK project. Marker files aren't used. Up to date.");
                return true;
            }

            // Get all of the reference assemblies.
            List<string> copyReferenceInputs = context.Instance.GetItems("ReferencePathWithRefAssemblies").Select(i => i.GetMetadataValue("FullPath")).ToList();
            if (copyReferenceInputs.Count == 0)
            {
                context.Logger.LogVerbose("    No input markers exist, skipping marker check. Up to date.");
                return true;
            }

            context.Logger.LogVerbose("    Adding input reference copy markers:");
            foreach (string referenceMarkerFile in copyReferenceInputs.OrderBy(i => i))
            {
                context.Logger.LogVerbose($"        '{referenceMarkerFile}'");
            }

            (DateTime latestInputMarkerTime, string latestInputMarkerPath) = GetLatestInput(copyReferenceInputs, context.TimeStampCache);
            context.Logger.LogVerbose($"    Latest write timestamp on input marker is {latestInputMarkerTime} on '{latestInputMarkerPath}'.");

            // Get the marker file.
            string markerFile = markerFileItem.GetMetadataValue("FullPath");
            context.Logger.LogVerbose("    Adding output reference copy marker:");
            context.Logger.LogVerbose($"        '{markerFile}'");

            DateTime? outputMarkerTime = Utilities.GetTimestampUtc(markerFile, context.TimeStampCache);
            if (outputMarkerTime != null)
            {
                context.Logger.LogVerbose($"    Write timestamp on output marker is {outputMarkerTime} on '{markerFile}'.");
            }
            else
            {
                context.Logger.LogVerbose($"    Output marker '{markerFile}' does not exist, skipping marker check. Up to date.");
                return true;
            }

            if (outputMarkerTime < latestInputMarkerTime)
            {
                failureMessage = $"Input marker ('{latestInputMarkerPath}': {latestInputMarkerTime:O}) is newer than output marker ('{markerFile}': {outputMarkerTime:O}), not up to date.";
                context.Logger.LogVerbose($"    {failureMessage}");
                return false;
            }

            context.Logger.LogVerbose("    Up to date.");
            return true;
        }

        private static (DateTime time, string path) GetLatestInput(IEnumerable<string> inputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime latest = DateTime.MinValue;
            string latestPath = null;

            foreach (string input in inputs)
            {
                DateTime? time = Utilities.GetTimestampUtc(input, timestampCache);

                if (time > latest)
                {
                    // TODO remove pragmas when https://github.com/dotnet/roslyn/issues/37039 is fixed
#pragma warning disable CS8629 // Nullable value type may be null
                    latest = time.Value;
#pragma warning restore CS8629
                    latestPath = input;
                }
            }

            return (latest, latestPath);
        }
    }
}
