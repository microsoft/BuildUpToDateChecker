// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Text;
using Microsoft.Build.Framework;

namespace BuildUpToDateChecker
{
    /// <summary>
    /// A simple MsBuild ILogger implementation to get the MsBuild log output.
    /// </summary>
    internal class SimpleMsBuildLogger : Microsoft.Build.Framework.ILogger
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly StringBuilder _sbError = new StringBuilder();

        public string LogText => _sb.ToString();
        public string ErrorText => _sbError.ToString();

        public void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += (sender, args) =>
            {
                _sb.AppendLine($"{args.GetType().Name}: {args.Message}");
            };

            eventSource.ErrorRaised += (sender, args) =>
            {
                _sbError.AppendLine(args.Message);
            };
        }

        public void Shutdown()
        {
        }

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Diagnostic;

        public string Parameters { get; set; }
    }
}
