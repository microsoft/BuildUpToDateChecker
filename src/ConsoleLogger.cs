// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;

namespace BuildUpToDateChecker
{
    /// <summary>
    /// Console logger implementation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class ConsoleLogger : ILogger
    {
        public readonly bool _verbose;

        public ConsoleLogger(bool verbose)
        {
            _verbose = verbose;
        }

        public void Log(string message)
        {
            if (message != null)
            {
                Console.WriteLine(message);
            }
        }

        public void LogVerbose(string message)
        {
            if (_verbose)
            {
                Log(message);
            }
        }
    }
}
