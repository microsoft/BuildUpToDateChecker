// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BuildUpToDateChecker.Tests
{
    internal class TestUtilities
    {
        public static string CreateTestProject(
            string rawMsBuildXmlToInsert = null,
            IEnumerable<string> filesToCreate = null,
            bool outputBinaries = false,
            bool createStandardOutputs = false)
        {
            string newDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(newDir);

            string projectName = Guid.NewGuid().ToString();
            string newProjectFile = Path.Combine(newDir, $"{projectName}.csproj");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<Project Sdk='Microsoft.NET.Sdk'>");

            sb.AppendLine("<PropertyGroup>");
            sb.AppendLine("<TargetFramework>net472</TargetFramework>");
            if (outputBinaries)
            {
                sb.AppendLine("<OutputType>Exe</OutputType>");
            }
            sb.AppendLine("</PropertyGroup>");

            // Add desired raw XML.
            if (rawMsBuildXmlToInsert != null)
                sb.AppendLine(rawMsBuildXmlToInsert);

            sb.AppendLine("</Project>");
            
            File.WriteAllText(newProjectFile, sb.ToString());

            if (createStandardOutputs)
            {
                List<string> standardOutputs = new List<string>();
                standardOutputs.Add($"bin\\Debug\\net472\\{projectName}.exe");
                standardOutputs.Add($"bin\\Debug\\net472\\{projectName}.pdb");
                standardOutputs.Add($"obj\\Debug\\net472\\{projectName}.exe");
                standardOutputs.Add($"obj\\Debug\\net472\\{projectName}.pdb");

                filesToCreate = filesToCreate == null ? standardOutputs : filesToCreate.Concat(standardOutputs);
            }

            // Create any desired files.
            if (filesToCreate != null)
            {
                foreach (string fileToCreate in filesToCreate)
                {
                    string filePath = Path.Combine(newDir, fileToCreate);
                    string fileDir = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(fileDir))
                        Directory.CreateDirectory(fileDir);

                    File.WriteAllText(filePath, "// Test");
                }
            }

            return newProjectFile;
        }

        public static void CleanUpTestProject(string projectFile)
        {
            if (string.IsNullOrEmpty(projectFile)) return;
            if (!File.Exists(projectFile)) return;

            string dir = Path.GetDirectoryName(projectFile);
            Directory.Delete(dir, true);
        }
    }
}
