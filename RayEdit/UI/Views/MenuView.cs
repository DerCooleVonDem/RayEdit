using System.Numerics;
using Raylib_cs;

namespace RayEdit.UI.Views
{
    /// <summary>
    /// The main menu view for file selection and navigation.
    /// </summary>
    public class MenuView : IView
    {
        // Events
        public event Action<string> OnFileSelected;

        // UI State
        private string[] _files;
        private int _selectedIndex = 0;
        private string _currentDirectory;
        private Font _font;
        
        // Layout constants
        private const int FontSize = 20;
        private const int LineHeight = 30;
        private const int MarginX = 50;
        private const int MarginY = 50;
        private const int MaxVisibleItems = 20;
        
        // Scrolling
        private int _scrollOffset = 0;

        public MenuView()
        {
            _currentDirectory = Directory.GetCurrentDirectory();
            RefreshFileList();
        }

        public void Load()
        {
            // Load the font (same as EditorView for consistency)
            _font = Raylib.LoadFont("Assets/Fonts/FiraCode-Regular.ttf");
            RefreshFileList();
        }

        public void Unload()
        {
            // Unload resources
            Raylib.UnloadFont(_font);
        }

        public void Update()
        {
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            // Navigation
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                EnsureSelectedItemVisible();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                _selectedIndex = Math.Min(_files.Length - 1, _selectedIndex + 1);
                EnsureSelectedItemVisible();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                SelectCurrentItem();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                NavigateToParentDirectory();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.F5))
            {
                RefreshFileList();
            }
        }

        private void EnsureSelectedItemVisible()
        {
            if (_selectedIndex < _scrollOffset)
            {
                _scrollOffset = _selectedIndex;
            }
            else if (_selectedIndex >= _scrollOffset + MaxVisibleItems)
            {
                _scrollOffset = _selectedIndex - MaxVisibleItems + 1;
            }
        }

        private void SelectCurrentItem()
        {
            if (_files.Length == 0) return;

            string selectedItem = _files[_selectedIndex];
            
            // Check if it's a directory (starts with [DIR])
            if (selectedItem.StartsWith("[DIR] "))
            {
                // Remove the [DIR] prefix to get the actual directory name
                string dirName = selectedItem.Substring(6);
                string fullPath = Path.Combine(_currentDirectory, dirName);
                
                if (Directory.Exists(fullPath))
                {
                    // Navigate into directory
                    _currentDirectory = fullPath;
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    RefreshFileList();
                }
            }
            else
            {
                // It's a file
                string fullPath = Path.Combine(_currentDirectory, selectedItem);
                if (File.Exists(fullPath))
                {
                    // Select file for editing
                    OnFileSelected?.Invoke(fullPath);
                }
            }
        }

        private void NavigateToParentDirectory()
        {
            DirectoryInfo parent = Directory.GetParent(_currentDirectory);
            if (parent != null)
            {
                _currentDirectory = parent.FullName;
                _selectedIndex = 0;
                _scrollOffset = 0;
                RefreshFileList();
            }
        }

        private void RefreshFileList()
        {
            try
            {
                var directories = Directory.GetDirectories(_currentDirectory)
                    .Select(d => "[DIR] " + Path.GetFileName(d))
                    .ToArray();

                var files = Directory.GetFiles(_currentDirectory)
                    .Where(f => IsTextFile(f))
                    .Select(f => Path.GetFileName(f))
                    .ToArray();

                _files = directories.Concat(files).ToArray();
                
                // Ensure selected index is within bounds
                if (_selectedIndex >= _files.Length)
                {
                    _selectedIndex = Math.Max(0, _files.Length - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading directory: {ex.Message}");
                _files = new string[0];
            }
        }

        private bool IsTextFile(string filePath)
        {
            string[] textExtensions = { ".txt", ".cs", ".js", ".html", ".css", ".json", ".xml", ".md", ".py", ".java", ".cpp", ".h" };
            string extension = Path.GetExtension(filePath).ToLower();
            return textExtensions.Contains(extension);
        }

        public void Draw()
        {
            // Draw title
            string title = "RayEdit - File Browser";
            Raylib.DrawTextEx(_font, title, new Vector2(MarginX, 20), FontSize + 5, 1, Color.White);

            // Draw current directory
            string dirText = $"Directory: {_currentDirectory}";
            Raylib.DrawTextEx(_font, dirText, new Vector2(MarginX, 60), FontSize - 4, 1, Color.LightGray);

            // Draw instructions
            string instructions = "↑↓: Navigate | Enter: Select | Backspace: Parent Dir | F5: Refresh";
            Raylib.DrawTextEx(_font, instructions, new Vector2(MarginX, 90), FontSize - 6, 1, Color.Gray);

            // Draw file list
            int startY = MarginY + 60;
            int visibleItems = Math.Min(MaxVisibleItems, _files.Length - _scrollOffset);

            for (int i = 0; i < visibleItems; i++)
            {
                int fileIndex = _scrollOffset + i;
                string fileName = _files[fileIndex];
                int yPos = startY + i * LineHeight;

                // Highlight selected item
                Color textColor = Color.White;
                if (fileIndex == _selectedIndex)
                {
                    Raylib.DrawRectangle(MarginX - 5, yPos - 2, Raylib.GetScreenWidth() - 2 * MarginX + 10, LineHeight - 2, Color.DarkBlue);
                    textColor = Color.Yellow;
                }

                // Different colors for directories and files
                if (fileName.StartsWith("[DIR]"))
                {
                    textColor = fileIndex == _selectedIndex ? Color.Yellow : Color.SkyBlue;
                }

                Raylib.DrawTextEx(_font, fileName, new Vector2(MarginX, yPos), FontSize - 2, 1, textColor);
            }

            // Draw scroll indicator if needed
            if (_files.Length > MaxVisibleItems)
            {
                int scrollBarHeight = (int)((float)MaxVisibleItems / _files.Length * (MaxVisibleItems * LineHeight));
                int scrollBarY = startY + (int)((float)_scrollOffset / _files.Length * (MaxVisibleItems * LineHeight));
                
                Raylib.DrawRectangle(Raylib.GetScreenWidth() - 20, startY, 10, MaxVisibleItems * LineHeight, Color.DarkGray);
                Raylib.DrawRectangle(Raylib.GetScreenWidth() - 20, scrollBarY, 10, scrollBarHeight, Color.White);
            }

            // Draw file count
            string countText = $"{_files.Length} items";
            Raylib.DrawTextEx(_font, countText, new Vector2(MarginX, Raylib.GetScreenHeight() - 40), FontSize - 4, 1, Color.Gray);
        }
    }
}