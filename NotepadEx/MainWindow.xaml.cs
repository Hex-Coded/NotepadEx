using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Properties;
using NotepadEx.Services;
using NotepadEx.Util;
using Brush = System.Windows.Media.Brush;

namespace NotepadEx
{
    public partial class MainWindow : Window, IDisposable
    {
        readonly MainWindowViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var windowService = new WindowService(this);
            var documentService = new DocumentService();
            var themeService = new ThemeService(Application.Current);
            var fontService = new FontService(Application.Current);
            fontService.LoadCurrentFont();

            // ** THE FIX: Apply theme brushes after the theme service is initialized. **
            ApplyAvalonEditTheme();
            // We also need to re-apply it whenever the theme changes.
            // A simple way is to subscribe to the ThemeEditorWindow's Closing event,
            // but for a robust solution, the ThemeService should raise an event.
            // For now, we will add a helper to re-apply.
            themeService.ThemeChanged += (s, e) => ApplyAvalonEditTheme();


            Settings.Default.MenuBarAutoHide = false;

            DataContext = viewModel = new MainWindowViewModel(windowService, documentService, themeService, fontService, MenuItemFileDropDown, textEditor, () => SettingsManager.SaveSettings(this, textEditor, themeService.CurrentThemeName));
            viewModel.TitleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "NotepadEx", onClose: Application.Current.Shutdown);

            InitializeWindowEvents();
        }

        private void ApplyAvalonEditTheme()
        {
            textEditor.TextArea.SelectionBrush = (Brush)FindResource("Color_TextEditorTextHighlight");
            textEditor.TextArea.Caret.CaretBrush = (Brush)FindResource("Color_TextEditorCaret");
        }

        void InitializeWindowEvents()
        {
            StateChanged += (s, e) =>
            {
                if(WindowState != WindowState.Minimized)
                    viewModel.UpdateWindowState(WindowState);
            };

            Closing += (s, e) => viewModel.Cleanup();
            Closed += (s, e) => viewModel.PromptToSaveChanges();
        }

        public void Dispose() => viewModel?.Cleanup();
    }
}