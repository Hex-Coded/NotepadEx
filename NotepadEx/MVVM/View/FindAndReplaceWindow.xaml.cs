using System.Windows;
using ICSharpCode.AvalonEdit;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Util;

namespace NotepadEx.MVVM.View
{
    public partial class FindAndReplaceWindow : Window
    {
        private readonly TextEditor targetEditor;
        private int lastSearchOffset = -1;
        private WindowChrome _windowChrome;

        public CustomTitleBarViewModel TitleBarViewModel { get; }

        public FindAndReplaceWindow(TextEditor editor)
        {
            InitializeComponent();
            DataContext = this;
            TitleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "Find and Replace", showMinimize: true, showMaximize: false, isResizable: false);
            targetEditor = editor;

            Loaded += FindAndReplaceWindow_Loaded;
            Closing += FindAndReplaceWindow_Closing;
        }

        private void FindAndReplaceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowChrome = new WindowChrome(this);
            _windowChrome.Enable();
        }

        private void FindAndReplaceWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _windowChrome?.Detach();
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e) => Find(true);
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e) => Find(false);

        private void Find(bool forward)
        {
            string searchText = FindTextBox.Text;
            if(string.IsNullOrEmpty(searchText)) return;

            int startOffset = forward ? targetEditor.SelectionStart + targetEditor.SelectionLength : targetEditor.SelectionStart;

            var comparison = MatchCaseCheckBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int foundIndex = -1;
            if(forward)
            {
                foundIndex = targetEditor.Document.Text.IndexOf(searchText, startOffset, comparison);
                if(foundIndex == -1)
                    foundIndex = targetEditor.Document.Text.IndexOf(searchText, 0, comparison);
            }
            else
            {
                foundIndex = targetEditor.Document.Text.LastIndexOf(searchText, startOffset - 1, comparison);
                if(foundIndex == -1)
                    foundIndex = targetEditor.Document.Text.LastIndexOf(searchText, targetEditor.Document.TextLength, comparison);
            }

            if(foundIndex != -1)
            {
                targetEditor.Focus();
                targetEditor.Select(foundIndex, searchText.Length);
                var loc = targetEditor.Document.GetLocation(foundIndex);
                targetEditor.ScrollTo(loc.Line, loc.Column);
                lastSearchOffset = foundIndex;
            }
            else
            {
                MessageBox.Show($"Cannot find \"{searchText}\"", "Find and Replace", MessageBoxButton.OK, MessageBoxImage.Information);
                lastSearchOffset = -1;
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = FindTextBox.Text;
            var comparison = MatchCaseCheckBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if(targetEditor.SelectionLength > 0 && targetEditor.SelectedText.Equals(searchText, comparison))
            {
                targetEditor.Document.Replace(targetEditor.SelectionStart, targetEditor.SelectionLength, ReplaceTextBox.Text);
            }
            Find(true);
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = FindTextBox.Text;
            string replaceText = ReplaceTextBox.Text;
            if(string.IsNullOrEmpty(searchText)) return;

            var comparison = MatchCaseCheckBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int offset = 0;
            int replacements = 0;

            targetEditor.Document.BeginUpdate();
            while((offset = targetEditor.Document.Text.IndexOf(searchText, offset, comparison)) != -1)
            {
                targetEditor.Document.Replace(offset, searchText.Length, replaceText);
                offset += replaceText.Length;
                replacements++;
            }
            targetEditor.Document.EndUpdate();

            MessageBox.Show($"Replaced {replacements} occurrence(s).", "Find and Replace", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}