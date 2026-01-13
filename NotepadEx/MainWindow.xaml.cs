using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
        private WindowChrome _windowChrome;

        public MainWindow()
        {
            InitializeComponent();

            var windowService = new WindowService(this);
            var documentService = new DocumentService();
            var themeService = new ThemeService(Application.Current);
            var fontService = new FontService(Application.Current);
            fontService.LoadCurrentFont();

            ApplyAvalonEditTheme();

            themeService.ThemeChanged += (s, e) => ApplyAvalonEditTheme();

            Settings.Default.MenuBarAutoHide = false;

            DataContext = viewModel = new MainWindowViewModel(windowService, documentService, themeService, fontService, MenuItemFileDropDown, textEditor, () => SettingsManager.SaveSettings(this, textEditor, themeService.CurrentThemeName));
            viewModel.TitleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "NotepadEx", onClose: Application.Current.Shutdown);

            InitializeWindowEvents();

            // Add Ctrl+S keyboard shortcut
            this.KeyDown += MainWindow_KeyDown;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowChrome = new WindowChrome(this);
            _windowChrome.Enable();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+S for save
            if(e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = viewModel.SaveDocument();
            }
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

            Closing += (s, e) =>
            {
                viewModel.Cleanup();
                _windowChrome?.Detach();
            };
            Closed += (s, e) => viewModel.PromptToSaveChanges();
        }

        public void Dispose() => viewModel?.Cleanup();
    }
}