using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NotepadEx.MVVM.Models; public class Document
{
    string content = string.Empty;
    string[] cachedLines = Array.Empty<string>();
    public int SelectionStart { get; set; }
    public int SelectionLength { get; set; }

    public string Content
    {
        get => content;
        set
        {
            content = value;
            UpdateCachedLines();
        }
    }

    public string FilePath { get; set; } = string.Empty;
    public bool IsModified { get; set; }
    public string FileName => string.IsNullOrEmpty(FilePath) ? string.Empty : Path.GetFileName(FilePath);
    public string SelectedText => SelectionLength > 0 ? content.Substring(SelectionStart, SelectionLength) : string.Empty;
    public int CurrentLineNumber => GetLineNumberFromPosition(SelectionStart);
    public int CaretIndex => SelectionStart;
    public int CaretLineIndex => GetColumnIndexInLine(SelectionStart);
    public int TotalLines => cachedLines.Length;

    void UpdateCachedLines() => cachedLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

    public int GetLineNumberFromPosition(int position)
    {
        if(position < 0 || string.IsNullOrEmpty(content))
            return 1;
        int lineCount = 1;
        int currentPos = 0;
        foreach(string line in cachedLines)
        {
            if(position <= currentPos + line.Length)
                return lineCount;
            currentPos += line.Length + Environment.NewLine.Length;
            lineCount++;
        }
        return lineCount;
    }

    public int GetColumnIndexInLine(int position)
    {
        if(position <= 0)
            return 0;
        int currentPos = 0;
        foreach(string line in cachedLines)
        {
            int lineLength = line.Length + Environment.NewLine.Length;
            if(position <= currentPos + lineLength)
                return position - currentPos;
            currentPos += lineLength;
        }
        return 0;
    }

    public string GetCurrentLine()
    {
        int lineNumber = CurrentLineNumber - 1;
        return lineNumber >= 0 && lineNumber < cachedLines.Length
            ? cachedLines[lineNumber]
            : string.Empty;
    }

    public int GetPosition(int lineNumber, int columnIndex)
    {
        if(lineNumber <= 0 || lineNumber > cachedLines.Length)
            return 0;
        int position = 0;
        for(int i = 0; i < lineNumber - 1; i++)
            position += cachedLines[i].Length + Environment.NewLine.Length;
        return position + Math.Min(columnIndex, cachedLines[lineNumber - 1].Length);
    }

    public void CutLine(TextBox textBox)
    {
        int lineIndex = CurrentLineNumber - 1;
        if(lineIndex >= 0 && lineIndex < cachedLines.Length)
        {
            int lineStartPosition = GetPosition(CurrentLineNumber, 0);
            int lineLength = cachedLines[lineIndex].Length;
            if(lineIndex < cachedLines.Length - 1)
                lineLength += Environment.NewLine.Length;

            int originalStart = SelectionStart;
            int originalLength = SelectionLength;

            textBox.Select(lineStartPosition, lineLength);

            string lineText = textBox.SelectedText;
            Clipboard.SetText(lineText);
            textBox.SelectedText = "";
        }
    }
}