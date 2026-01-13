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
        // FIX: Test name updated to reflect the new method name
        public async Task LoadDocumentContentAsync_ShouldReadContentCorrectly()
        {
            // Arrange
            var service = new DocumentService();
            var tempFile = Path.GetTempFileName();
            var expectedContent = "Hello, World!";
            await File.WriteAllTextAsync(tempFile, expectedContent);

            try
            {
                // Act
                // FIX: Call the method with one argument and capture the returned string
                var actualContent = await service.LoadDocumentContentAsync(tempFile);

                // Assert
                // FIX: Assert that the returned string is correct.
                // The service is no longer responsible for modifying the Document object.
                Assert.Equal(expectedContent, actualContent);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task SaveDocumentAsync_ShouldWriteContentCorrectly()
        {
            // This test was already correct as the SaveDocumentAsync signature did not change.
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
        // FIX: Test name updated to reflect the new method name
        public async Task LoadDocumentContentAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var service = new DocumentService();
            var nonExistentFile = "C:\\non_existent_file_12345.tmp";

            // Act & Assert
            // FIX: Call the method with only one argument in the lambda
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.LoadDocumentContentAsync(nonExistentFile));
        }
    }
}