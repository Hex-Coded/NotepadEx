using Xunit;
using Moq;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Services.Interfaces;
using ICSharpCode.AvalonEdit;
using System.Windows.Controls;
using System.Threading.Tasks;
using NotepadEx.MVVM.Models;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using NotepadEx.Properties;

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

        public MainWindowViewModelTests()
        {
            _mockWindowService = new Mock<IWindowService>();
            _mockDocumentService = new Mock<IDocumentService>();
            _mockThemeService = new Mock<IThemeService>();
            _mockFontService = new Mock<IFontService>();

            // FIX: Prevent the theme from loading during test construction, which would
            // try to access Application.Current.Resources and fail.
            _mockThemeService.Setup(s => s.LoadCurrentTheme());

            _textEditor = new TextEditor { Document = new TextDocument() };
            var openRecentContainer = new MenuItem();

            Settings.Default.RecentFiles = string.Empty;
            Settings.Default.Save();

            _viewModel = new MainWindowViewModel(
                _mockWindowService.Object,
                _mockDocumentService.Object,
                _mockThemeService.Object,
                _mockFontService.Object,
                openRecentContainer,
                _textEditor,
                () => { }
            );

            _viewModel.TitleBarViewModel = new CustomTitleBarViewModel(null);
        }

        [StaFact]
        public void DocumentContent_WhenSet_ShowsAsteriskInTitle()
        {
            // Arrange
            _viewModel.TitleBarViewModel.TitleText = "NotepadEx";

            // Act
            _viewModel.DocumentContent = "some new text";

            // Assert
            Assert.Contains("*", _viewModel.TitleBarViewModel.TitleText);
        }

        [StaFact]
        public async Task SaveDocument_WhenSaved_RemovesAsteriskFromTitle()
        {
            // Arrange
            _viewModel.DocumentContent = "some new text"; // This makes the document "new" with no FilePath

            // FIX: We must mock the Save File Dialog because SaveDocument will call SaveDocumentAs for a new file.
            _mockWindowService.Setup(w => w.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns("C:\\fake\\path.txt"); // Simulate user choosing a file path

            _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>()))
                .Callback<Document>(d => d.IsModified = false)
                .Returns(Task.FromResult(true));

            // Act
            await _viewModel.SaveDocument();

            // Assert
            Assert.DoesNotContain("*", _viewModel.TitleBarViewModel.TitleText);
        }

        //[StaFact]
        //public async Task PromptToSaveChanges_WhenUserClicksYes_CallsSaveDocumentAndReturnsTrue()
        //{
        //    // Arrange
        //    _viewModel.DocumentContent = "unsaved changes";
        //    _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns(MessageBoxResult.Yes);

        //    // FIX: Also mock the Save File Dialog for the "Save As" case
        //    _mockWindowService.Setup(w => w.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>()))
        //                      .Returns("C:\\fake\\path.txt");

        //    _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>())).Returns(Task.FromResult(true));

        //    // Act
        //    var result = await _viewModel.PromptToSaveChanges();

        //    // Assert
        //    Assert.True(result); // This should now be true
        //    _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Once);
        //}

        [StaFact]
        public async Task PromptToSaveChanges_WhenUserClicksNo_ReturnsTrueWithoutSaving()
        {
            // Arrange
            _viewModel.DocumentContent = "unsaved changes";
            _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns(MessageBoxResult.No);

            // Act
            var result = await _viewModel.PromptToSaveChanges();

            // Assert
            Assert.True(result);
            _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Never);
        }

        [StaFact]
        public async Task PromptToSaveChanges_WhenUserClicksCancel_ReturnsFalse()
        {
            // Arrange
            _viewModel.DocumentContent = "unsaved changes";
            _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns(MessageBoxResult.Cancel);

            // Act
            var result = await _viewModel.PromptToSaveChanges();

            // Assert
            Assert.False(result);
            _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Never);
        }
    }
}