// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Text;

namespace BuildUpToDateChecker.Tests
{
    internal class TestLogger : ILogger
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public string LogText => _sb.ToString();

        public void Log(string message)
        {
            _sb.AppendLine(message);
        }

        public void LogVerbose(string message)
        {
            _sb.AppendLine(message);
        }
    }
}
