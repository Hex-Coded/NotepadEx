using Xunit;
using NotepadEx.Util;
using NotepadEx.Properties;
using System.Linq;

namespace NotepadEx.Tests.Utils
{
    public class RecentFileManagerTests : IDisposable
    {
        public RecentFileManagerTests()
        {
            // Reset settings before each test
            Settings.Default.RecentFiles = string.Empty;
            Settings.Default.Save();
        }

        [Fact]
        public void AddFile_WhenListIsEmpty_AddsFile()
        {
            // Arrange
            var filePath = "C:\\test1.txt";

            // Act
            RecentFileManager.AddFile(filePath);
            var recents = RecentFileManager.GetRecentFiles();

            // Assert
            Assert.Single(recents);
            Assert.Equal(filePath, recents.First());
        }

        [Fact]
        public void AddFile_WhenFileExists_MovesToTop()
        {
            // Arrange
            var filePath1 = "C:\\test1.txt";
            var filePath2 = "C:\\test2.txt";
            var filePath3 = "C:\\test3.txt";
            RecentFileManager.AddFile(filePath3);
            RecentFileManager.AddFile(filePath2);
            RecentFileManager.AddFile(filePath1);

            // Act: Re-add the middle file
            RecentFileManager.AddFile(filePath2);
            var recents = RecentFileManager.GetRecentFiles();

            // Assert
            Assert.Equal(3, recents.Count);
            Assert.Equal(filePath2, recents[0]);
            Assert.Equal(filePath1, recents[1]);
            Assert.Equal(filePath3, recents[2]);
        }

        [Fact]
        public void AddFile_WhenListIsFull_RemovesOldest()
        {
            // Arrange
            // Add 20 files
            for(int i = 0; i < 20; i++)
            {
                RecentFileManager.AddFile($"C:\\file{i}.txt");
            }
            var newFile = "C:\\newFile.txt";

            // Act
            RecentFileManager.AddFile(newFile);
            var recents = RecentFileManager.GetRecentFiles();

            // Assert
            Assert.Equal(20, recents.Count);
            Assert.Equal(newFile, recents.First());
            Assert.DoesNotContain("C:\\file0.txt", recents); // The first one added should be gone
        }

        public void Dispose()
        {
            // Clean up settings after tests
            Settings.Default.RecentFiles = string.Empty;
            Settings.Default.Save();
        }
    }
}