using Xunit;
using NotepadEx.Services;
using NotepadEx.MVVM.Models;
using System.IO;
using System.Threading.Tasks;

namespace NotepadEx.Tests.Services
{
    public class DocumentServiceTests
    {
        [Fact]
        public async Task LoadDocumentAsync_ShouldReadContentCorrectly()
        {
            // Arrange
            var service = new DocumentService();
            var document = new Document();
            var tempFile = Path.GetTempFileName();
            var expectedContent = "Hello, World!";
            await File.WriteAllTextAsync(tempFile, expectedContent);

            try
            {
                // Act
                await service.LoadDocumentAsync(tempFile, document);

                // Assert
                Assert.Equal(expectedContent, document.Content);
                Assert.Equal(tempFile, document.FilePath);
                Assert.False(document.IsModified);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task SaveDocumentAsync_ShouldWriteContentCorrectly()
        {
            // Arrange
            var service = new DocumentService();
            var document = new Document
            {
                Content = "This is a test.",
                FilePath = Path.GetTempFileName()
            };

            try
            {
                // Act
                await service.SaveDocumentAsync(document);
                var actualContent = await File.ReadAllTextAsync(document.FilePath);

                // Assert
                Assert.Equal(document.Content, actualContent);
                Assert.False(document.IsModified);
            }
            finally
            {
                File.Delete(document.FilePath);
            }
        }

        [Fact]
        public async Task LoadDocumentAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var service = new DocumentService();
            var document = new Document();
            var nonExistentFile = "C:\\non_existent_file_12345.tmp";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.LoadDocumentAsync(nonExistentFile, document));
        }
    }
}