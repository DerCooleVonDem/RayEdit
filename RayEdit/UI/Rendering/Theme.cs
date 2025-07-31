using System.Text.Json;
using Raylib_cs;

namespace RayEdit.UI.Rendering
{
    /// <summary>
    /// Represents a theme configuration for the editor
    /// </summary>
    public class Theme
    {
        public string Name { get; set; } = "Default";
        
        // Colors
        public Color BackgroundColor { get; set; } = new Color(45, 45, 48, 255);
        public Color ForegroundColor { get; set; } = new Color(220, 220, 220, 255);
        public Color SelectionColor { get; set; } = new Color(38, 79, 120, 255);
        public Color CursorColor { get; set; } = Color.White;
        public Color LineNumberColor { get; set; } = new Color(133, 133, 133, 255);
        public Color CurrentLineColor { get; set; } = new Color(58, 58, 60, 255);
        public Color GutterColor { get; set; } = new Color(45, 45, 48, 255);
        public Color BorderColor { get; set; } = new Color(63, 63, 70, 255);
        
        // Syntax colors
        public Color KeywordColor { get; set; } = new Color(86, 156, 214, 255);
        public Color StringColor { get; set; } = new Color(206, 145, 120, 255);
        public Color CommentColor { get; set; } = new Color(106, 153, 85, 255);
        public Color NumberColor { get; set; } = new Color(181, 206, 168, 255);
        public Color OperatorColor { get; set; } = new Color(212, 212, 212, 255);
        public Color IdentifierColor { get; set; } = new Color(220, 220, 220, 255);
        public Color TypeColor { get; set; } = new Color(78, 201, 176, 255);
        public Color FunctionColor { get; set; } = new Color(220, 220, 170, 255);
        
        // UI colors
        public Color MenuBackgroundColor { get; set; } = new Color(45, 45, 48, 255);
        public Color MenuForegroundColor { get; set; } = new Color(220, 220, 220, 255);
        public Color MenuHoverColor { get; set; } = new Color(58, 58, 60, 255);
        public Color StatusBarBackgroundColor { get; set; } = new Color(0, 122, 204, 255);
        public Color StatusBarForegroundColor { get; set; } = Color.White;
        public Color CommandBarBackgroundColor { get; set; } = new Color(30, 30, 30, 255);
        public Color CommandBarForegroundColor { get; set; } = new Color(220, 220, 220, 255);
        
        // Font settings
        public Font Font { get; set; }
        public string FontPath { get; set; } = "Assets/Fonts/FiraCode-Regular.ttf";
        public int FontSize { get; set; } = 20;
        public int LineSpacing { get; set; } = 5;

        public Theme()
        {
            Font = Raylib.GetFontDefault();
        }

        public void LoadFont()
        {
            if (!string.IsNullOrEmpty(FontPath) && File.Exists(FontPath))
            {
                Font = Raylib.LoadFont(FontPath);
            }
            else
            {
                Font = Raylib.GetFontDefault();
            }
        }

        public static Theme LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new Theme();

                string jsonContent = File.ReadAllText(filePath);
                var themeData = JsonSerializer.Deserialize<ThemeData>(jsonContent);
                
                var theme = new Theme();
                
                // Apply colors
                if (themeData.colors != null)
                {
                    theme.BackgroundColor = ParseColor(themeData.colors.background, theme.BackgroundColor);
                    theme.ForegroundColor = ParseColor(themeData.colors.foreground, theme.ForegroundColor);
                    theme.SelectionColor = ParseColor(themeData.colors.selection, theme.SelectionColor);
                    theme.CursorColor = ParseColor(themeData.colors.cursor, theme.CursorColor);
                    theme.LineNumberColor = ParseColor(themeData.colors.lineNumber, theme.LineNumberColor);
                    theme.CurrentLineColor = ParseColor(themeData.colors.currentLine, theme.CurrentLineColor);
                    theme.GutterColor = ParseColor(themeData.colors.gutter, theme.GutterColor);
                    theme.BorderColor = ParseColor(themeData.colors.border, theme.BorderColor);
                }
                
