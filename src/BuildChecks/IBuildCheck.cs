// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Execution;
using Microsoft.Build.Prediction;
using System;
using System.Collections.Generic;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Context class for consumption by IBuildCheck.
    /// </summary>
    public class ProjectBuildCheckContext
    {
        public ILogger Logger { get; set; }
        public ProjectInstance Instance { get; set; }
        public ProjectPredictions Predictions { get; set; }
        public HashSet<string> Inputs { get; set; }
        public HashSet<string> Outputs { get; set; }
        public Dictionary<string, DateTime> TimeStampCache { get; set; }
        public bool VerboseOutput { get; set; }
    }

    /// <summary>
    /// Defines the interface for build checkers.
    /// </summary>
    public interface IBuildCheck
    {
        /// <summary>
        /// Checks if the build is up to date.
        /// </summary>
        /// <param name="context">The context for this particular check.</param>
        /// <param name="errors">A list for found errors to be added to.</para>
        /// <returns>True if the build is up to date. False if not up to date.</returns>
        bool Check(ProjectBuildCheckContext context, out string checkFailureMessage);
    }
}
