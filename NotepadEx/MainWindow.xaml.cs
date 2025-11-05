using System;
using System.Windows;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Properties;
using NotepadEx.Services;

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

            Settings.Default.MenuBarAutoHide = false;

            DataContext = viewModel = new MainWindowViewModel(windowService, documentService, themeService, fontService, MenuItemFileDropDown, textEditor, () => SettingsManager.SaveSettings(this, textEditor, themeService.CurrentThemeName));
            viewModel.TitleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "NotepadEx", onClose: Application.Current.Shutdown);

            InitializeWindowEvents();
        }

        void InitializeWindowEvents()
        {
            StateChanged += (s, e) =>
            {
                if(WindowState != WindowState.Minimized)
                    viewModel.UpdateWindowState(WindowState);
            };

            // Register the Cleanup method to be called when the window is closing
            Closing += (s, e) => viewModel.Cleanup();
            Closed += (s, e) => viewModel.PromptToSaveChanges();
        }

        public void Dispose() => viewModel?.Cleanup();
    }
}