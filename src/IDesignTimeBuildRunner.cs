// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace BuildUpToDateChecker
{
    /// <summary>
    /// Interface for the design-time build runner.
    /// </summary>
    public interface IDesignTimeBuildRunner
    {
        /// <summary>
        /// Executes a design-time build of a project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> object to run the design-time build on.</param>
        /// <returns>The resulting <see cref="ProjectInstance"/> object of the design-time build.</returns>
        ProjectInstance Execute(Project project);
    }
}