                // Apply syntax colors
                if (themeData.syntax != null)
                {
                    theme.KeywordColor = ParseColor(themeData.syntax.keyword, theme.KeywordColor);
                    theme.StringColor = ParseColor(themeData.syntax.@string, theme.StringColor);
                    theme.CommentColor = ParseColor(themeData.syntax.comment, theme.CommentColor);
                    theme.NumberColor = ParseColor(themeData.syntax.number, theme.NumberColor);
                    theme.OperatorColor = ParseColor(themeData.syntax.@operator, theme.OperatorColor);
                    theme.IdentifierColor = ParseColor(themeData.syntax.identifier, theme.IdentifierColor);
                    theme.TypeColor = ParseColor(themeData.syntax.type, theme.TypeColor);
                    theme.FunctionColor = ParseColor(themeData.syntax.function, theme.FunctionColor);
                }
                
                // Apply UI colors
                if (themeData.ui != null)
                {
                    theme.MenuBackgroundColor = ParseColor(themeData.ui.menuBackground, theme.MenuBackgroundColor);
                    theme.MenuForegroundColor = ParseColor(themeData.ui.menuForeground, theme.MenuForegroundColor);
                    theme.MenuHoverColor = ParseColor(themeData.ui.menuHover, theme.MenuHoverColor);
                    theme.StatusBarBackgroundColor = ParseColor(themeData.ui.statusBarBackground, theme.StatusBarBackgroundColor);
                    theme.StatusBarForegroundColor = ParseColor(themeData.ui.statusBarForeground, theme.StatusBarForegroundColor);
                    theme.CommandBarBackgroundColor = ParseColor(themeData.ui.commandBarBackground, theme.CommandBarBackgroundColor);
                    theme.CommandBarForegroundColor = ParseColor(themeData.ui.commandBarForeground, theme.CommandBarForegroundColor);
                }
                
                // Apply font settings
                if (themeData.font != null)
                {
                    if (!string.IsNullOrEmpty(themeData.font.family))
                        theme.FontPath = "Assets/Fonts/" + themeData.font.family;
                    if (themeData.font.size > 0)
                        theme.FontSize = themeData.font.size;
                    if (themeData.font.lineSpacing > 0)
                        theme.LineSpacing = themeData.font.lineSpacing;
                }
                
                theme.Name = themeData.name ?? "Default";
                theme.LoadFont();
                
                return theme;
            }
            catch
            {
                return new Theme();
            }
        }

        private static Color ParseColor(string hexColor, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return defaultColor;
                
            try
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                    
                if (hexColor.Length == 6)
                {
                    int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
                    return new Color(r, g, b, 255);
                }
            }
            catch
            {
                // Fall back to default color
            }
            
            return defaultColor;
        }

        // Helper classes for JSON deserialization
        private class ThemeData
        {
            public string name { get; set; }
            public ColorData colors { get; set; }
            public SyntaxData syntax { get; set; }
            public UIData ui { get; set; }
            public FontData font { get; set; }
        }

        private class ColorData
        {
            public string background { get; set; }
            public string foreground { get; set; }
            public string selection { get; set; }
            public string cursor { get; set; }
            public string lineNumber { get; set; }
            public string currentLine { get; set; }
            public string gutter { get; set; }
            public string border { get; set; }
        }

        private class SyntaxData
        {
            public string keyword { get; set; }
            public string @string { get; set; }
            public string comment { get; set; }
            public string number { get; set; }
            public string @operator { get; set; }
            public string identifier { get; set; }
            public string type { get; set; }
            public string function { get; set; }
        }

        private class UIData
        {
            public string menuBackground { get; set; }
            public string menuForeground { get; set; }
            public string menuHover { get; set; }
            public string statusBarBackground { get; set; }
            public string statusBarForeground { get; set; }
            public string commandBarBackground { get; set; }
            public string commandBarForeground { get; set; }
        }

        private class FontData
        {
            public string family { get; set; }
            public int size { get; set; }
            public int lineSpacing { get; set; }
        }
    }
}