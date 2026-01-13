using System.Windows;
using NotepadEx.Services.Interfaces;

namespace NotepadEx.Services;

public class WindowService : IWindowService
{
    public Window owner { get; private set; }

    public WindowService(Window owner) => this.owner = owner;

    public void ShowDialog(string message, string title = "") => MessageBox.Show(owner, message, title, MessageBoxButton.OK);

    public bool ShowConfirmDialog(string message, string title = "")
    {
        var result = MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public string ShowOpenFileDialog(string filter = "")
    {
        var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = string.IsNullOrEmpty(filter) ? "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" : filter
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
    }

    public string ShowSaveFileDialog(string filter = "", string defaultExt = "")
    {
        var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = string.IsNullOrEmpty(filter) ? "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" : filter,
            DefaultExt = string.IsNullOrEmpty(defaultExt) ? ".txt" : defaultExt
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
    }

    public void SetWindowState(WindowState state) => owner.WindowState = state;

    public WindowState GetWindowState() => owner.WindowState;

    public MessageBoxResult ShowSaveConfirmationDialog(string message, string title = "")
    {
        return MessageBox.Show(owner, message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
    }
}

