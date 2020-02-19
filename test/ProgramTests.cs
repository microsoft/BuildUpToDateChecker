// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BuildUpToDateChecker.Tests
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void TestNoArgs()
        {
            int exitCode = Program.Main(Array.Empty<string>());
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void TestBadProjectPath()
        {
            int exitCode = Program.Main(new string[]{ ".\\myProject.csproj" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void TestBadMsBuildPath()
        {
            string tempFile = Path.GetTempFileName();
            int exitCode = Program.Main(new string[]{ tempFile, "--msbuild .\\not-a-valid-path\\msbuild.exe" });
            Assert.AreNotEqual(0, exitCode);
            File.Delete(tempFile);
        }

        #region MSBuild Assembly Resolution Tests...
        // NOTE: These tests work if run individually/manually but will fail since MSBuild assembly resolution
        // is only supposed to be set up once, and BEFORE any MSBuild assemblies are loaded.
        // This creates race conditions when running these tests so they are ignored by default.

        [TestMethod]
        [Ignore]
        public void TestMsBuildAssemblyResolutionWithBadMsBuildPath()
        {
            var logger = new TestLogger();

            string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "MSBuild.exe");

            Program.SetUpMsBuildAssemblyResolution(logger, nonExistentPath);

            string logText = logger.LogText;
            Assert.IsTrue(logText.Contains("Unable to find MSBuild at specified location"));
        }

        [TestMethod]
        [Ignore]
        public void TestMsBuildAssemblyResolutionWithGoodMsBuildPath()
        {
            var logger = new TestLogger();
            string path = Path.GetTempFileName();
            string usedPath = Program.SetUpMsBuildAssemblyResolution(logger, path);
            Assert.AreEqual(Path.GetDirectoryName(path), usedPath);
        }

        [TestMethod]
        [Ignore]
        public void TestMsBuildAssemblyResolutionInCoreXtBuild()
        {
            var logger = new TestLogger();
            string envVarNme = "MSBuildToolsPath_160";
            string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string currentValue = Environment.GetEnvironmentVariable(envVarNme) ?? string.Empty;

            Directory.CreateDirectory(dir);

            try
            {
                Environment.SetEnvironmentVariable(envVarNme, dir);
                string usedPath = Program.SetUpMsBuildAssemblyResolution(logger, null);
                Assert.AreEqual(dir, usedPath);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarNme, currentValue);
            }
        }

        #endregion

        [TestMethod]
        public void TestAdditionalMsBuildPropertiesWithNoAdditionalPropArguments()
        {
            var mockLogger = new Mock<ILogger>();
            IDictionary<string, string> properties = Program.GetAdditionalMsBuildProperties(mockLogger.Object, null);

            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("Configuration"));
            Assert.IsTrue(properties.ContainsKey("Platform"));
            Assert.AreEqual("debug", properties["Configuration"], true);
            Assert.AreEqual("AnyCPU", properties["Platform"]);
        }

        [TestMethod]
        public void TestAdditionalMsBuildPropertiesAreUsed()
        {
            var mockLogger = new Mock<ILogger>();
            IDictionary<string, string> properties = Program.GetAdditionalMsBuildProperties(mockLogger.Object, new []{ "foo=bar", "bar=baz"});

            Assert.IsNotNull(properties);

            // These should not have been removed.
            Assert.IsTrue(properties.ContainsKey("Configuration"));
            Assert.IsTrue(properties.ContainsKey("Platform"));
            Assert.AreEqual("debug", properties["Configuration"], true);
            Assert.AreEqual("AnyCPU", properties["Platform"]);

            // These should have been added.
            Assert.IsTrue(properties.ContainsKey("foo"));
            Assert.IsTrue(properties.ContainsKey("bar"));
            Assert.AreEqual("bar", properties["foo"]);
            Assert.AreEqual("baz", properties["bar"]);
        }

        [TestMethod]
        public void TestAdditionalMsBuildPropertiesCanOverrideTheDefaults()
        {
            var mockLogger = new Mock<ILogger>();
            IDictionary<string, string> properties = Program.GetAdditionalMsBuildProperties(mockLogger.Object, new[] { "Configuration=Retail", "Platform=ARM" });

            Assert.IsNotNull(properties);

            // These should have been overridden.
            Assert.IsTrue(properties.ContainsKey("Configuration"));
            Assert.IsTrue(properties.ContainsKey("Platform"));
            Assert.AreEqual("Retail", properties["Configuration"]);
            Assert.AreEqual("ARM", properties["Platform"]);
        }

        [TestMethod]
        public void TestRun()
        {
            Options options = new Options()
            {
                InputProjectFile = Path.GetTempFileName()
            };

            Program p = new Program(options);

            // Just test that the plumbing is hooked up and the GraphAnalyzer attempts to do its thing.
            try
            {
                p.Run();
            }
            catch (Exception) { }
        }
    }
}
