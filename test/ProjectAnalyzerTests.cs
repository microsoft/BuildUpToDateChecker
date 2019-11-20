// Copyright (c) Microsoft Corporation. All rights reserved.

using BuildUpToDateChecker.BuildChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;

namespace BuildUpToDateChecker.Tests
{
    [TestClass]
    public class ProjectAnalyzerTests
    {
        #region Constructor Tests...
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorNullLogger() 
        {
            Mock<IDesignTimeBuildRunner> buildRunner = new Mock<IDesignTimeBuildRunner>();
            Mock<IBuildCheckProvider> checks = new Mock<IBuildCheckProvider>();
            new ProjectAnalyzer(null, buildRunner.Object, checks.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorNullRunner()
        {
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IBuildCheckProvider> checks = new Mock<IBuildCheckProvider>();
            new ProjectAnalyzer(logger.Object, null, checks.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructorNullChecks()
        {
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IDesignTimeBuildRunner> buildRunner = new Mock<IDesignTimeBuildRunner>();
            new ProjectAnalyzer(logger.Object, buildRunner.Object, null);
        }
        #endregion

        [TestMethod]
        public void TestEmptyProjectWithNoChecksIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject();
            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: new Mock<IBuildCheckProvider>().Object);

            Assert.IsTrue(isUpToDate);
        }

        #region CopyToOutput = Always Tests...
        [TestMethod]
        public void TestAlwaysCopyToOutputDirectory_AlwaysIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><Content Include='MyContent.js'><CopyToOutputDirectory>Always</CopyToOutputDirectory></Content></ItemGroup>", 
                filesToCreate: new string[] { "MyContent.js" });

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckAlwaysCopyToOutput() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }

