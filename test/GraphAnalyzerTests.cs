// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BuildUpToDateChecker.Tests
{
    [TestClass]
    public class GraphAnalyzerTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestMissingLogger()
        {
            var mockProjectAnalyzer = new Mock<IProjectAnalyzer>();
            var mockResultsReporter = new Mock<IResultsReporter>();

            new GraphAnalyzer(null, mockProjectAnalyzer.Object, mockResultsReporter.Object, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestMissingAnalyzer()
        {
            var mockLogger = new Mock<ILogger>();
            var mockResultsReporter = new Mock<IResultsReporter>();

            new GraphAnalyzer(mockLogger.Object, null, mockResultsReporter.Object, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestMissingReporter()
        {
            var mockLogger = new Mock<ILogger>();
            var mockProjectAnalyzer = new Mock<IProjectAnalyzer>();

            new GraphAnalyzer(mockLogger.Object, mockProjectAnalyzer.Object, null, false);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestMissingProjectFile()
        {
            var mockLogger = new Mock<ILogger>();
            var mockProjectAnalyzer = new Mock<IProjectAnalyzer>();
            var mockResultsReporter = new Mock<IResultsReporter>();

            var analyzer = new GraphAnalyzer(mockLogger.Object, mockProjectAnalyzer.Object, mockResultsReporter.Object, true);

            string doesNotExistPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
            analyzer.AnalyzeGraph(doesNotExistPath);
        }

        [TestMethod]
        public void TestExistingProjectFile()
        {
            string testProject = TestUtilities.CreateTestProject();

            var mockLogger = new Mock<ILogger>();
            var mockProjectAnalyzer = new Mock<IProjectAnalyzer>();
            var mockResultsReporter = new Mock<IResultsReporter>();

            mockProjectAnalyzer.Setup(a => a.IsBuildUpToDate(It.IsAny<string>())).Returns((true, string.Empty));

            var analyzer = new GraphAnalyzer(mockLogger.Object, mockProjectAnalyzer.Object, mockResultsReporter.Object, true);
            bool isUpToDate = analyzer.AnalyzeGraph(testProject);
            Assert.IsTrue(isUpToDate);

            TestUtilities.CleanUpTestProject(testProject);
        }
    }
}
