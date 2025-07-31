using System.Numerics;
using RayEdit.Core.Text;
using Raylib_cs;

namespace RayEdit.UI.Controls
{
    /// <summary>
    /// Gutter component for displaying line numbers
    /// </summary>
    public class Gutter
    {
        private readonly int _width;
        private Color _backgroundColor;
        private Color _textColor;
        private Color _currentLineColor;
        private Font _font;
        private int _fontSize;
        private int _padding;

        public int Width => _width;

        public Gutter(int width = 60)
        {
            _width = width;
            _backgroundColor = Color.DarkGray;
            _textColor = Color.LightGray;
            _currentLineColor = Color.White;
            _font = Raylib.GetFontDefault();
            _fontSize = 16;
            _padding = 5;
        }

        public void SetTheme(Color backgroundColor, Color textColor, Color currentLineColor, Font font, int fontSize)
        {
            _backgroundColor = backgroundColor;
            _textColor = textColor;
            _currentLineColor = currentLineColor;
            _font = font;
            _fontSize = fontSize;
        }

        public void Draw(int x, int y, int height, TextBuffer textBuffer, int scrollY, int lineHeight)
        {
            // Draw background
            Raylib.DrawRectangle(x, y, _width, height, _backgroundColor);

            string content = textBuffer.Content;
            int cursorIndex = textBuffer.CursorIndex;

            // Calculate current line number
            int currentLineNumber = 1;
            for (int i = 0; i < cursorIndex && i < content.Length; i++)
            {
                if (content[i] == '\n')
                    currentLineNumber++;
            }

            // Calculate visible line range
            int firstVisibleLine = Math.Max(1, (scrollY / lineHeight) + 1);
            int lastVisibleLine = firstVisibleLine + (height / lineHeight) + 1;

            // Count total lines
            int totalLines = 1;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                    totalLines++;
            }

            lastVisibleLine = Math.Min(lastVisibleLine, totalLines);

            // Draw line numbers
            for (int lineNum = firstVisibleLine; lineNum <= lastVisibleLine; lineNum++)
            {
                int lineY = y + ((lineNum - 1) * lineHeight) - scrollY;
                
                // Skip lines that are not visible
                if (lineY + lineHeight < y || lineY > y + height)
                    continue;

                string lineNumberText = lineNum.ToString();
                Vector2 textSize = Raylib.MeasureTextEx(_font, lineNumberText, (float)_fontSize, 1.0f);
                
                // Use different color for current line
                Color color = (lineNum == currentLineNumber) ? _currentLineColor : _textColor;
                
                // Right-align the line numbers
                float textX = x + _width - textSize.X - _padding;
                float textY = lineY + (lineHeight - textSize.Y) / 2;
                
                Raylib.DrawTextEx(_font, lineNumberText, new Vector2(textX, textY), _fontSize, 1, color);
            }

            // Draw separator line
            Raylib.DrawLine(x + _width - 1, y, x + _width - 1, y + height, Color.Gray);
        }
    }
}