// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildUpToDateChecker
{
    [ExcludeFromCodeCoverage]
    internal static class Utilities
    {
        public static DateTime? GetTimestampUtc(string path, IDictionary<string, DateTime> timestampCache)
        {
            // If already in cache, return that value.
            if (timestampCache.TryGetValue(path, out DateTime time)) return time;

            // If the file doesn't exist, return null.
            if (!FileExists(path)) return null;

            // Get the last write timestamp from either this file or, if a symlink, the symlink target.
            time = File.GetLastWriteTimeUtc(IsSymLink(path) ? GetTargetOfSymlink(path) : path);
            timestampCache[path] = time;

            return time;
        }

        public static bool FileExists(string filePath)
        {
            // If the file doesn't exist, it doesn't matter if it's supposed to be a regular file or a symlink.
            if (!File.Exists(filePath)) return false;

            // If it's not a symlink or if the target of the symlink exists, return true.
            return !IsSymLink(filePath) || File.Exists(GetTargetOfSymlink(filePath));
        }

        private static bool IsSymLink(string filePath)
        {
            FileAttributes attr = File.GetAttributes(filePath);
            return ((attr & FileAttributes.ReparsePoint) != 0);
        }

        private static string GetTargetOfSymlink(string filePath)
        {
            IntPtr h = NativeMethods.CreateFile(filePath,
                NativeMethods.FILE_READ_EA,
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (h == NativeMethods.INVALID_HANDLE_VALUE)
            {
                throw new Win32Exception($"Unable to open file '{filePath}'.");
            }

            try
            {
                var sb = new StringBuilder(1024);
                var res = NativeMethods.GetFinalPathNameByHandle(h, sb, 1024, 0);

                if (res == 0)
                {
                    throw new Win32Exception((int)res, $"Unable to get target from symlink '{filePath}'.");
                }

                return sb.ToString();
            }
            finally
            {
                NativeMethods.CloseHandle(h);
            }
        }
    }
}
