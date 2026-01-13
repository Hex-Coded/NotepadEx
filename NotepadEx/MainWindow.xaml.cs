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
        private bool _isClosingForReal = false;

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
            viewModel.TitleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "NotepadEx", onClose: this.Close);

            LoadWindowSettings();
            InitializeWindowEvents();

            this.KeyDown += MainWindow_KeyDown;
            Loaded += MainWindow_Loaded;
        }

        private void LoadWindowSettings()
        {
            ProcessSync.RunSynchronized(() =>
            {
                if(Settings.Default.WindowSizeX > 100 && Settings.Default.WindowSizeY > 100)
                {
                    this.Width = Settings.Default.WindowSizeX;
                    this.Height = Settings.Default.WindowSizeY;
                }

                if(!string.IsNullOrEmpty(Settings.Default.WindowState) &&
                    Enum.TryParse(Settings.Default.WindowState, out WindowState parsedState))
                {
                    this.WindowState = parsedState == WindowState.Minimized ? WindowState.Normal : parsedState;
                }
            });
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowChrome = new WindowChrome(this);
            _windowChrome.Enable();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
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

        // FIX: The entire Closing event handler is replaced with this robust version.
        void InitializeWindowEvents()
        {
            StateChanged += (s, e) =>
            {
                if(WindowState != WindowState.Minimized)
                    viewModel.UpdateWindowState(WindowState);
            };

            Closing += async (s, e) =>
            {
                // If the flag is set, it means we've already confirmed with the user
                // and are now closing for real. The cleanup happens here, and we
                // let the event proceed by NOT cancelling it.
                if(_isClosingForReal)
                {
                    viewModel.Cleanup();
                    _windowChrome?.Detach();
                    return;
                }

                // For the FIRST attempt to close, we ALWAYS cancel it immediately.
                // This gives our async prompt time to run without the window disappearing.
                e.Cancel = true;

                // Now, ask the user if they want to save changes.
                bool canClose = await viewModel.PromptToSaveChanges();

                // If the user didn't click "Cancel" in the prompt...
                if(canClose)
                {
                    // Set the flag to true.
                    _isClosingForReal = true;

                    // Re-initiate the close operation. Because we are using the Dispatcher,
                    // this call will be queued to run *after* the current Closing event handler
                    // has finished. This avoids the InvalidOperationException.
                    // When this new Close() is called, the 'if (_isClosingForReal)' block above
                    // will execute, and the window will close cleanly.
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => this.Close()));
                }
                // If the user clicked "Cancel" (canClose is false), we do nothing.
                // The window stays open because we set e.Cancel = true at the beginning.
            };
        }

        public void Dispose() => viewModel?.Cleanup();
    }
}