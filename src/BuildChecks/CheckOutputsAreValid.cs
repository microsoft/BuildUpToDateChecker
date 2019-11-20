// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Checks that all the inputs are older than all the outputs.
    /// </summary>
    internal class CheckOutputsAreValid : IBuildCheck
    {
        public bool Check(ProjectBuildCheckContext context, out string failureMessage)
        {
            context.Logger.LogVerbose(string.Empty);
            context.Logger.LogVerbose("CheckOutputsAreValid:");

            (DateTime? outputTime, string outputPath) = GetEarliestOutput(context.Outputs, context.TimeStampCache);

            if (outputTime != null)
            {
                // Search for an input that's either missing or newer than the earliest output.
                // As soon as we find one, we can stop the scan.
                // Due to some recently introduced issues (https://github.com/dotnet/project-system/issues/4736),
                // explicitly skip the CoreCompileInputs.cache file.
                foreach (string input in context.Inputs.Where(i => !i.EndsWith(".CoreCompileInputs.cache", StringComparison.OrdinalIgnoreCase)))
                {
                    DateTime? time = Utilities.GetTimestampUtc(input, context.TimeStampCache);

                    if (time == null)
                    {
                        failureMessage = $"Input '{input}' does not exist, not up to date.";
                        context.Logger.LogVerbose($"    {failureMessage}");
                        return false;
                    }

                    if (time > outputTime)
                    {
                        failureMessage = $"Input '{input}' is newer ({time.Value:O}) than earliest output '{outputPath}' ({outputTime.Value:O}), not up to date.";
                        context.Logger.LogVerbose($"    {failureMessage}");
                        return false;
                    }
                }

                context.Logger.LogVerbose($"    No inputs are newer than earliest output '{outputPath}' ({outputTime.Value}).");
            }
            else if (outputPath != null)
            {
                failureMessage = $"Output '{outputPath}' does not exist, not up to date.";
                context.Logger.LogVerbose($"    {failureMessage}");
                return false;
            }
            else
            {
                context.Logger.LogVerbose("    No build outputs defined.");
            }

            context.Logger.LogVerbose("    Up to date.");

            failureMessage = string.Empty;
            return true;
        }

        private static (DateTime? time, string path) GetEarliestOutput(IEnumerable<string> outputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime? earliest = DateTime.MaxValue;
            string earliestPath = null;
            bool hasOutput = false;

            foreach (string output in outputs)
            {
                DateTime? time = Utilities.GetTimestampUtc(output, timestampCache);

                if (time == null)
                {
                    return (null, output);
                }

                if (time < earliest)
                {
                    earliest = time;
                    earliestPath = output;
                }

                hasOutput = true;
            }

            return hasOutput
                ? (earliest, earliestPath)
                : (null, null);
        }
    }
}