        [TestMethod]
        public void TestAlwaysCopyToOutputDirectory_NeverIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><Content Include='MyContent.js'><CopyToOutputDirectory>Never</CopyToOutputDirectory></Content></ItemGroup>",
                filesToCreate: new string[] { "MyContent.js" });

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckAlwaysCopyToOutput() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsTrue(isUpToDate);
        }
        #endregion

        #region CopyToOutput = PreserveNewest Tests...

        [TestMethod]
        public void TestAreCopyToOutputDirectoryFilesValid_MissingInputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><Content Include='MyContent.js'><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content></ItemGroup>");

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckAreCopyToOutputDirectoryFilesValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }

        [TestMethod]
        public void TestAreCopyToOutputDirectoryFilesValid_MissingOutputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><Content Include='MyContent.js'><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content></ItemGroup>",
                filesToCreate: new string[] { "MyContent.js" });

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckAreCopyToOutputDirectoryFilesValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }

        [TestMethod]
        public void TestAreCopyToOutputDirectoryFilesValid_ExistingOutputIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><Content Include='MyContent.js'><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content></ItemGroup>",
                filesToCreate: new string[] { "MyContent.js", "bin\\Debug\\net472\\MyContent.js" });

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckAreCopyToOutputDirectoryFilesValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsTrue(isUpToDate);
        }

        [TestMethod]
        public void TestAreCopyToOutputDirectoryFilesValid_ExistingOutputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><Content Include='MyContent.js'><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content></ItemGroup>",
                filesToCreate: new string[] { "bin\\Debug\\net472\\MyContent.js", "MyContent.js" });

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckAreCopyToOutputDirectoryFilesValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }
        #endregion

        #region CopyUpToDateMarkersValid Tests...
        [TestMethod]
        public void TestCheckCopyUpToDateMarkersValid_NoReferencesIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(outputBinaries: true);

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckCopyUpToDateMarkersValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsTrue(isUpToDate);
        }

        [TestMethod]
        public void TestCheckCopyUpToDateMarkersValid_MissingMarkerFileIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(outputBinaries: true);

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckCopyUpToDateMarkersValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            // NOTE: This is counter-intuitive to me. Checking with
            // owners of original code to verify this is the expected result.
            Assert.IsTrue(isUpToDate);
        }

        [TestMethod]
        public void TestCheckCopyUpToDateMarkersValid_ExistingMarkerFileIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(outputBinaries: true);

            // Set up the expected marker file.
            string markerFile = Path.Combine(Path.GetDirectoryName(projectFile), "obj\\Debug\\net472", Path.GetFileName(projectFile) + ".CopyComplete");
            Directory.CreateDirectory(Path.GetDirectoryName(markerFile));
            File.WriteAllText(markerFile, "// TEST");

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckCopyUpToDateMarkersValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsTrue(isUpToDate);
        }

        [TestMethod]
        public void TestCheckCopyUpToDateMarkersValid_ExistingMarkerFileIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(outputBinaries: true);

            // Set up the expected marker file.
            string markerFile = Path.Combine(Path.GetDirectoryName(projectFile), "obj\\Debug\\net472", Path.GetFileName(projectFile) + ".CopyComplete");
            Directory.CreateDirectory(Path.GetDirectoryName(markerFile));
            File.WriteAllText(markerFile, "// TEST");

            // Force the marker file to be "older".
            DateTime updatedTime = new DateTime(2001, 1, 1);
            FileInfo fi = new FileInfo(markerFile);
            fi.CreationTime = updatedTime;
            fi.LastWriteTime = updatedTime;

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckCopyUpToDateMarkersValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }
        #endregion

        #region OutputsAreValid Tests...
        [TestMethod]
        public void TestCheckOutputsAreValid_MissingOutputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(filesToCreate: new string[]{ "SomeSource.cs" }, outputBinaries: true);

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckOutputsAreValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }


        [TestMethod]
        public void TestCheckOutputsAreValid_ExistingOutputIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                filesToCreate: new string[] { "SomeSource.cs" }, 
                outputBinaries: true, 
                createStandardOutputs: true);

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckOutputsAreValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsTrue(isUpToDate);
        }

        [TestMethod]
        public void TestCheckOutputsAreValid_OldOutputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(filesToCreate: new string[] { "SomeSource.cs" }, outputBinaries: true, createStandardOutputs: true);

            // Update our inputs now that the outputs have been created.
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(projectFile), "SomeSource.cs"), "Updated!");

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckOutputsAreValid() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }
        #endregion

        #region UpToDateCheckBuiltItems Tests...

        [TestMethod]
        public void TestAreCopiedOutputFilesValid_MissingInputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><UpToDateCheckBuilt Include='$(OutputPath)/doesnotexist.js' Original='MyContent.js'/></ItemGroup>");

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckUpToDateCheckBuiltItems() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }

        [TestMethod]
        public void TestAreCopiedOutputFilesValid_MissingOutputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><UpToDateCheckBuilt Include='$(OutputPath)/doesnotexist.js' Original='MyContent.js'/></ItemGroup>",
                filesToCreate: new string[] { "MyContent.js" });

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckUpToDateCheckBuiltItems() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }

        [TestMethod]
        public void TestAreCopiedOutputFilesValid_ExistingOutputIsUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><UpToDateCheckBuilt Include='$(OutputPath)/exists.js' Original='MyContent.js'/></ItemGroup>",
                filesToCreate: new string[] { "MyContent.js", "bin\\Debug\\net472\\exists.js" }); // Files are created in order. So the "destination" file will be newest.

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckUpToDateCheckBuiltItems() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsTrue(isUpToDate);
        }

        [TestMethod]
        public void TestAreCopiedOutputFilesValid_ExistingOutputIsNotUpToDate()
        {
            string projectFile = TestUtilities.CreateTestProject(
                rawMsBuildXmlToInsert: "<ItemGroup><UpToDateCheckBuilt Include='$(OutputPath)/exists.js' Original='MyContent.js'/></ItemGroup>",
                filesToCreate: new string[] { "bin\\Debug\\exists.js", "MyContent.js" }); // Files are created in order. So the "source" file will be newest.

            var checks = new Mock<IBuildCheckProvider>(MockBehavior.Strict);
            checks.Setup(c => c.GetBuildChecks()).Returns(new IBuildCheck[] { new CheckUpToDateCheckBuiltItems() });

            bool isUpToDate = ProjectAnalyzerIsUpToDateCall(
                projectFile,
                logger: new Mock<ILogger>().Object,
                checks: checks.Object);

            Assert.IsFalse(isUpToDate);
        }
        #endregion

        private static bool ProjectAnalyzerIsUpToDateCall(string projectFile, ILogger logger = null, IDesignTimeBuildRunner runner = null, IBuildCheckProvider checks = null, bool cleanUp = true)
        {
            try
            {
                var usedLogger = logger ?? new ConsoleLogger(false);

                ProjectAnalyzer analyzer = new ProjectAnalyzer(
                    usedLogger,
                    runner ?? new DesignTimeBuildRunner(usedLogger, null),
                    checks ?? new BuildCheckProvider());

                (bool isUpToDate, string failureMessage) = analyzer.IsBuildUpToDate(projectFile);
                return isUpToDate;
            }
            finally
            {
                if (cleanUp)
                    TestUtilities.CleanUpTestProject(projectFile);
            }
        }
    }
}
