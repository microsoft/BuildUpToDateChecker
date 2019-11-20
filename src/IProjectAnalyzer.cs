// Copyright (c) Microsoft Corporation. All rights reserved.

namespace BuildUpToDateChecker
{
    /// <summary>
    /// Interface defining a Project Analyzer.
    /// </summary>
    public interface IProjectAnalyzer
    {
        /// <summary>
        /// Checks a project to see if it's up to date.
        /// </summary>
        /// <param name="fullPathToProjectFile">The full path to the project file.</param>
        /// <returns>True if the project is up to date. Else, false. Also returns the failure message, if it failed.</returns>
        (bool IsUpToDate, string failureMessage) IsBuildUpToDate(string fullPathToProjectFile);
    }
}
