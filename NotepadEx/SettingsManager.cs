using System.Windows;
using ICSharpCode.AvalonEdit;
using NotepadEx.Properties;
using NotepadEx.Util;

namespace NotepadEx
{
    public static class SettingsManager
    {
        public static void SaveSettings(Window window, TextEditor textEditor, string themeName)
        {
            ProcessSync.RunSynchronized(() =>
            {
                // Save window state and size
                Settings.Default.WindowState = window.WindowState.ToString();
                if(window.WindowState == WindowState.Maximized)
                {
                    Settings.Default.WindowSizeX = window.RestoreBounds.Width;
                    Settings.Default.WindowSizeY = window.RestoreBounds.Height;
                }
                else
                {
                    Settings.Default.WindowSizeX = window.Width;
                    Settings.Default.WindowSizeY = window.Height;
                }

                // Save editor and preference settings
                Settings.Default.TextWrapping = textEditor.WordWrap;
                Settings.Default.FontSize = textEditor.FontSize;
                Settings.Default.FontFamily = textEditor.FontFamily.Source;
                Settings.Default.FontWeight = textEditor.FontWeight.ToString();
                Settings.Default.FontStyle = textEditor.FontStyle.ToString();
                Settings.Default.ShowLineNumbers = textEditor.ShowLineNumbers;
                Settings.Default.InfoBarVisible = (window.DataContext as MVVM.ViewModels.MainWindowViewModel)?.IsInfoBarVisible ?? true;
                Settings.Default.SyntaxHighlightingName = textEditor.SyntaxHighlighting?.Name ?? "None / Plain Text";

                // *** THE FIX: This line was missing and has been re-added. ***
                Settings.Default.ThemeName = themeName;

                // Unused settings
                Settings.Default.Underline = false;
                Settings.Default.Strikethrough = false;

                // Final save
                Settings.Default.Save();
            });
        }
    }
}