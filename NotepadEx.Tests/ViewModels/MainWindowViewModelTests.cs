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

            _textEditor = new TextEditor { Document = new TextDocument() };

            // FIX: No more complex mocking needed. Just create a real MenuItem to act as the container.
            var openRecentContainer = new MenuItem();

            Settings.Default.RecentFiles = string.Empty;
            Settings.Default.Save();

            _viewModel = new MainWindowViewModel(
                _mockWindowService.Object,
                _mockDocumentService.Object,
                _mockThemeService.Object,
                _mockFontService.Object,
                openRecentContainer, // FIX: Pass the simple MenuItem
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

        [StaFact]
        public async Task SaveDocument_WhenSaved_RemovesAsteriskFromTitle()
        {
            _viewModel.DocumentContent = "some new text";
            _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>()))
                .Callback<Document>(d => d.IsModified = false)
                .Returns(Task.FromResult(true));

            await _viewModel.SaveDocument();

            Assert.DoesNotContain("*", _viewModel.TitleBarViewModel.TitleText);
        }

        [StaFact]
        public async Task PromptToSaveChanges_WhenUserClicksYes_CallsSaveDocumentAndReturnsTrue()
        {
            _viewModel.DocumentContent = "unsaved changes";
            _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns(MessageBoxResult.Yes);
            _mockDocumentService.Setup(s => s.SaveDocumentAsync(It.IsAny<Document>())).Returns(Task.FromResult(true));

            var result = await _viewModel.PromptToSaveChanges();

            Assert.True(result);
            _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Once);
        }

        [StaFact]
        public async Task PromptToSaveChanges_WhenUserClicksNo_ReturnsTrueWithoutSaving()
        {
            _viewModel.DocumentContent = "unsaved changes";
            _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns(MessageBoxResult.No);

            var result = await _viewModel.PromptToSaveChanges();

            Assert.True(result);
            _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Never);
        }

        [StaFact]
        public async Task PromptToSaveChanges_WhenUserClicksCancel_ReturnsFalse()
        {
            _viewModel.DocumentContent = "unsaved changes";
            _mockWindowService.Setup(w => w.ShowSaveConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()))
                              .Returns(MessageBoxResult.Cancel);

            var result = await _viewModel.PromptToSaveChanges();

            Assert.False(result);
            _mockDocumentService.Verify(s => s.SaveDocumentAsync(It.IsAny<Document>()), Times.Never);
        }
    }
}