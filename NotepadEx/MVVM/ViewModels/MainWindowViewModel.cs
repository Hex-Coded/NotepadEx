using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using NotepadEx.MVVM.Models;
using NotepadEx.MVVM.View;
using NotepadEx.Properties;
using NotepadEx.Services.Interfaces;
using NotepadEx.Util;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace NotepadEx.MVVM.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly Document document;
        private readonly IWindowService windowService;
        private readonly IDocumentService documentService;
        private readonly IThemeService themeService;
        private readonly IFontService fontService;
        private readonly TextEditor textEditor;
        private readonly MenuItem menuItemFileDropdown;
        private readonly Action SaveSettings;
        private FindAndReplaceWindow findAndReplaceWindow;
        private string statusText;
        private double menuBarHeight;
        private double infoBarHeight;
        private bool isMenuBarEnabled;
        private bool isWordWrapEnabled;
        private bool isInfoBarVisible;
        private bool showLineNumbers;
        private IHighlightingDefinition currentSyntaxHighlighting;

        public ICommand NewCommand { get; private set; }
        public ICommand OpenCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand SaveAsCommand { get; private set; }
        public ICommand PrintCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand ToggleWordWrapCommand { get; private set; }
        public ICommand ToggleInfoBarCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand CutCommand { get; private set; }
        public ICommand PasteCommand { get; private set; }
        public ICommand ChangeThemeCommand { get; private set; }
        public ICommand OpenThemeEditorCommand { get; private set; }
        public ICommand OpenFontEditorCommand { get; private set; }
        public ICommand OpenFindReplaceCommand { get; private set; }
        public ICommand OpenFileLocationCommand { get; private set; }
        public ICommand MouseMoveCommand { get; private set; }
        public ICommand ResizeCommand { get; private set; }
        public ICommand OpenRecentCommand { get; private set; }
        public ICommand ChangeSyntaxHighlightingCommand { get; private set; }

        public ObservableCollection<ThemeInfo> AvailableThemes => themeService.AvailableThemes;
        public ObservableCollection<IHighlightingDefinition> AvailableSyntaxHighlightings { get; }

        public MainWindowViewModel(IWindowService windowService, IDocumentService documentService, IThemeService themeService, IFontService fontService, MenuItem menuItemFileDropdown, TextEditor textEditor, Action SaveSettings)
        {
            this.windowService = windowService;
            this.documentService = documentService;
            this.themeService = themeService;
            this.fontService = fontService;
            this.menuItemFileDropdown = menuItemFileDropdown;
            this.textEditor = textEditor;
            this.SaveSettings = SaveSettings;

            document = new Document();
            this.textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

            // ** THIS IS THE FIX **
            // Manually construct the syntax highlighting list with sorting and a "None" option.
            var sortedHighlightings = HighlightingManager.Instance.HighlightingDefinitions
                .OrderBy(h => h.Name).ToList();

            // AvalonEdit uses a null definition for plain text. We create a placeholder for the menu.
            // Using a simple custom class makes the XAML binding work cleanly.
            var plainTextHighlighting = new PlainTextHighlightingDefinition();
            sortedHighlightings.Insert(0, plainTextHighlighting);

            AvailableSyntaxHighlightings = new ObservableCollection<IHighlightingDefinition>(sortedHighlightings);

            InitializeCommands();
            UpdateMenuBarVisibility(Settings.Default.MenuBarAutoHide);

            IsInfoBarVisible = Settings.Default.InfoBarVisible;
            IsWordWrapEnabled = Settings.Default.TextWrapping;
            ShowLineNumbers = Settings.Default.ShowLineNumbers;

            // Set the default highlighting
            var savedHighlightingName = Settings.Default.SyntaxHighlightingName;
            CurrentSyntaxHighlighting = HighlightingManager.Instance.GetDefinition(savedHighlightingName)
                                        ?? (savedHighlightingName == "Plain Text" ? null : HighlightingManager.Instance.GetDefinition("C#"));

            this.themeService.LoadCurrentTheme();
            UpdateRecentFilesMenu();
            UpdateStatusBar();
            OnPropertyChanged(nameof(AvailableThemes));
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        void InitializeCommands()
        {
            NewCommand = new RelayCommand(NewDocument);
            OpenCommand = new RelayCommand(async () => await OpenDocument());
            SaveCommand = new RelayCommand(async () => await SaveDocument());
            SaveAsCommand = new RelayCommand(async () => await SaveDocumentAs());
            PrintCommand = new RelayCommand(PrintDocument);
            ToggleWordWrapCommand = new RelayCommand(ToggleWordWrap);
            ToggleInfoBarCommand = new RelayCommand(ToggleInfoBar);
            CopyCommand = new RelayCommand(Copy, () => textEditor.SelectionLength > 0);
            CutCommand = new RelayCommand(Cut, () => textEditor.SelectionLength > 0);
            PasteCommand = new RelayCommand(Paste, () => Clipboard.ContainsText());
            ChangeThemeCommand = new RelayCommand<ThemeInfo>(OnThemeChange);
            OpenThemeEditorCommand = new RelayCommand(OnOpenThemeEditor);
            OpenFontEditorCommand = new RelayCommand(OnOpenFontEditor);
            OpenFindReplaceCommand = new RelayCommand(OnOpenFindReplaceEditor);
            OpenFileLocationCommand = new RelayCommand(OpenFileLocation, () => !string.IsNullOrEmpty(document.FilePath));
            MouseMoveCommand = new RelayCommand<double>(HandleMouseMovement);
            ResizeCommand = new RelayCommand<Point>(p => HandleWindowResize(Application.Current.MainWindow, p));
            OpenRecentCommand = new RelayCommand<RoutedEventArgs>(HandleOpenRecent);
            ChangeSyntaxHighlightingCommand = new RelayCommand<IHighlightingDefinition>(def => CurrentSyntaxHighlighting = def);
        }


        private async Task LoadDocument(string filePath)
        {
            try
            {
                await documentService.LoadDocumentAsync(filePath, document);

                DocumentContent = document.Content;
                document.IsModified = false;

                UpdateTitle();
                UpdateStatusBar();
                AddRecentFile(filePath);
            }
            catch(Exception ex)
            {
                // ** THIS IS THE FIX **
                // Check if the error was because the file is missing.
                if(ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    windowService.ShowDialog($"The file could not be found:\n{filePath}\n\nIt will be removed from the recent files list.", "File Not Found");

                    // Atomically remove the bad entry and refresh the UI.
                    RecentFileManager.RemoveFile(filePath);
                    UpdateRecentFilesMenu();
                }
                else
                {
                    // For any other error, show a generic message.
                    windowService.ShowDialog($"Error loading file: {ex.Message}", "Error");
                }
            }
        }

        private void AddRecentFile(string filePath)
        {
            RecentFileManager.AddFile(filePath);
            UpdateRecentFilesMenu();
        }

        private void UpdateRecentFilesMenu()
        {
            var openRecentMenuItem = (MenuItem)menuItemFileDropdown.FindName("MenuItem_OpenRecent");
            if(openRecentMenuItem == null) return;

            var recentFiles = RecentFileManager.GetRecentFiles();
            var menuFgBrush = (Brush)Application.Current.FindResource("Color_MenuItemFg");

            openRecentMenuItem.Items.Clear();
            foreach(string file in recentFiles)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = file,
                    Foreground = menuFgBrush
                };
                openRecentMenuItem.Items.Add(menuItem);
            }
        }


        public string CurrentThemeName
        {
            get => Settings.Default.ThemeName;
            set
            {
                if(Settings.Default.ThemeName == value) return;
                Settings.Default.ThemeName = value;
                OnPropertyChanged();
            }
        }

        public string DocumentContent
        {
            get => document.Content;
            set
            {
                // ** FIX 1: The setter is now clean. Its only job is to set the value and notify. **
                // The early exit is removed to ensure OnPropertyChanged ALWAYS fires.
                document.Content = value;
                // The responsibility of setting IsModified is moved to the actions that cause it.
                OnPropertyChanged();
            }
        }

        public string StatusText { get => statusText; set => SetProperty(ref statusText, value); }
        public double MenuBarHeight { get => menuBarHeight; set => SetProperty(ref menuBarHeight, value); }
        public double InfoBarHeight { get => infoBarHeight; set => SetProperty(ref infoBarHeight, value); }
        public bool IsMenuBarEnabled { get => isMenuBarEnabled; set => SetProperty(ref isMenuBarEnabled, value); }
        public CustomTitleBarViewModel TitleBarViewModel { get; set; }

        public bool IsWordWrapEnabled
        {
            get => isWordWrapEnabled;
            set => SetProperty(ref isWordWrapEnabled, value);
        }

        public bool IsInfoBarVisible
        {
            get => isInfoBarVisible;
            set
            {
                if(SetProperty(ref isInfoBarVisible, value))
                {
                    InfoBarHeight = value ? UIConstants.InfoBarHeight : 0;
                }
            }
        }

        public bool ShowLineNumbers
        {
            get => showLineNumbers;
            set => SetProperty(ref showLineNumbers, value);
        }

        public IHighlightingDefinition CurrentSyntaxHighlighting
        {
            get => currentSyntaxHighlighting;
            set
            {
                if(value is PlainTextHighlightingDefinition)
                {
                    SetProperty(ref currentSyntaxHighlighting, null);
                }
                else
                {
                    SetProperty(ref currentSyntaxHighlighting, value);
                }
            }
        }


        private void OnOpenThemeEditor() => themeService.OpenThemeEditor();

        private void OnOpenFontEditor() => fontService.OpenFontEditor();

        private void OnOpenFindReplaceEditor()
        {
            findAndReplaceWindow ??= new FindAndReplaceWindow(textEditor);
            findAndReplaceWindow.Show();
            findAndReplaceWindow.Activate();
        }

        private void OnThemeChange(ThemeInfo theme)
        {
            if(theme == null) return;
            themeService.ApplyTheme(theme.Name);
            CurrentThemeName = theme.Name;
            themeService.AddEditableColorLinesToWindow();
        }

        private async Task OpenDocument()
        {
            if(!PromptToSaveChanges()) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            if(dialog.ShowDialog() != true) return;

            var fileInfo = new FileInfo(dialog.FileName);
            if(fileInfo.Length > 20 * 1024 * 1024) // 20 MB warning threshold
            {
                var proceed = windowService.ShowConfirmDialog("This file is very large and may cause performance issues. Continue?", "Large File Warning");
                if(!proceed) return;
            }

            await LoadDocument(dialog.FileName);
        }

        private async Task SaveDocumentAs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".txt"
            };

            if(dialog.ShowDialog() != true) return;

            document.FilePath = dialog.FileName;
            await SaveDocument();
        }

        private void ToggleWordWrap() => IsWordWrapEnabled = !IsWordWrapEnabled;

        private void ToggleInfoBar() => IsInfoBarVisible = !IsInfoBarVisible;

        private void UpdateMenuBarVisibility(bool autoHide)
        {
            MenuBarHeight = autoHide ? 0 : UIConstants.MenuBarHeight;
            IsMenuBarEnabled = !autoHide;
        }

        private void UpdateTitle()
        {
            var title = string.IsNullOrEmpty(document.FileName) ? "NotepadEx" : $"NotepadEx   |  {document.FileName}{(document.IsModified ? "*" : "")}";
            if(TitleBarViewModel != null)
            {
                TitleBarViewModel.TitleText = title;
            }
        }

        public bool PromptToSaveChanges()
        {
            if(!document.IsModified) return true;

            var result = MessageBox.Show("You have unsaved changes. Would you like to save them before proceeding?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch(result)
            {
                case MessageBoxResult.Yes:
                    _ = SaveDocument();
                    return true;
                case MessageBoxResult.No:
                    // ** FIX 2: Explicitly mark changes as discarded. **
                    document.IsModified = false;
                    return true;
                case MessageBoxResult.Cancel:
                    return false;
            }
            return true;
        }

        private async Task SaveDocument()
        {
            if(string.IsNullOrEmpty(document.FilePath))
            {
                await SaveDocumentAs();
                return;
            }

            try
            {
                // User-driven text change happens via binding. Here we just set content before saving.
                document.Content = textEditor.Document.Text;
                await documentService.SaveDocumentAsync(document);
                document.IsModified = false;
                UpdateTitle();
                UpdateStatusBar();
            }
            catch(Exception ex)
            {
                windowService.ShowDialog($"Error saving file: {ex.Message}", "Error");
            }
        }

        private void UpdateStatusBar()
        {
            var caret = textEditor.TextArea.Caret;
            var totalChars = textEditor.Document.TextLength;
            var totalLines = textEditor.Document.LineCount;
            StatusText = $"Ln {caret.Line}, Col {caret.Column}   |   Characters: {totalChars}   |   Lines: {totalLines}";
        }

        public async Task OpenRecentFile(string path)
        {
            if(!PromptToSaveChanges()) return;
            await LoadDocument(path);
        }

        private void Copy() => textEditor.Copy();
        private void Cut() => textEditor.Cut();
        private void Paste() => textEditor.Paste();

      
        private void NewDocument()
        {
            if(!PromptToSaveChanges()) return;

            // ** FIX 4: Simplified and explicit state control. **
            document.FilePath = string.Empty;
            DocumentContent = string.Empty; // This clears the UI
            document.IsModified = false;    // Explicitly set state to unmodified

            UpdateTitle();
            UpdateStatusBar();
        }

        private void PrintDocument()
        {
            try
            {
                documentService.PrintDocument(document);
            }
            catch(Exception ex)
            {
                windowService.ShowDialog($"Error printing document: {ex.Message}", "Error");
            }
        }

        public void HandleMouseMovement(double mouseY)
        {
            if(Settings.Default.MenuBarAutoHide && mouseY < 2)
                UpdateMenuBarVisibility(false);
            else if(Settings.Default.MenuBarAutoHide && mouseY > UIConstants.MenuBarHeight)
                UpdateMenuBarVisibility(true);
        }

        public void UpdateWindowState(WindowState newState)
        {
            if(newState == WindowState.Minimized) return;
            UpdateMenuBarVisibility(Settings.Default.MenuBarAutoHide);
        }

        public void HandleWindowResize(Window window, Point position) => WindowResizerUtil.ResizeWindow(window, position);

        private void OpenFileLocation()
        {
            var path = document.FilePath;
            if(File.Exists(path))
                Process.Start("explorer.exe", $"/select,\"{path}\"");
        }

        private async void HandleOpenRecent(RoutedEventArgs e)
        {
            if(e.OriginalSource is MenuItem menuItem && menuItem.Header is string path && path != "...")
                await OpenRecentFile(path);
        }

        public void Cleanup()
        {
            SaveSettings?.Invoke();
        }
    }
}