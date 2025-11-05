using NotepadEx.Properties;
using NotepadEx.Util;
using System.Collections.Generic;
using System.Linq;

namespace NotepadEx.Util
{
    public static class RecentFileManager
    {
        private const int MaxRecentsToTrack = 20;

        /// <summary>
        /// Atomically adds a file path to the recent files list, ensuring process safety.
        /// </summary>
        public static void AddFile(string filePath)
        {
            if(string.IsNullOrEmpty(filePath))
            {
                return;
            }

            ProcessSync.RunSynchronized(() =>
            {
                var recentFiles = GetRecentFilesFromSettings();
                recentFiles.Remove(filePath);
                recentFiles.Insert(0, filePath);

                if(recentFiles.Count > MaxRecentsToTrack)
                {
                    recentFiles.RemoveAt(recentFiles.Count - 1);
                }

                Settings.Default.RecentFiles = string.Join(",", recentFiles);
                Settings.Default.Save();
            });
        }

        /// <summary>
        /// Atomically removes a file path from the recent files list, ensuring process safety.
        /// </summary>
        public static void RemoveFile(string filePath)
        {
            if(string.IsNullOrEmpty(filePath))
            {
                return;
            }

            ProcessSync.RunSynchronized(() =>
            {
                var recentFiles = GetRecentFilesFromSettings();
                // Check if the file was actually removed to avoid an unnecessary save.
                if(recentFiles.Remove(filePath))
                {
                    Settings.Default.RecentFiles = string.Join(",", recentFiles);
                    Settings.Default.Save();
                }
            });
        }

        /// <summary>
        /// Atomically retrieves the current list of recent files.
        /// </summary>
        public static List<string> GetRecentFiles()
        {
            List<string> recentFiles = new List<string>();
            ProcessSync.RunSynchronized(() =>
            {
                recentFiles = GetRecentFilesFromSettings();
            });
            return recentFiles;
        }

        private static List<string> GetRecentFilesFromSettings()
        {
            string recentFilesString = Settings.Default.RecentFiles;
            if(!string.IsNullOrEmpty(recentFilesString))
            {
                return recentFilesString.Split(',').ToList();
            }
            return new List<string>();
        }
    }
}