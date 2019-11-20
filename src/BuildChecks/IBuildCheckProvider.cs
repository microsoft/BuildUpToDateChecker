// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;

namespace BuildUpToDateChecker.BuildChecks
{
    /// <summary>
    /// Interface for a build check provider.
    /// </summary>
    public interface IBuildCheckProvider
    {
        IEnumerable<IBuildCheck> GetBuildChecks();
    }
}
