// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Implementation of IBuildCheckProvider
    /// </summary>
    internal class BuildCheckProvider : IBuildCheckProvider
    {
        /// <summary>
        /// Returns all of the build checks that are:
        ///     a. In this assembly,
        ///     b. Implements IBuildCheck
        ///     c. Is NOT an abstract class
        ///     d. Is NOT an interface.
        /// </summary>
        /// <returns>An enumeration of the build checks to run for a project.</returns>
        public IEnumerable<IBuildCheck> GetBuildChecks()
        {
            return new IBuildCheck[]
            {
                new CheckAlwaysCopyToOutput(), 
                new CheckAreCopyToOutputDirectoryFilesValid(), 
                new CheckCopyUpToDateMarkersValid(),
                new CheckOutputsAreValid(), 
                new CheckUpToDateCheckBuiltItems()
            };
        }
    }
}
