using ICSharpCode.AvalonEdit.Highlighting;

namespace NotepadEx.MVVM.ViewModels
{
    public class PlainTextHighlightingDefinition : IHighlightingDefinition
    {
        public string Name => "None / Plain Text";

        public HighlightingRuleSet MainRuleSet => null;

        public IEnumerable<HighlightingColor> NamedHighlightingColors => Enumerable.Empty<HighlightingColor>();

        public IDictionary<string, string> Properties => new Dictionary<string, string>();

        public HighlightingColor GetNamedColor(string name) => null;

        public HighlightingRuleSet GetNamedRuleSet(string name) => null;
    }
}