using System.Numerics;
using System.Text.RegularExpressions;
using Raylib_cs;

namespace RayEdit.UI.Controls
{
    public class AutoCompletion
    {
        private List<string> _suggestions;
        private int _selectedIndex;
        private bool _isVisible;
        private string _currentPrefix;
        private Vector2 _position;
        private Font _font;
        
        // C# Keywords
        private readonly HashSet<string> _csharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "async",
            "await", "by", "descending", "dynamic", "equals", "from", "get", "global", "group",
            "into", "join", "let", "nameof", "on", "orderby", "partial", "remove", "select",
            "set", "value", "var", "when", "where", "yield"
        };

        public AutoCompletion(Font font)
        {
            _font = font;
            _suggestions = new List<string>();
            _selectedIndex = 0;
            _isVisible = false;
        }

        public bool IsVisible => _isVisible;
        public string SelectedSuggestion => _suggestions.Count > 0 ? _suggestions[_selectedIndex] : "";

        public void Show(string prefix, Vector2 position, string documentContent, bool isCSharpFile)
        {
            _currentPrefix = prefix.ToLower();
            _position = position;
            _selectedIndex = 0;
            
            GenerateSuggestions(documentContent, isCSharpFile);
            
            _isVisible = _suggestions.Count > 0;
        }

        public void Hide()
        {
            _isVisible = false;
            _suggestions.Clear();
        }

        public void MoveSelection(int direction)
        {
            if (_suggestions.Count == 0) return;
            
            _selectedIndex += direction;
            if (_selectedIndex < 0) _selectedIndex = _suggestions.Count - 1;
            if (_selectedIndex >= _suggestions.Count) _selectedIndex = 0;
        }

        public void UpdatePrefix(string newPrefix, string documentContent, bool isCSharpFile)
        {
            if (!_isVisible) return;
            
            _currentPrefix = newPrefix.ToLower();
            _selectedIndex = 0;
            
            GenerateSuggestions(documentContent, isCSharpFile);
            
            if (_suggestions.Count == 0)
            {
                Hide();
            }
        }

        private void GenerateSuggestions(string documentContent, bool isCSharpFile)
        {
            _suggestions.Clear();
            
            if (string.IsNullOrEmpty(_currentPrefix)) return;

            var allSuggestions = new HashSet<string>();

            // Add C# keywords if it's a C# file
            if (isCSharpFile)
            {
                foreach (var keyword in _csharpKeywords)
                {
                    if (keyword.StartsWith(_currentPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        allSuggestions.Add(keyword);
                    }
                }
            }

            // Extract words from document content
            var words = ExtractWordsFromDocument(documentContent);
            foreach (var word in words)
            {
                if (word.Length > _currentPrefix.Length && 
                    word.StartsWith(_currentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    allSuggestions.Add(word);
                }
            }

            // Convert to list and sort
            _suggestions = allSuggestions.OrderBy(s => s.Length).ThenBy(s => s).Take(10).ToList();
        }

        private HashSet<string> ExtractWordsFromDocument(string content)
        {
            var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Use regex to find words (alphanumeric + underscore)
            var matches = Regex.Matches(content, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
            
            foreach (Match match in matches)
            {
                string word = match.Value;
                if (word.Length >= 2) // Only consider words with 2+ characters
                {
                    words.Add(word);
                }
            }
            
            return words;
        }

        public void Draw()
        {
            if (!_isVisible || _suggestions.Count == 0) return;

            const int itemHeight = 25;
            const int padding = 5;
            const int maxWidth = 200;
            
            int boxHeight = _suggestions.Count * itemHeight + padding * 2;
            
            // Draw background
            Raylib.DrawRectangle((int)_position.X, (int)_position.Y, maxWidth, boxHeight, 
                new Color(40, 40, 40, 240));
            
            // Draw border
            Raylib.DrawRectangleLines((int)_position.X, (int)_position.Y, maxWidth, boxHeight, 
                new Color(100, 100, 100, 255));

            // Draw suggestions
            for (int i = 0; i < _suggestions.Count; i++)
            {
                int y = (int)_position.Y + padding + i * itemHeight;
                
                // Highlight selected item
                if (i == _selectedIndex)
                {
                    Raylib.DrawRectangle((int)_position.X + 2, y, maxWidth - 4, itemHeight, 
                        new Color(0, 120, 215, 100));
                }
                
                // Draw text
                Color textColor = i == _selectedIndex ? Color.White : new Color(200, 200, 200, 255);
                Raylib.DrawTextEx(_font, _suggestions[i], 
                    new Vector2(_position.X + padding, y + 3), 16, 1, textColor);
            }
        }
    }
}