using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace TextEditor
{
    /// <summary>
    /// Spotlight-inspired command bar with auto-completion dropdown
    /// </summary>
    public class CommandBar
    {
        private readonly CommandRegistry _commandRegistry;
        private readonly Font _font;
        
        // State
        private bool _isVisible = false;
        private string _inputText = "";
        private int _selectedIndex = 0;
        private List<Command> _filteredCommands = new List<Command>();
        
        // Visual settings
        private const int FontSize = 18;
        private const int ItemHeight = 40;
        private const int MaxVisibleItems = 10;
        private const int Padding = 15;
        private const int BorderRadius = 12;
        
        // Colors
        private readonly Color BackgroundColor = new Color(40, 40, 40, 240);
        private readonly Color InputBackgroundColor = new Color(60, 60, 60, 255);
        private readonly Color SelectedItemColor = new Color(80, 120, 200, 255);
        private readonly Color TextColor = Color.White;
        private readonly Color DescriptionColor = new Color(180, 180, 180, 255);
        private readonly Color CategoryColor = new Color(120, 160, 255, 255);
        private readonly Color BorderColor = new Color(80, 80, 80, 255);
        
        public bool IsVisible => _isVisible;
        public event Action<string> OnCommandExecuted;
        
        public CommandBar(CommandRegistry commandRegistry, Font font)
        {
            _commandRegistry = commandRegistry;
            _font = font;
            UpdateFilteredCommands();
        }
        
        public void Show()
        {
            _isVisible = true;
            _inputText = "";
            _selectedIndex = 0;
            UpdateFilteredCommands();
        }
        
        public void Hide()
        {
            _isVisible = false;
            _inputText = "";
            _selectedIndex = 0;
        }
        
        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }
        
        public void HandleInput()
        {
            if (!_isVisible) return;
            
            // Handle text input
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if (key >= 32 && key <= 125) // Printable characters
                {
                    _inputText += (char)key;
                    _selectedIndex = 0;
                    UpdateFilteredCommands();
                }
                key = Raylib.GetCharPressed();
            }
            
            // Handle special keys
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                if (_inputText.Length > 0)
                {
                    _inputText = _inputText.Substring(0, _inputText.Length - 1);
                    _selectedIndex = 0;
                    UpdateFilteredCommands();
                }
            }
            
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                Hide();
            }
            
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                ExecuteSelectedCommand();
            }
            
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            }
            
            if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                _selectedIndex = Math.Min(_filteredCommands.Count - 1, _selectedIndex + 1);
            }
            
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                if (_filteredCommands.Count > 0)
                {
                    var selectedCommand = _filteredCommands[_selectedIndex];
                    _inputText = selectedCommand.Name + " ";
                    UpdateFilteredCommands();
                }
            }
        }
        
        public void Draw()
        {
            if (!_isVisible) return;
            
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            
            // Calculate dimensions
            int barWidth = Math.Min(800, screenWidth - 80);
            int inputHeight = 50;
            int dropdownHeight = Math.Min(_filteredCommands.Count * ItemHeight, MaxVisibleItems * ItemHeight);
            int totalHeight = inputHeight + dropdownHeight + (dropdownHeight > 0 ? Padding : 0);
            
            int x = (screenWidth - barWidth) / 2;
            int y = screenHeight / 4;
            
            // Draw main background
            Raylib.DrawRectangleRounded(new Rectangle(x, y, barWidth, totalHeight), (float)BorderRadius / Math.Min(barWidth, totalHeight), 16, BackgroundColor);
            Raylib.DrawRectangleRoundedLines(new Rectangle(x, y, barWidth, totalHeight), (float)BorderRadius / Math.Min(barWidth, totalHeight), 16, BorderColor);
            
            // Draw input field
            Raylib.DrawRectangleRounded(new Rectangle(x + Padding, y + Padding, barWidth - 2 * Padding, inputHeight - Padding), 0.1f, 16, InputBackgroundColor);
            
            // Draw input text
            string displayText = _inputText;
            if (string.IsNullOrEmpty(displayText))
            {
                displayText = "Type a command...";
                Raylib.DrawTextEx(_font, displayText, new Vector2(x + Padding * 2, y + Padding + 12), FontSize, 1, DescriptionColor);
            }
            else
            {
                Raylib.DrawTextEx(_font, displayText, new Vector2(x + Padding * 2, y + Padding + 12), FontSize, 1, TextColor);
            }
            
            // Draw cursor
            if (!string.IsNullOrEmpty(_inputText))
            {
                Vector2 textSize = Raylib.MeasureTextEx(_font, _inputText, FontSize, 1);
                int cursorX = x + Padding * 2 + (int)textSize.X;
                int cursorY = y + Padding + 10;
                Raylib.DrawRectangle(cursorX, cursorY, 2, FontSize + 4, TextColor);
            }
            
            // Draw dropdown
            if (_filteredCommands.Count > 0)
            {
                int dropdownY = y + inputHeight + Padding;
                DrawDropdown(x, dropdownY, barWidth, dropdownHeight);
            }
        }
        
        private void DrawDropdown(int x, int y, int width, int height)
        {
            // Draw dropdown background
            Raylib.DrawRectangleRounded(new Rectangle(x + Padding, y, width - 2 * Padding, height), 0.1f, 16, InputBackgroundColor);
            
            int visibleItems = Math.Min(_filteredCommands.Count, MaxVisibleItems);
            int startIndex = Math.Max(0, _selectedIndex - MaxVisibleItems / 2);
            startIndex = Math.Min(startIndex, _filteredCommands.Count - visibleItems);
            
            for (int i = 0; i < visibleItems; i++)
            {
                int commandIndex = startIndex + i;
                if (commandIndex >= _filteredCommands.Count) break;
                
                var command = _filteredCommands[commandIndex];
                int itemY = y + i * ItemHeight;
                
                // Draw selection highlight
                if (commandIndex == _selectedIndex)
                {
                    Raylib.DrawRectangleRounded(new Rectangle(x + Padding + 4, itemY + 2, width - 2 * Padding - 8, ItemHeight - 4), 0.1f, 16, SelectedItemColor);
                }
                
                // Draw command name
                Raylib.DrawTextEx(_font, command.Name, new Vector2(x + Padding * 2, itemY + 6), FontSize, 1, TextColor);
                
                // Draw category
                Vector2 nameSize = Raylib.MeasureTextEx(_font, command.Name, FontSize, 1);
                string categoryText = $"[{command.Category}]";
                Raylib.DrawTextEx(_font, categoryText, new Vector2(x + Padding * 2 + nameSize.X + 10, itemY + 6), FontSize - 2, 1, CategoryColor);
                
                // Draw description
                if (!string.IsNullOrEmpty(command.Description))
                {
                    string description = command.Description;
                    if (description.Length > 50)
                    {
                        description = description.Substring(0, 47) + "...";
                    }
                    Raylib.DrawTextEx(_font, description, new Vector2(x + Padding * 2, itemY + 6 + FontSize + 2), FontSize - 4, 1, DescriptionColor);
                }
            }
            
            // Draw scrollbar if needed
            if (_filteredCommands.Count > MaxVisibleItems)
            {
                DrawScrollbar(x + width - Padding - 8, y, 6, height);
            }
        }
        
        private void DrawScrollbar(int x, int y, int width, int height)
        {
            // Draw scrollbar track
            Raylib.DrawRectangle(x, y, width, height, new Color(60, 60, 60, 255));
            
            // Calculate thumb position and size
            float thumbHeight = (float)MaxVisibleItems / _filteredCommands.Count * height;
            float thumbY = (float)Math.Max(0, _selectedIndex - MaxVisibleItems / 2) / (_filteredCommands.Count - MaxVisibleItems) * (height - thumbHeight);
            
            // Draw scrollbar thumb
            Raylib.DrawRectangle(x, y + (int)thumbY, width, (int)thumbHeight, new Color(120, 120, 120, 255));
        }
        
        private void UpdateFilteredCommands()
        {
            _filteredCommands = _commandRegistry.SearchCommands(_inputText, 20);
            _selectedIndex = Math.Min(_selectedIndex, Math.Max(0, _filteredCommands.Count - 1));
        }
        
        private void ExecuteSelectedCommand()
        {
            if (_filteredCommands.Count > 0 && _selectedIndex < _filteredCommands.Count)
            {
                string commandLine = _inputText;
                if (string.IsNullOrWhiteSpace(commandLine))
                {
                    commandLine = _filteredCommands[_selectedIndex].Name;
                }
                
                bool success = _commandRegistry.ExecuteCommand(commandLine);
                if (success)
                {
                    OnCommandExecuted?.Invoke(commandLine);
                    Hide();
                }
                else
                {
                    Console.WriteLine($"Failed to execute command: {commandLine}");
                }
            }
        }
        

    }
}