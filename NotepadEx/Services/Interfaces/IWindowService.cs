using System.Windows;

namespace NotepadEx.Services.Interfaces;

public interface IWindowService
{
    void ShowDialog(string message, string title = "");
    bool ShowConfirmDialog(string message, string title = "");
    MessageBoxResult ShowSaveConfirmationDialog(string message, string title = "");
    string ShowOpenFileDialog(string filter = "");
    string ShowSaveFileDialog(string filter = "", string defaultExt = "");
    void SetWindowState(WindowState state);
    WindowState GetWindowState();
}