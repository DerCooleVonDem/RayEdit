using System.Numerics;
using RayEdit.Core.Text;
using Raylib_cs;

namespace RayEdit.UI.Controls
{
    /// <summary>
    /// Status bar component that displays editor information like cursor position, file status, etc.
    /// </summary>
    public class StatusBar
    {
        private readonly int _height;
        private Color _backgroundColor;
        private Color _textColor;
        private Font _font;
        private int _fontSize;

        public int Height => _height;

        public StatusBar(int height = 30)
        {
            _height = height;
            _backgroundColor = Color.Blue;
            _textColor = Color.White;
            _font = Raylib.GetFontDefault();
            _fontSize = 16;
        }

        public void SetTheme(Color backgroundColor, Color textColor, Font font, int fontSize)
        {
            _backgroundColor = backgroundColor;
            _textColor = textColor;
            _font = font;
            _fontSize = fontSize;
        }

        public void Draw(int x, int y, int width, TextBuffer textBuffer, string fileName = null, bool isDirty = false)
        {
            // Draw background
            Raylib.DrawRectangle(x, y, width, _height, _backgroundColor);

            // Calculate cursor line and column
            string content = textBuffer.Content;
            int cursorIndex = textBuffer.CursorIndex;
            
            int line = 1;
            int column = 1;
            
            for (int i = 0; i < cursorIndex && i < content.Length; i++)
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

            // Status text components
            string fileStatus = fileName ?? "Untitled";
            if (isDirty) fileStatus += " •";
            
            string positionInfo = $"Ln {line}, Col {column}";
            string selectionInfo = "";
            
            if (textBuffer.HasSelection())
            {
                var (start, end) = textBuffer.GetSelectionRange();
                int selectedChars = end - start;
                selectionInfo = $" | {selectedChars} selected";
            }

            // Draw file status (left side)
            Vector2 fileStatusSize = Raylib.MeasureTextEx(_font, fileStatus, (float)_fontSize, 1.0f);
            Raylib.DrawTextEx(_font, fileStatus, new Vector2(x + 10, y + (_height - fileStatusSize.Y) / 2), (float)_fontSize, 1.0f, _textColor);

            // Draw position info (right side)
            string rightText = positionInfo + selectionInfo;
            Vector2 rightTextSize = Raylib.MeasureTextEx(_font, rightText, (float)_fontSize, 1.0f);
            Raylib.DrawTextEx(_font, rightText, new Vector2(x + width - rightTextSize.X - 10, y + (_height - rightTextSize.Y) / 2), (float)_fontSize, 1.0f, _textColor);
        }
    }
}