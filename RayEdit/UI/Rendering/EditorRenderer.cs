using System.Numerics;
using RayEdit.Core.Text;
using Raylib_cs;

namespace RayEdit.UI.Rendering
{
    /// <summary>
    /// Handles the rendering of the text editor content
    /// </summary>
    public class EditorRenderer
    {
        private Font _font;
        private int _fontSize;
        private int _lineSpacing;
        private Color _backgroundColor;
        private Color _textColor;
        private Color _selectionColor;
        private Color _cursorColor;
        private Color _currentLineColor;

        public EditorRenderer()
        {
            _font = Raylib.GetFontDefault();
            _fontSize = 20;
            _lineSpacing = 5;
            _backgroundColor = Color.DarkGray;
            _textColor = Color.White;
            _selectionColor = Color.Blue;
            _cursorColor = Color.White;
            _currentLineColor = Color.Gray;
        }

        public void SetTheme(Theme theme)
        {
            _font = theme.Font;
            _fontSize = theme.FontSize;
            _lineSpacing = theme.LineSpacing;
            _backgroundColor = theme.BackgroundColor;
            _textColor = theme.ForegroundColor;
            _selectionColor = theme.SelectionColor;
            _cursorColor = theme.CursorColor;
            _currentLineColor = theme.CurrentLineColor;
        }

        public int GetLineHeight()
        {
            return _fontSize + _lineSpacing;
        }

        public void DrawEditor(int x, int y, int width, int height, TextBuffer textBuffer, int scrollY, bool enableSyntaxHighlighting = true)
        {
            // Draw background
            Raylib.DrawRectangle(x, y, width, height, _backgroundColor);

            string content = textBuffer.Content;
            int cursorIndex = textBuffer.CursorIndex;
            
            // Calculate line height
            int lineHeight = GetLineHeight();
            
            // Calculate cursor position
            var (cursorLine, cursorColumn) = GetCursorPosition(content, cursorIndex);
            
            // Draw current line highlight
            int currentLineY = y + ((cursorLine - 1) * lineHeight) - scrollY;
            if (currentLineY >= y && currentLineY < y + height)
            {
                Raylib.DrawRectangle(x, currentLineY, width, lineHeight, _currentLineColor);
            }

            // Draw selection
            if (textBuffer.HasSelection())
            {
                DrawSelection(x, y, width, height, textBuffer, scrollY, lineHeight);
            }

            // Draw text
            DrawText(x, y, width, height, content, scrollY, lineHeight, enableSyntaxHighlighting);

            // Draw cursor
            DrawCursor(x, y, cursorIndex, content, scrollY, lineHeight);
        }

        private void DrawSelection(int x, int y, int width, int height, TextBuffer textBuffer, int scrollY, int lineHeight)
        {
            var (selStart, selEnd) = textBuffer.GetSelectionRange();
            string content = textBuffer.Content;
            
            var (startLine, startCol) = GetCursorPosition(content, selStart);
            var (endLine, endCol) = GetCursorPosition(content, selEnd);

            for (int line = startLine; line <= endLine; line++)
            {
                int lineY = y + ((line - 1) * lineHeight) - scrollY;
                
                // Skip lines not visible
                if (lineY + lineHeight < y || lineY > y + height)
                    continue;

                // Get line start and end positions
                int lineStartPos = GetLineStartPosition(content, line);
                int lineEndPos = GetLineEndPosition(content, line);

                // Calculate selection bounds for this line
                int selectionStart = (line == startLine) ? startCol - 1 : 0;
                int selectionEnd = (line == endLine) ? endCol - 1 : lineEndPos - lineStartPos;

                if (selectionStart < selectionEnd)
                {
                    // Calculate pixel positions
                    string lineText = GetLineText(content, line);
                    string beforeSelection = lineText.Substring(0, Math.Min(selectionStart, lineText.Length));
                    string selectionText = lineText.Substring(selectionStart, Math.Min(selectionEnd - selectionStart, lineText.Length - selectionStart));

                    Vector2 beforeSize = Raylib.MeasureTextEx(_font, beforeSelection, (float)_fontSize, 1.0f);
                    Vector2 selectionSize = Raylib.MeasureTextEx(_font, selectionText, (float)_fontSize, 1.0f);

                    int selectionX = x + (int)beforeSize.X;
                    int selectionWidth = (int)selectionSize.X;

                    Raylib.DrawRectangle(selectionX, lineY, selectionWidth, lineHeight, _selectionColor);
                }
            }
        }

        private void DrawText(int x, int y, int width, int height, string content, int scrollY, int lineHeight, bool enableSyntaxHighlighting)
        {
            string[] lines = content.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                int lineY = y + (i * lineHeight) - scrollY;
                
                // Skip lines not visible
                if (lineY + lineHeight < y || lineY > y + height)
                    continue;

                string line = lines[i];
                
                if (enableSyntaxHighlighting)
                {
                    // Use syntax highlighting
                    var tokens = RayEdit.Core.Text.SyntaxHighlighter.TokenizeLine(line, 0);
                    float currentX = x;
                    
                    foreach (var token in tokens)
                    {
                        Raylib.DrawTextEx(_font, token.Text, new Vector2(currentX, lineY + _lineSpacing / 2), (float)_fontSize, 1.0f, token.Color);
                        Vector2 tokenSize = Raylib.MeasureTextEx(_font, token.Text, (float)_fontSize, 1.0f);
                        currentX += tokenSize.X;
                    }
                }
                else
                {
                    // Regular text rendering
                    Raylib.DrawTextEx(_font, line, new Vector2(x, lineY + _lineSpacing / 2), (float)_fontSize, 1.0f, _textColor);
                }
            }
        }

        private void DrawCursor(int x, int y, int cursorIndex, string content, int scrollY, int lineHeight)
        {
            var (cursorLine, cursorColumn) = GetCursorPosition(content, cursorIndex);
            
            int cursorY = y + ((cursorLine - 1) * lineHeight) - scrollY;
            
            // Only draw cursor if it's visible
            if (cursorY >= y && cursorY < y + lineHeight)
            {
                string lineText = GetLineText(content, cursorLine);
                string textBeforeCursor = lineText.Substring(0, Math.Min(cursorColumn - 1, lineText.Length));
                
                Vector2 textSize = Raylib.MeasureTextEx(_font, textBeforeCursor, (float)_fontSize, 1.0f);
                int cursorX = x + (int)textSize.X;
                
                Raylib.DrawRectangle(cursorX, cursorY, 2, lineHeight, _cursorColor);
            }
        }

        private (int line, int column) GetCursorPosition(string content, int index)
        {
            int line = 1;
            int column = 1;
            
            for (int i = 0; i < index && i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
            
            return (line, column);
        }

        private int GetLineStartPosition(string content, int lineNumber)
        {
            int currentLine = 1;
            for (int i = 0; i < content.Length; i++)
            {
                if (currentLine == lineNumber)
                    return i;
                if (content[i] == '\n')
                    currentLine++;
            }
            return content.Length;
        }

        private int GetLineEndPosition(string content, int lineNumber)
        {
            int startPos = GetLineStartPosition(content, lineNumber);
            for (int i = startPos; i < content.Length; i++)
            {
                if (content[i] == '\n')
                    return i;
            }
            return content.Length;
        }

        private string GetLineText(string content, int lineNumber)
        {
            string[] lines = content.Split('\n');
            if (lineNumber > 0 && lineNumber <= lines.Length)
                return lines[lineNumber - 1];
            return "";
        }
    }
}