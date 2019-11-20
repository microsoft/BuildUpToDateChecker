// Copyright (c) Microsoft Corporation. All rights reserved.

namespace BuildUpToDateChecker
{
    /// <summary>
    /// Interface for logging.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a message to the log on a single line.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        void Log(string message);

        /// <summary>
        /// Writes a verbose message to the log on a single line (if verbose mode is enabled).
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        void LogVerbose(string message);
    }
}
