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
                Settings.Default.RecentFiles = string.Join(",", RecentFileManager.RecentFiles);
                Settings.Default.WindowSizeX = window.Width;
                Settings.Default.WindowSizeY = window.Height;
                Settings.Default.TextWrapping = textEditor.WordWrap;
                Settings.Default.FontSize = textEditor.FontSize;
                Settings.Default.FontFamily = textEditor.FontFamily.Source;
                Settings.Default.FontWeight = textEditor.FontWeight.ToString();
                Settings.Default.FontStyle = textEditor.FontStyle.ToString();
                Settings.Default.ShowLineNumbers = textEditor.ShowLineNumbers;
                if(textEditor.SyntaxHighlighting != null)
                {
                    Settings.Default.SyntaxHighlightingName = textEditor.SyntaxHighlighting.Name;
                }
                Settings.Default.Underline = false;
                Settings.Default.Strikethrough = false;
                Settings.Default.Save();
            });
        }
    }
}