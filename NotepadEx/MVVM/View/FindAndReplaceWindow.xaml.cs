using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Util;
using Color = System.Windows.Media.Color;

namespace NotepadEx.MVVM.View
{
    public class MatchInfo
    {
        public string Location { get; set; }
        public string Preview { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }

    public class SearchHighlightRenderer : DocumentColorizingTransformer
    {
        private List<TextSegment> highlights = new List<TextSegment>();
        private int currentMatchIndex = -1;

        public void SetHighlights(List<TextSegment> newHighlights)
        {
            highlights = newHighlights;
        }

        public void SetCurrentMatch(int index)
        {
            currentMatchIndex = index;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if(highlights == null) return;

            for(int i = 0; i < highlights.Count; i++)
            {
                var highlight = highlights[i];
                if(highlight.StartOffset <= line.EndOffset && highlight.EndOffset >= line.Offset)
                {
                    int start = Math.Max(highlight.StartOffset, line.Offset);
                    int end = Math.Min(highlight.EndOffset, line.EndOffset);

                    // Current match gets a different color
                    if(i == currentMatchIndex)
                    {
                        ChangeLinePart(start, end, element =>
                        {
                            element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromRgb(255, 165, 0))); // Orange
                        });
                    }
                    else
                    {
                        ChangeLinePart(start, end, element =>
                        {
                            element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromRgb(255, 255, 0))); // Yellow
                        });
                    }
                }
            }
        }
    }

    public partial class FindAndReplaceWindow : Window
    {
        private readonly TextEditor targetEditor;
        private int currentMatchIndex = -1;
        private List<MatchInfo> allMatches = new List<MatchInfo>();
        private SearchHighlightRenderer highlightRenderer;
        private WindowChrome _windowChrome;

        public CustomTitleBarViewModel TitleBarViewModel { get; }

        public FindAndReplaceWindow(TextEditor editor)
        {
            InitializeComponent();
            DataContext = this;
            TitleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "Find and Replace", showMinimize: true, showMaximize: false, isResizable: false);
            targetEditor = editor;

            // Add highlight renderer
            highlightRenderer = new SearchHighlightRenderer();
            targetEditor.TextArea.TextView.LineTransformers.Add(highlightRenderer);

            // Add keyboard shortcuts
            this.KeyDown += Window_KeyDown;

            Loaded += FindAndReplaceWindow_Loaded;
            Closing += FindAndReplaceWindow_Closing;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.F3)
            {
                if(Keyboard.Modifiers == ModifierKeys.Shift)
                    Find(false);
                else
                    Find(true);
                e.Handled = true;
            }
            else if(e.Key == Key.Escape)
            {
                ClearHighlights();
                this.Hide();
                e.Handled = true;
            }
        }

        private void FindAndReplaceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowChrome = new WindowChrome(this);
            _windowChrome.Enable();
        }

        private void FindAndReplaceWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Don't actually close, just hide
            e.Cancel = true;
            ClearHighlights();
            this.Hide();
        }

        private void FindTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateMatchCount();
        }

        private void SearchOption_Changed(object sender, RoutedEventArgs e)
        {
            UpdateMatchCount();
        }

        private void UpdateMatchCount()
        {
            string searchText = FindTextBox.Text;
            if(string.IsNullOrEmpty(searchText))
            {
                MatchCountText.Text = "";
                StatusText.Text = "Ready";
                ClearHighlights();
                return;
            }

            try
            {
                var matches = FindAllMatches();
                int count = matches.Count;

                if(count > 0)
                {
                    MatchCountText.Text = $"{count} match{(count != 1 ? "es" : "")}";
                    StatusText.Text = count == 1 ? "1 match found" : $"{count} matches found";
                }
                else
                {
                    MatchCountText.Text = "No matches";
                    StatusText.Text = "No matches found";
                }
            }
            catch(Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MatchCountText.Text = "";
            }
        }

        private List<MatchInfo> FindAllMatches()
        {
            var matches = new List<MatchInfo>();
            string searchText = FindTextBox.Text;
            if(string.IsNullOrEmpty(searchText)) return matches;

            string text = targetEditor.Document.Text;

            if(UseRegexCheckBox.IsChecked == true)
            {
                try
                {
                    var regex = new Regex(searchText, MatchCaseCheckBox.IsChecked == true ? RegexOptions.None : RegexOptions.IgnoreCase);
                    var regexMatches = regex.Matches(text);

                    foreach(Match match in regexMatches)
                    {
                        var location = targetEditor.Document.GetLocation(match.Index);
                        var line = targetEditor.Document.GetLineByNumber(location.Line);
                        var lineText = targetEditor.Document.GetText(line.Offset, line.Length);
                        matches.Add(new MatchInfo
                        {
                            Location = $"Ln {location.Line}, Col {location.Column}",
                            Preview = lineText.Trim(),
                            Offset = match.Index,
                            Length = match.Length
                        });
                    }
                }
                catch(Exception)
                {
                    // Invalid regex
                    return matches;
                }
            }
            else
            {
                var comparison = MatchCaseCheckBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                int offset = 0;

                while((offset = text.IndexOf(searchText, offset, comparison)) != -1)
                {
                    // Check whole word if needed
                    if(MatchWholeWordCheckBox.IsChecked == true)
                    {
                        bool isWholeWord = true;

                        // Check character before
                        if(offset > 0 && char.IsLetterOrDigit(text[offset - 1]))
                            isWholeWord = false;

                        // Check character after
                        if(offset + searchText.Length < text.Length &&
                            char.IsLetterOrDigit(text[offset + searchText.Length]))
                            isWholeWord = false;

                        if(!isWholeWord)
                        {
                            offset++;
                            continue;
                        }
                    }

                    var location = targetEditor.Document.GetLocation(offset);
                    var line = targetEditor.Document.GetLineByNumber(location.Line);
                    var lineText = targetEditor.Document.GetText(line.Offset, line.Length);

                    matches.Add(new MatchInfo
                    {
                        Location = $"Ln {location.Line}, Col {location.Column}",
                        Preview = lineText.Trim(),
                        Offset = offset,
                        Length = searchText.Length
                    });

                    offset += searchText.Length;
                }
            }

            return matches;
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e) => Find(true);
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e) => Find(false);

        private void Find(bool forward)
        {
            string searchText = FindTextBox.Text;
            if(string.IsNullOrEmpty(searchText))
            {
                StatusText.Text = "Please enter search text";
                return;
            }

            allMatches = FindAllMatches();

            if(allMatches.Count == 0)
            {
                StatusText.Text = "No matches found";
                MessageBox.Show($"Cannot find \"{searchText}\"", "Find and Replace", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Highlight all matches
            var highlights = allMatches.Select(m => new TextSegment { StartOffset = m.Offset, Length = m.Length }).ToList();
            highlightRenderer.SetHighlights(highlights);
            targetEditor.TextArea.TextView.Redraw();

            // Find next/previous match based on current selection
            int startOffset = targetEditor.SelectionStart;

            if(forward)
            {
                // Find next match after current position
                currentMatchIndex = allMatches.FindIndex(m => m.Offset > startOffset);

                if(currentMatchIndex == -1)
                {
                    if(WrapAroundCheckBox.IsChecked == true)
                    {
                        currentMatchIndex = 0;
                        StatusText.Text = "Search wrapped to beginning";
                    }
                    else
                    {
                        StatusText.Text = "No more matches found";
                        return;
                    }
                }
            }
            else
            {
                // Find previous match before current position
                for(int i = allMatches.Count - 1; i >= 0; i--)
                {
                    if(allMatches[i].Offset < startOffset)
                    {
                        currentMatchIndex = i;
                        break;
                    }
                }

                if(currentMatchIndex == -1)
                {
                    if(WrapAroundCheckBox.IsChecked == true)
                    {
                        currentMatchIndex = allMatches.Count - 1;
                        StatusText.Text = "Search wrapped to end";
                    }
                    else
                    {
                        StatusText.Text = "No more matches found";
                        return;
                    }
                }
            }

            SelectMatch(currentMatchIndex);
        }

        private void SelectMatch(int index)
        {
            if(index < 0 || index >= allMatches.Count) return;

            var match = allMatches[index];
            targetEditor.Focus();
            targetEditor.Select(match.Offset, match.Length);

            var loc = targetEditor.Document.GetLocation(match.Offset);
            targetEditor.ScrollToLine(loc.Line);

            highlightRenderer.SetCurrentMatch(index);
            targetEditor.TextArea.TextView.Redraw();

            StatusText.Text = $"Match {index + 1} of {allMatches.Count}";
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = FindTextBox.Text;
            string replaceText = ReplaceTextBox.Text;

            if(string.IsNullOrEmpty(searchText))
            {
                StatusText.Text = "Please enter search text";
                return;
            }

            var comparison = MatchCaseCheckBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if(targetEditor.SelectionLength > 0 && targetEditor.SelectedText.Equals(searchText, comparison))
            {
                targetEditor.Document.Replace(targetEditor.SelectionStart, targetEditor.SelectionLength, replaceText);
                StatusText.Text = "Replaced 1 occurrence";
            }

            Find(true);
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = FindTextBox.Text;
            string replaceText = ReplaceTextBox.Text;

            if(string.IsNullOrEmpty(searchText))
            {
                StatusText.Text = "Please enter search text";
                return;
            }

            var matches = FindAllMatches();

            if(matches.Count == 0)
            {
                StatusText.Text = "No matches to replace";
                MessageBox.Show("No matches found to replace.", "Find and Replace", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Replace {matches.Count} occurrence(s)?", "Confirm Replace All", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if(result != MessageBoxResult.Yes)
                return;

            targetEditor.Document.BeginUpdate();

            // Replace from end to beginning to maintain offsets
            for(int i = matches.Count - 1; i >= 0; i--)
            {
                targetEditor.Document.Replace(matches[i].Offset, matches[i].Length, replaceText);
            }

            targetEditor.Document.EndUpdate();

            StatusText.Text = $"Replaced {matches.Count} occurrence(s)";
            ClearHighlights();
            UpdateMatchCount();
        }

        private void FindAllButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = FindTextBox.Text;
            if(string.IsNullOrEmpty(searchText))
            {
                StatusText.Text = "Please enter search text";
                return;
            }

            allMatches = FindAllMatches();
            MatchesListBox.ItemsSource = allMatches;

            if(allMatches.Count > 0)
            {
                var highlights = allMatches.Select(m => new TextSegment { StartOffset = m.Offset, Length = m.Length }).ToList();
                highlightRenderer.SetHighlights(highlights);
                targetEditor.TextArea.TextView.Redraw();
                StatusText.Text = $"Found {allMatches.Count} match{(allMatches.Count != 1 ? "es" : "")}";
            }
            else
            {
                StatusText.Text = "No matches found";
            }
        }

        private void ClearHighlightsButton_Click(object sender, RoutedEventArgs e)
        {
            ClearHighlights();
        }

        private void ClearHighlights()
        {
            highlightRenderer.SetHighlights(new List<TextSegment>());
            highlightRenderer.SetCurrentMatch(-1);
            targetEditor.TextArea.TextView.Redraw();
            MatchesListBox.ItemsSource = null;
            allMatches.Clear();
            currentMatchIndex = -1;
        }
    }
}