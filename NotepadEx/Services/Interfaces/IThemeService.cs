using System;
using System.Collections.ObjectModel;
using NotepadEx.MVVM.Models;
using NotepadEx.Theme;

namespace NotepadEx.Services.Interfaces
{
    public interface IThemeService
    {
        event EventHandler ThemeChanged;
        ColorTheme CurrentTheme { get; }
        string CurrentThemeName { get; set; }

        ObservableCollection<ThemeInfo> AvailableThemes { get; }
        void LoadCurrentTheme();
        void ApplyTheme(string themeName);
        void OpenThemeEditor();
        void LoadAvailableThemes();
        void AddEditableColorLinesToWindow();
        void TriggerLiveUpdate(); // ADD THIS METHOD
    }
}