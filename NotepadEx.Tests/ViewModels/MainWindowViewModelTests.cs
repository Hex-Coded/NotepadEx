using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Moq;
using NotepadEx.MVVM.Models;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Properties;
using NotepadEx.Services.Interfaces;
using NotepadEx.Util;
using Xunit;

namespace NotepadEx.Tests.ViewModels
{
    public class MainWindowViewModelTests
    {
        private readonly Mock<IWindowService> _mockWindowService;
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly Mock<IThemeService> _mockThemeService;
        private readonly Mock<IFontService> _mockFontService;
        private readonly TextEditor _textEditor;
        private readonly MainWindowViewModel _viewModel;
        private readonly MenuItem _openRecentContainer;

        public MainWindowViewModelTests()
        {
            _mockWindowService = new Mock<IWindowService>();
            _mockDocumentService = new Mock<IDocumentService>();
            _mockThemeService = new Mock<IThemeService>();
            _mockFontService = new Mock<IFontService>();

            _mockThemeService.Setup(s => s.LoadCurrentTheme());

            _textEditor = new TextEditor { Document = new TextDocument() };
            _openRecentContainer = new MenuItem();

            // Clear settings before each test to ensure isolation
            Settings.Default.RecentFiles = string.Empty;
            Settings.Default.Save();

            _viewModel = new MainWindowViewModel(
                _mockWindowService.Object,
                _mockDocumentService.Object,
                _mockThemeService.Object,
                _mockFontService.Object,
                _openRecentContainer,
                _textEditor,
                () => { }
            );

            _viewModel.TitleBarViewModel = new CustomTitleBarViewModel(null);
        }

        [StaFact]
        public void DocumentContent_WhenSet_ShowsAsteriskInTitle()
        {
            _viewModel.TitleBarViewModel.TitleText = "NotepadEx";
            _viewModel.DocumentContent = "some new text";
            Assert.Contains("*", _viewModel.TitleBarViewModel.TitleText);
        }

        //[StaFact]
        //public async Task SaveDocument_WhenSaved_RemovesAsteriskFromTitle()
        //{
        //    _viewModel.DocumentContent = "some new text";
        //    _mockWindowService.Setup(w => w.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns("C:\\fake\\path.txt");
        //    _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>()))
        //        .Callback<Document>(d => d.IsModified = false)
        //        .Returns(Task.FromResult(true));

        //    await _viewModel.SaveDocument();

        //    Assert.DoesNotContain("*", _viewModel.TitleBarViewModel.TitleText);
        //}

        //[StaFact]
        //public async Task PromptToSaveChanges_WhenUserClicksYes_CallsSaveDocumentAndReturnsTrue()
        //{
        //    _viewModel.DocumentContent = "unsaved changes";
        //    _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns(MessageBoxResult.Yes);
        //    _mockWindowService.Setup(w => w.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns("C:\\fake\\path.txt");
        //    _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>())).Returns(Task.FromResult(true));

        //    var result = await _viewModel.PromptToSaveChanges();

        //    Assert.True(result);
        //    _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Once);
        //}



        // --- NEW REGRESSION AND FEATURE TESTS ---

        [StaFact]
        [Trait("Regression", "Asterisk")]
        public async Task LoadDocument_ShouldNotShowAsteriskInTitle()
        {
            // This test verifies the fix for the bug where an asterisk incorrectly appeared on file load.
            // Arrange
            _mockDocumentService.Setup(s => s.LoadDocumentContentAsync(It.IsAny<string>()))
                                .ReturnsAsync("file content");

            // Act
            await _viewModel.OpenRecentFile("C:\\somefile.txt");

            // Assert
            Assert.DoesNotContain("*", _viewModel.TitleBarViewModel.TitleText);
        }

        [StaFact]
        [Trait("Regression", "Content Loading")]
        public async Task LoadDocument_ShouldUpdateDocumentContent()
        {
            // This test verifies the fix for the bug where file content wasn't displayed after opening.
            // Arrange
            var expectedContent = "Hello from the test file!";
            _mockDocumentService.Setup(s => s.LoadDocumentContentAsync(It.IsAny<string>()))
                                .ReturnsAsync(expectedContent);

            // Act
            await _viewModel.OpenRecentFile("C:\\anyfile.txt");

            // Assert
            Assert.Equal(expectedContent, _viewModel.DocumentContent);
        }

        //[StaFact]
        //[Trait("Feature", "Recent Files")]
        //public async Task SaveDocumentAs_OnSuccessfulSave_AddsFileToRecents()
        //{
        //    // This test verifies the new feature: saving a file adds it to the recent files list.
        //    // Arrange
        //    var savedFilePath = "C:\\savedfile.txt";
        //    _viewModel.DocumentContent = "new content"; // Mark as dirty
        //    _mockWindowService.Setup(w => w.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns(savedFilePath);
        //    _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>()))
        //                        .Returns(Task.FromResult(true));

        //    // Act
        //    await _viewModel.SaveDocument(); // This will trigger SaveDocumentAs internally
        //    var recents = RecentFileManager.GetRecentFiles();

        //    // Assert
        //    Assert.Single(recents);
        //    Assert.Equal(savedFilePath, recents.First());
        //}

        //[StaFact]
        //public async Task PromptToSaveChanges_WhenUserClicksNo_ReturnsTrueWithoutSaving()
        //{
        //    _viewModel.DocumentContent = "unsaved changes";
        //    _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns(MessageBoxResult.No);

        //    var result = await _viewModel.PromptToSaveChanges();

        //    Assert.True(result);
        //    _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Never);
        //}

        //[StaFact]
        //public async Task PromptToSaveChanges_WhenUserClicksCancel_ReturnsFalse()
        //{
        //    _viewModel.DocumentContent = "unsaved changes";
        //    _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns(MessageBoxResult.Cancel);

        //    var result = await _viewModel.PromptToSaveChanges();

        //    Assert.False(result);
        //    _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Never);
        //}

        [StaFact]
        [Trait("Edge Case", "New Document")]
        public void NewDocument_WithUnsavedChangesAndUserClicksNo_ClearsState()
        {
            // This test verifies that the "New" command works correctly when there are unsaved changes.
            // Arrange
            _viewModel.DocumentContent = "dirty text";
            _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns(MessageBoxResult.No);

            // Act
            // Note: We cast to ICommand to ensure we are testing the public interface.
            (_viewModel.NewCommand as ICommand).Execute(null);

            // Assert
            Assert.Equal(string.Empty, _viewModel.DocumentContent);
            Assert.DoesNotContain("*", _viewModel.TitleBarViewModel.TitleText);
        }
    }
}