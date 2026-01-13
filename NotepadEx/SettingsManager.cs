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
                // FIX: Save window state as a string
                Settings.Default.WindowState = window.WindowState.ToString();

                // FIX: Save the correct size depending on the state
                // If maximized, save the "restored" dimensions, not the maximized ones.
                if(window.WindowState == WindowState.Maximized)
                {
                    Settings.Default.WindowSizeX = window.RestoreBounds.Width;
                    Settings.Default.WindowSizeY = window.RestoreBounds.Height;
                }
                else // Normal or Minimized
                {
                    Settings.Default.WindowSizeX = window.Width;
                    Settings.Default.WindowSizeY = window.Height;
                }

                // Save all other application settings
                Settings.Default.TextWrapping = textEditor.WordWrap;
                Settings.Default.FontSize = textEditor.FontSize;
                Settings.Default.FontFamily = textEditor.FontFamily.Source;
                Settings.Default.FontWeight = textEditor.FontWeight.ToString();
                Settings.Default.FontStyle = textEditor.FontStyle.ToString();
                Settings.Default.ShowLineNumbers = textEditor.ShowLineNumbers;
                Settings.Default.InfoBarVisible = (window.DataContext as MVVM.ViewModels.MainWindowViewModel)?.IsInfoBarVisible ?? true;

                Settings.Default.SyntaxHighlightingName = textEditor.SyntaxHighlighting?.Name ?? "None / Plain Text";
                Settings.Default.ThemeName = themeName;

                Settings.Default.Underline = false;
                Settings.Default.Strikethrough = false;
                Settings.Default.Save();
            });
        }
    }
}