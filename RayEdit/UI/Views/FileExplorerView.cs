using System.Numerics;
using RayEdit.Core.Commands;
using RayEdit.UI.Controls;
using Raylib_cs;

namespace RayEdit.UI.Views
{
    public class FileItem
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string FullPath { get; set; }
        
        public string GetSizeString()
        {
            if (IsDirectory) return "<DIR>";
            
            if (Size < 1024) return $"{Size} B";
            if (Size < 1024 * 1024) return $"{Size / 1024.0:F1} KB";
            if (Size < 1024 * 1024 * 1024) return $"{Size / (1024.0 * 1024.0):F1} MB";
            return $"{Size / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
        
        public string GetDateString()
        {
            return LastModified.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public class FileExplorerView : IView
    {
        public event Action OnBackToMenu;
        public event Action<string> OnFileSelected;

        private Font _font;
        private CommandRegistry _commandRegistry;
        private CommandBar _commandBar;
        
        private string _currentDirectory = Directory.GetCurrentDirectory();
        private List<FileItem> _items = new List<FileItem>();
        private int _selectedIndex = 0;
        private int _scrollOffset = 0;

        private const int FontSize = 16;
        private const int LineHeight = 24;
        private const int Padding = 20;

        public void Load()
        {
            _font = Raylib.LoadFont("Assets/Fonts/FiraCode-Regular.ttf");
            InitializeCommandSystem();
            RefreshDirectory();
        }

        public void Unload()
        {
            Raylib.UnloadFont(_font);
        }

        public void Update()
        {
            // Handle command bar input first
            if (_commandBar.IsVisible)
            {
                _commandBar.HandleInput();
            }
            else
            {
                HandleInput();
            }
        }

        public void Draw()
        {
            Raylib.ClearBackground(new Color(30, 30, 30, 255));

            // Draw header
            string headerText = $"File Explorer - {_currentDirectory}";
            Raylib.DrawTextEx(_font, headerText, new Vector2(Padding, Padding), FontSize + 2, 1, Color.White);

            // Draw column headers
            int headerY = Padding + 30;
            Raylib.DrawTextEx(_font, "Name", new Vector2(Padding, headerY), FontSize - 2, 1, new Color(180, 180, 180, 255));
            Raylib.DrawTextEx(_font, "Size", new Vector2(400, headerY), FontSize - 2, 1, new Color(180, 180, 180, 255));
            Raylib.DrawTextEx(_font, "Modified", new Vector2(500, headerY), FontSize - 2, 1, new Color(180, 180, 180, 255));
            
            // Draw separator line
            Raylib.DrawLine(Padding, headerY + 18, Raylib.GetScreenWidth() - Padding, headerY + 18, new Color(100, 100, 100, 255));

            // Draw files and directories
            int startY = headerY + 25;
            int visibleItems = (Raylib.GetScreenHeight() - startY - Padding - 40) / LineHeight;
            
            for (int i = 0; i < Math.Min(visibleItems, _items.Count); i++)
            {
                int itemIndex = _scrollOffset + i;
                if (itemIndex >= _items.Count) break;

                var item = _items[itemIndex];
                int y = startY + i * LineHeight;
                
                Color textColor = item.IsDirectory ? new Color(100, 150, 255, 255) : Color.White;
                string prefix = item.IsDirectory ? "[DIR] " : "[FILE] ";

                // Highlight selected item
                if (itemIndex == _selectedIndex)
                {
                    Raylib.DrawRectangle(Padding - 5, y - 2, Raylib.GetScreenWidth() - 2 * Padding + 10, LineHeight, new Color(80, 120, 200, 100));
                }

                // Draw name
                Raylib.DrawTextEx(_font, prefix + item.Name, new Vector2(Padding, y), FontSize, 1, textColor);
                
                // Draw size
                Raylib.DrawTextEx(_font, item.GetSizeString(), new Vector2(400, y), FontSize, 1, textColor);
                
                // Draw modified date
                Raylib.DrawTextEx(_font, item.GetDateString(), new Vector2(500, y), FontSize, 1, textColor);
            }

            // Draw command bar
            _commandBar?.Draw();

            // Draw help text
            if (!_commandBar.IsVisible)
            {
                string helpText = "Ctrl+P: Command Palette | Enter: Open | Backspace: Parent Dir | Esc: Back to Menu";
                Raylib.DrawTextEx(_font, helpText, new Vector2(Padding, Raylib.GetScreenHeight() - 30), 12, 1, new Color(150, 150, 150, 255));
            }
        }

        private void HandleInput()
        {
            bool ctrlPressed = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);

            // Command palette
            if (ctrlPressed && Raylib.IsKeyPressed(KeyboardKey.P))
            {
                _commandBar.Show();
                return;
            }

            // Navigation
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                EnsureSelectedItemVisible();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                _selectedIndex = Math.Min(_items.Count - 1, _selectedIndex + 1);
                EnsureSelectedItemVisible();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                OpenSelectedItem();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                NavigateToParentDirectory();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                OnBackToMenu?.Invoke();
            }
        }

        private void InitializeCommandSystem()
        {
            _commandRegistry = new CommandRegistry();
            _commandBar = new CommandBar(_commandRegistry, _font);
            
            // Set context to FileExplorer
            _commandRegistry.SetContext(CommandContext.FileExplorer);
            
            RegisterFileExplorerCommands();
            
            _commandBar.OnCommandExecuted += (commandLine) =>
            {
                Console.WriteLine($"Executed command: {commandLine}");
            };
        }

        private void RegisterFileExplorerCommands()
        {
            // Global commands
            _commandRegistry.RegisterCommand("help", "Show available commands", 
                args => {
                    var categories = _commandRegistry.GetCategories();
                    Console.WriteLine("Available commands:");
                    foreach (var category in categories)
                    {
                        Console.WriteLine($"\n{category}:");
                        var commands = _commandRegistry.GetCommandsByCategory(category);
                        foreach (var cmd in commands)
                        {
                            Console.WriteLine($"  {cmd.Usage} - {cmd.Description}");
                        }
                    }
                }, "Help", new[] { "h", "?" }, "help", CommandContext.Global);

            _commandRegistry.RegisterCommand("exit", "Exit to main menu", 
                args => {
                    OnBackToMenu?.Invoke();
                }, "Application", new[] { "quit", "q", "back" }, "exit", CommandContext.Global);

            // File Explorer specific commands
            _commandRegistry.RegisterCommand("open", "Open the selected file or directory", 
                args => {
                    OpenSelectedItem();
                }, "Navigation", new[] { "o" }, "open", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("cd", "Change to a specific directory", 
                args => {
                    if (args.Length > 0)
                    {
                        string targetDir = args[0];
                        if (targetDir == "..")
                        {
                            NavigateToParentDirectory();
                        }
                        else
                        {
                            NavigateToDirectory(targetDir);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Current directory: {_currentDirectory}");
                    }
                }, "Navigation", new[] { "chdir" }, "cd [directory]", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("ls", "List files and directories", 
                args => {
                    RefreshDirectory();
                    int dirCount = _items.Count(item => item.IsDirectory);
                    int fileCount = _items.Count(item => !item.IsDirectory);
                    Console.WriteLine($"Listed {dirCount} directories and {fileCount} files");
                }, "Navigation", new[] { "list", "dir" }, "ls", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("mkdir", "Create a new directory", 
                args => {
                    if (args.Length > 0)
                    {
                        string dirName = args[0];
                        try
                        {
                            string fullPath = Path.Combine(_currentDirectory, dirName);
                            Directory.CreateDirectory(fullPath);
                            RefreshDirectory();
                            Console.WriteLine($"Directory '{dirName}' created!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating directory: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Usage: mkdir <directory-name>");
                    }
                }, "File Operations", new[] { "md" }, "mkdir <directory-name>", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("touch", "Create a new empty file", 
                args => {
                    if (args.Length > 0)
                    {
                        string fileName = args[0];
                        try
                        {
                            string fullPath = Path.Combine(_currentDirectory, fileName);
                            File.WriteAllText(fullPath, "");
                            RefreshDirectory();
                            Console.WriteLine($"File '{fileName}' created!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating file: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Usage: touch <filename>");
                    }
                }, "File Operations", new[] { "create" }, "touch <filename>", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("delete", "Delete the selected file or directory", 
                args => {
                    DeleteSelectedItem();
                }, "File Operations", new[] { "del", "rm" }, "delete", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("refresh", "Refresh the current directory", 
                args => {
                    RefreshDirectory();
                    Console.WriteLine("Directory refreshed!");
                }, "Navigation", new[] { "r", "f5" }, "refresh", CommandContext.FileExplorer);

            _commandRegistry.RegisterCommand("home", "Go to home directory", 
                args => {
                    _currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    RefreshDirectory();
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    Console.WriteLine("Navigated to home directory!");
                }, "Navigation", new[] { "~" }, "home", CommandContext.FileExplorer);
        }

        private void RefreshDirectory()
        {
            try
            {
                _items.Clear();

                // Add directories first
                var dirs = Directory.GetDirectories(_currentDirectory);
                foreach (var dir in dirs.OrderBy(d => Path.GetFileName(d)))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        _items.Add(new FileItem
                        {
                            Name = dirInfo.Name,
                            IsDirectory = true,
                            Size = 0,
                            LastModified = dirInfo.LastWriteTime,
                            FullPath = dir
                        });
                    }
                    catch (Exception ex)
                    {
                        // Skip directories we can't access
                        Console.WriteLine($"Cannot access directory {dir}: {ex.Message}");
                    }
                }

                // Add files
                var files = Directory.GetFiles(_currentDirectory);
                foreach (var file in files.OrderBy(f => Path.GetFileName(f)))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        _items.Add(new FileItem
                        {
                            Name = fileInfo.Name,
                            IsDirectory = false,
                            Size = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            FullPath = file
                        });
                    }
                    catch (Exception ex)
                    {
                        // Skip files we can't access
                        Console.WriteLine($"Cannot access file {file}: {ex.Message}");
                    }
                }

                _selectedIndex = Math.Min(_selectedIndex, _items.Count - 1);
                if (_selectedIndex < 0) _selectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading directory: {ex.Message}");
            }
        }

        private void OpenSelectedItem()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                var selectedItem = _items[_selectedIndex];
                
                if (selectedItem.IsDirectory)
                {
                    // Open directory
                    NavigateToDirectory(selectedItem.Name);
                }
                else
                {
                    // Open file
                    OnFileSelected?.Invoke(selectedItem.FullPath);
                }
            }
        }

        private void NavigateToDirectory(string dirName)
        {
            try
            {
                string newPath = Path.Combine(_currentDirectory, dirName);
                if (Directory.Exists(newPath))
                {
                    _currentDirectory = Path.GetFullPath(newPath);
                    RefreshDirectory();
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    Console.WriteLine($"Navigated to: {_currentDirectory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to directory: {ex.Message}");
            }
        }

        private void NavigateToParentDirectory()
        {
            try
            {
                DirectoryInfo parent = Directory.GetParent(_currentDirectory);
                if (parent != null)
                {
                    _currentDirectory = parent.FullName;
                    RefreshDirectory();
                    _selectedIndex = 0;
                    _scrollOffset = 0;
                    Console.WriteLine($"Navigated to parent: {_currentDirectory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to parent directory: {ex.Message}");
            }
        }

        private void DeleteSelectedItem()
        {
            try
            {
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                {
                    var selectedItem = _items[_selectedIndex];
                    
                    if (selectedItem.IsDirectory)
                    {
                        // Delete directory
                        Directory.Delete(selectedItem.FullPath, true);
                        Console.WriteLine($"Directory '{selectedItem.Name}' deleted!");
                    }
                    else
                    {
                        // Delete file
                        File.Delete(selectedItem.FullPath);
                        Console.WriteLine($"File '{selectedItem.Name}' deleted!");
                    }
                    
                    RefreshDirectory();
                    _selectedIndex = Math.Min(_selectedIndex, _items.Count - 1);
                    if (_selectedIndex < 0) _selectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting item: {ex.Message}");
            }
        }

        private void EnsureSelectedItemVisible()
        {
            int visibleItems = (Raylib.GetScreenHeight() - 100) / LineHeight;
            
            if (_selectedIndex < _scrollOffset)
            {
                _scrollOffset = _selectedIndex;
            }
            else if (_selectedIndex >= _scrollOffset + visibleItems)
            {
                _scrollOffset = _selectedIndex - visibleItems + 1;
            }
        }
    }
}