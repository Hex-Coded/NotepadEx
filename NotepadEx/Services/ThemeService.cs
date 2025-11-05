using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using NotepadEx.MVVM.Models;
using NotepadEx.MVVM.View;
using NotepadEx.Properties;
using NotepadEx.Services.Interfaces;
using NotepadEx.Theme;
using NotepadEx.Util;

namespace NotepadEx.Services
{
    public class ThemeService : IThemeService
    {
        public event EventHandler ThemeChanged;
        public ColorTheme CurrentTheme { get; private set; }
        public string CurrentThemeName { get; private set; }
        public ObservableCollection<ThemeInfo> AvailableThemes { get; private set; }

        private readonly Application application;
        private ThemeEditorWindow themeEditorWindow;

        public ThemeService(Application application)
        {
            this.application = application;
            AvailableThemes = new ObservableCollection<ThemeInfo>();
            LoadAvailableThemes();
        }

        public void LoadCurrentTheme()
        {
            // This method reads the saved theme name from settings and tells ApplyTheme to load it.
            ApplyTheme(Settings.Default.ThemeName);
        }

        public void ApplyTheme(string themeName)
        {
            ProcessSync.RunSynchronized(() =>
            {
                try
                {
                    ColorTheme theme;
                    if(themeName != null && File.Exists(Path.Combine(DirectoryUtil.NotepadExThemesPath, themeName)))
                    {
                        var fileData = File.ReadAllText(Path.Combine(DirectoryUtil.NotepadExThemesPath, themeName));
                        var themeSerialized = JsonSerializer.Deserialize<ColorThemeSerializable>(fileData);
                        theme = themeSerialized.ToColorTheme();
                    }
                    else
                    {
                        // If no theme is found or specified, create a default/empty one.
                        theme = new ColorTheme();
                    }

                    // The service's main job is to manage the theme object and apply the brushes.
                    // It no longer manages the name or saves the setting.
                    CurrentTheme = theme;

                    ApplyThemeObject(theme.themeObj_TextEditorBg, UIConstants.Color_TextEditorBg);
                    ApplyThemeObject(theme.themeObj_TextEditorFg, UIConstants.Color_TextEditorFg);
                    ApplyThemeObject(theme.themeObj_TextEditorCaret, UIConstants.Color_TextEditorCaret);
                    ApplyThemeObject(theme.themeObj_TextEditorScrollBar, UIConstants.Color_TextEditorScrollBar);
                    ApplyThemeObject(theme.themeObj_TextEditorTextHighlight, UIConstants.Color_TextEditorTextHighlight);
                    ApplyThemeObject(theme.themeObj_TitleBarBg, UIConstants.Color_TitleBarBg);
                    ApplyThemeObject(theme.themeObj_TitleBarFont, UIConstants.Color_TitleBarFont);
                    ApplyThemeObject(theme.themeObj_SystemButtons, UIConstants.Color_SystemButtons);
                    ApplyThemeObject(theme.themeObj_BorderColor, UIConstants.Color_BorderColor);
                    ApplyThemeObject(theme.themeObj_MenuBarBg, UIConstants.Color_MenuBarBg);
                    ApplyThemeObject(theme.themeObj_MenuItemFg, UIConstants.Color_MenuItemFg);
                    ApplyThemeObject(theme.themeObj_InfoBarBg, UIConstants.Color_InfoBarBg);
                    ApplyThemeObject(theme.themeObj_InfoBarFg, UIConstants.Color_InfoBarFg);
                    ApplyThemeObject(theme.themeObj_MenuBorder, UIConstants.Color_MenuBorder);
                    ApplyThemeObject(theme.themeObj_MenuBg, UIConstants.Color_MenuBg);
                    ApplyThemeObject(theme.themeObj_MenuFg, UIConstants.Color_MenuFg);
                    ApplyThemeObject(theme.themeObj_MenuSeperator, UIConstants.Color_MenuSeperator);
                    ApplyThemeObject(theme.themeObj_MenuDisabledFg, UIConstants.Color_MenuDisabledFg);
                    ApplyThemeObject(theme.themeObj_MenuItemSelectedBg, UIConstants.Color_MenuItemSelectedBg);
                    ApplyThemeObject(theme.themeObj_MenuItemSelectedBorder, UIConstants.Color_MenuItemSelectedBorder);
                    ApplyThemeObject(theme.themeObj_MenuItemHighlightBg, UIConstants.Color_MenuItemHighlightBg);
                    ApplyThemeObject(theme.themeObj_MenuItemHighlightBorder, UIConstants.Color_MenuItemHighlightBorder);
                    ApplyThemeObject(theme.themeObj_ToolWindowBg, UIConstants.Color_ToolWindowBg);
                    ApplyThemeObject(theme.themeObj_ToolWindowButtonBg, UIConstants.Color_ToolWindowButtonBg);
                    ApplyThemeObject(theme.themeObj_ToolWindowButtonBorder, UIConstants.Color_ToolWindowButtonBorder);
                    ApplyThemeObject(theme.themeObj_ToolWindowFont, UIConstants.Color_ToolWindowFont);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error Loading Theme ({themeName}): {ex.Message}");
                }
                finally
                {
                    // Notify any subscribers (like MainWindow for AvalonEdit) that the theme has changed.
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public void OpenThemeEditor()
        {
            if(themeEditorWindow == null)
            {
                themeEditorWindow = new ThemeEditorWindow(this);
                LoadAvailableThemes();
            }
            themeEditorWindow.Show();
        }

        public void LoadAvailableThemes()
        {
            AvailableThemes.Clear();
            var themeFiles = GetThemeFiles();

            foreach(var file in themeFiles)
            {
                AvailableThemes.Add(new ThemeInfo
                {
                    Name = file.Name,
                    FilePath = file.FullName,
                    LastModified = file.LastWriteTime
                });
            }
        }

        private IEnumerable<FileInfo> GetThemeFiles()
        {
            try
            {
                if(!Directory.Exists(DirectoryUtil.NotepadExThemesPath))
                {
                    Directory.CreateDirectory(DirectoryUtil.NotepadExThemesPath);
                    return Enumerable.Empty<FileInfo>();
                }
                var directory = new DirectoryInfo(DirectoryUtil.NotepadExThemesPath);
                return directory.GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();
            }
            catch(Exception)
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        private void ApplyThemeObject(ThemeObject themeObj, string resourceKey)
        {
            if(themeObj == null) return;

            if(themeObj.isGradient)
                AppResourceUtil<LinearGradientBrush>.TrySetResource(application, resourceKey, themeObj.gradient);
            else
                AppResourceUtil<SolidColorBrush>.TrySetResource(application, resourceKey, new SolidColorBrush(themeObj.color.GetValueOrDefault()));
        }

        public void AddEditableColorLinesToWindow() => themeEditorWindow?.AddEditableColorLinesToWindow();
    }
}