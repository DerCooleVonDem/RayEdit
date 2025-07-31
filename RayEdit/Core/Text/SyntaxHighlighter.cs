using Raylib_cs;

namespace RayEdit.Core.Text
{
    /// <summary>
    /// Provides syntax highlighting for C# code.
    /// </summary>
    public static class SyntaxHighlighter
    {
        // C# keywords that should be highlighted in blue
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while", "var", "async", "await", "yield"
        };

        // Characters that should be highlighted in yellow (brackets, operators, etc.)
        private static readonly HashSet<char> SpecialCharacters = new HashSet<char>
        {
            '(', ')', '{', '}', '[', ']', '<', '>', ';', ',', '.', ':', '=', '+', '-', '*', '/', '%',
            '&', '|', '^', '!', '?', '~'
        };

        /// <summary>
        /// Represents a token with its text, position, and color.
        /// </summary>
        public struct Token
        {
            public string Text;
            public int StartIndex;
            public int Length;
            public Color Color;

            public Token(string text, int startIndex, int length, Color color)
            {
                Text = text;
                StartIndex = startIndex;
                Length = length;
                Color = color;
            }
        }

        /// <summary>
        /// Tokenizes a line of C# code and returns tokens with appropriate colors.
        /// </summary>
        /// <param name="line">The line of code to tokenize</param>
        /// <param name="lineStartIndex">The starting character index of this line in the full text</param>
        /// <returns>List of tokens with colors</returns>
        public static List<Token> TokenizeLine(string line, int lineStartIndex)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrEmpty(line))
                return tokens;

            int i = 0;
            while (i < line.Length)
            {
                char currentChar = line[i];

                // Whitespaces als eigenen Token hinzufügen
                if (char.IsWhiteSpace(currentChar))
                {
                    int start = i;
                    while (i < line.Length && char.IsWhiteSpace(line[i]))
                        i++;
                    string ws = line.Substring(start, i - start);
                    // Replace tabs with spaces for consistent rendering
                    string displayWs = ws.Replace("\t", "    ");
                    tokens.Add(new Token(displayWs, lineStartIndex + start, i - start, Color.White));
                    continue;
                }

                // Handle string literals
                if (currentChar == '"')
                {
                    int start = i;
                    i++; // Skip opening quote
                    while (i < line.Length && line[i] != '"')
                    {
                        if (line[i] == '\\' && i + 1 < line.Length)
                            i += 2; // Skip escaped character
                        else
                            i++;
                    }
                    if (i < line.Length) i++; // Skip closing quote
                    
                    string stringLiteral = line.Substring(start, i - start);
                    tokens.Add(new Token(stringLiteral, lineStartIndex + start, i - start, Color.Green));
                    continue;
                }

                // Handle character literals
                if (currentChar == '\'')
                {
                    int start = i;
                    i++; // Skip opening quote
                    if (i < line.Length && line[i] == '\\' && i + 1 < line.Length)
                        i += 2; // Skip escaped character
                    else if (i < line.Length)
                        i++; // Skip character
                    if (i < line.Length) i++; // Skip closing quote
                    
                    string charLiteral = line.Substring(start, i - start);
                    tokens.Add(new Token(charLiteral, lineStartIndex + start, i - start, Color.Green));
                    continue;
                }

                // Handle single-line comments
                if (currentChar == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    string comment = line.Substring(i);
                    tokens.Add(new Token(comment, lineStartIndex + i, comment.Length, Color.Gray));
                    break; // Rest of line is comment
                }

                // Handle special characters (brackets, operators, etc.)
                if (SpecialCharacters.Contains(currentChar))
                {
                    tokens.Add(new Token(currentChar.ToString(), lineStartIndex + i, 1, Color.Yellow));
                    i++;
                    continue;
                }

                // Handle identifiers and keywords
                if (char.IsLetter(currentChar) || currentChar == '_')
                {
                    int start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                        i++;
                    
                    string identifier = line.Substring(start, i - start);
                    Color color = CSharpKeywords.Contains(identifier) ? Color.Blue : Color.White;
                    tokens.Add(new Token(identifier, lineStartIndex + start, i - start, color));
                    continue;
                }

                // Handle numbers
                if (char.IsDigit(currentChar))
                {
                    int start = i;
                    while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.' || line[i] == 'f' || line[i] == 'd' || line[i] == 'm'))
                        i++;
                    
                    string number = line.Substring(start, i - start);
                    tokens.Add(new Token(number, lineStartIndex + start, i - start, Color.Magenta));
                    continue;
                }

                // Default: treat as regular text
                tokens.Add(new Token(currentChar.ToString(), lineStartIndex + i, 1, Color.White));
                i++;
            }

            return tokens;
        }

        /// <summary>
        /// Checks if syntax highlighting should be applied based on file extension.
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the file should have C# syntax highlighting</returns>
        public static bool ShouldHighlight(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}