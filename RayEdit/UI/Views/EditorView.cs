using System.Numerics;
using RayEdit.Core.Commands;
using RayEdit.Core.IO;
using RayEdit.Core.Text;
using RayEdit.UI.Controls;
using Raylib_cs;

namespace RayEdit.UI.Views
{
    /// <summary>
    /// The main view for text editing. Handles user input and rendering of the text buffer.
    /// </summary>
    public class EditorView : IView
    {
        // Events
        public event Action OnBackToMenu;
        // Data
        private TextBuffer _textBuffer;
        private string _currentFilePath = "document.txt";
        
        // Command system
        private CommandRegistry _commandRegistry;
        private CommandBar _commandBar;
        
        // Auto completion
        private AutoCompletion _autoCompletion;
        
        // UI Components
        private Gutter _gutter;

        // Rendering & Layout
        private Font _font;
        private const int FontSize = 20;
        private const int LineSpacing = 5;
        private const int CharWidth = 10; // Approx. width for monospaced font
        private const int MarginX = 20;
        private const int MarginY = 20;

        // State
        private int _scrollOffsetLine = 0;
        private float _deleteKeyTimer = 0f;
        private float _backspaceKeyTimer = 0f;
        private float _leftKeyTimer = 0f;
        private float _rightKeyTimer = 0f;
        private float _upKeyTimer = 0f;
        private float _downKeyTimer = 0f;
        private const float KeyRepeatDelay = 0.5f;
        private const float KeyRepeatRate = 0.05f;
        
        // Undo/Redo timing
        private double _lastActionTime = 0.0;
        private const double GroupFinalizationDelay = 1.0; // 1 second

        public void Load()
        {
            // Load the font
            _font = Raylib.LoadFont("Assets/Fonts/FiraCode-Regular.ttf");
            
            // Load initial content from a file and create the text buffer
            string initialContent = FileManager.LoadText(_currentFilePath);
            _textBuffer = new TextBuffer(initialContent);
            
            // Initialize command system
            InitializeCommandSystem();
            
            // Initialize auto completion
            _autoCompletion = new AutoCompletion(_font);
            
            // Initialize gutter
            _gutter = new Gutter(80); // 80px width for line numbers
            _gutter.SetTheme(new Color(45, 45, 45, 255), new Color(150, 150, 150, 255), Color.White, _font, FontSize);
        }

        public void LoadFile(string filePath)
        {
            _currentFilePath = filePath;
            if (_textBuffer != null)
            {
                string content = FileManager.LoadText(_currentFilePath);
                _textBuffer.SetContent(content);
            }
        }

        public void Unload()
        {
            // Unload resources
            Raylib.UnloadFont(_font);
        }

        public void Update()
        {
            // Handle command bar input first (it has priority when visible)
            if (_commandBar.IsVisible)
            {
                _commandBar.HandleInput();
            }
            else if (_autoCompletion.IsVisible)
            {
                HandleAutoCompletionInput();
            }
            else
            {
                HandleKeyboardInput();
                HandleMouseInput();
            }
            
            HandleUndoRedoGrouping();
        }

        private void HandleKeyboardInput()
        {
            bool shiftPressed = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
            bool ctrlPressed = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);

            // Character input
            int key = Raylib.GetCharPressed();
            
            while (key > 0)
            {
                if ((key >= 32) && (key <= 125))
                {
                    string toInsert = ((char)key).ToString();
                    // Auto-closing pairs
                    switch (key)
                    {
                        case '(': toInsert = "()"; break;
                        case '{': toInsert = "{}"; break;
                        case '[': toInsert = "[]"; break;
                        case '"': toInsert = "\"\""; break;
                        case '\'': toInsert = "''"; break;
                    }
                    
                    _textBuffer.InsertText(toInsert);
                    // If it was a pair, move cursor back inside
                    if (toInsert.Length > 1) _textBuffer.MoveCursor(-1);
                    _lastActionTime = Raylib.GetTime();
                    
                    // Trigger auto completion for word characters
                    if (char.IsLetter((char)key) || (char)key == '_')
                    {
                        TriggerAutoCompletion();
                    }
                    else
                    {
                        _autoCompletion.Hide();
                    }
                }
                key = Raylib.GetCharPressed();
            }

            // Special keys
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                _textBuffer.InsertText("    ");
                _lastActionTime = Raylib.GetTime();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                HandleEnterKey();
                _lastActionTime = Raylib.GetTime();
            }

            // Key Repeats for Backspace and Delete
            HandleKeyRepeat(KeyboardKey.Backspace, ref _backspaceKeyTimer, () => {
                _textBuffer.PerformBackspace(ctrlPressed);
                _lastActionTime = Raylib.GetTime();
                
                // Update auto completion after backspace
                if (_autoCompletion.IsVisible)
                {
                    string currentWord = GetCurrentWord();
                    if (string.IsNullOrEmpty(currentWord))
                    {
                        _autoCompletion.Hide();
                    }
                    else
                    {
                        bool isCSharpFile = _currentFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                        _autoCompletion.UpdatePrefix(currentWord, _textBuffer.Content, isCSharpFile);
                    }
                }
            });
            HandleKeyRepeat(KeyboardKey.Delete, ref _deleteKeyTimer, () => {
                _textBuffer.PerformDelete(ctrlPressed);
                _lastActionTime = Raylib.GetTime();
                
                // Hide auto completion after delete
                _autoCompletion.Hide();
            });

            // Cursor Movement
            bool cursorMoved = HandleCursorMovement(shiftPressed, ctrlPressed);
            
            // Hide auto completion on cursor movement (except when selecting)
            if (cursorMoved && !shiftPressed && _autoCompletion.IsVisible)
            {
                _autoCompletion.Hide();
            }

            // Shortcuts (Save, Load, Copy, Paste, etc.)
            HandleShortcuts(ctrlPressed);

            // Command palette (Ctrl+P)
            if (ctrlPressed && Raylib.IsKeyPressed(KeyboardKey.P))
            {
                _commandBar.Show();
                return; // Don't process other keys when opening command bar
            }
            
            // Manual auto completion trigger (Ctrl+Space)
            if (ctrlPressed && Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                TriggerAutoCompletion();
                return;
            }
            
            // Back to menu
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                OnBackToMenu?.Invoke();
            }
            
            // Ensure cursor is not out of bounds (redundant with TextBuffer clamp, but safe)
            _textBuffer.SetCursorPosition(_textBuffer.CursorIndex);
            
            // Adjust scrolling to keep cursor in view
            EnsureCursorIsVisible();
        }

        private void HandleKeyRepeat(KeyboardKey key, ref float timer, Action action)
        {
            float deltaTime = Raylib.GetFrameTime();
            if (Raylib.IsKeyDown(key))
            {
                if (Raylib.IsKeyPressed(key))
                {
                    action();
                    timer = 0f;
                }
                else
                {
                    timer += deltaTime;
                    if (timer >= KeyRepeatDelay)
                    {
                        action();
                        // Reset timer for repeat rate, not full delay
                        timer = KeyRepeatDelay - KeyRepeatRate; 
                    }
                }
            }
            else
            {
                timer = 0f;
            }
        }

        private bool HandleKeyRepeatWithReturn(KeyboardKey key, ref float timer)
        {
            float deltaTime = Raylib.GetFrameTime();
            if (Raylib.IsKeyDown(key))
            {
                if (Raylib.IsKeyPressed(key))
                {
                    timer = 0f;
                    return true;
                }
                else
                {
                    timer += deltaTime;
                    if (timer >= KeyRepeatDelay)
                    {
                        // Reset timer for repeat rate, not full delay
                        timer = KeyRepeatDelay - KeyRepeatRate; 
                        return true;
                    }
                }
            }
            else
            {
                timer = 0f;
            }
            return false;
        }

        private bool HandleCursorMovement(bool shiftPressed, bool ctrlPressed)
        {
            bool keyWasPressed = false;

            // Start selection BEFORE moving cursor if shift is pressed and we're not already selecting
            if (shiftPressed && !_textBuffer.IsSelecting)
            {
                _textBuffer.StartSelection();
            }

            // Handle Left arrow key with repeat
            if (HandleKeyRepeatWithReturn(KeyboardKey.Left, ref _leftKeyTimer))
            {
                // Clear selection only if we're moving without shift and have a selection
                if (!shiftPressed && _textBuffer.HasSelection())
                {
                    _textBuffer.ClearSelection();
                }
                
                if (ctrlPressed) _textBuffer.MoveToWordStart();
                else _textBuffer.MoveCursor(-1);
                keyWasPressed = true;
            }

            // Handle Right arrow key with repeat
            if (HandleKeyRepeatWithReturn(KeyboardKey.Right, ref _rightKeyTimer))
            {
                // Clear selection only if we're moving without shift and have a selection
                if (!shiftPressed && _textBuffer.HasSelection())
                {
                    _textBuffer.ClearSelection();
                }
                
                if (ctrlPressed) _textBuffer.MoveToWordEnd();
                else _textBuffer.MoveCursor(1);
                keyWasPressed = true;
            }

            // Handle Up arrow key with repeat
            if (HandleKeyRepeatWithReturn(KeyboardKey.Up, ref _upKeyTimer))
            {
                // Clear selection only if we're moving without shift and have a selection
                if (!shiftPressed && _textBuffer.HasSelection())
                {
                    _textBuffer.ClearSelection();
                }
                
                MoveCursorLine(-1);
                keyWasPressed = true;
            }

            // Handle Down arrow key with repeat
            if (HandleKeyRepeatWithReturn(KeyboardKey.Down, ref _downKeyTimer))
            {
                // Clear selection only if we're moving without shift and have a selection
                if (!shiftPressed && _textBuffer.HasSelection())
                {
                    _textBuffer.ClearSelection();
                }
                
                MoveCursorLine(1);
                keyWasPressed = true;
            }

            // Update selection after cursor movement if shift is pressed
            if (keyWasPressed && shiftPressed)
            {
                _textBuffer.UpdateSelection();
            }
            
            return keyWasPressed;
        }
        
        private void HandleShortcuts(bool ctrlPressed)
        {
            if (!ctrlPressed) return;

            if (Raylib.IsKeyPressed(KeyboardKey.S))
            {
                FileManager.SaveText(_currentFilePath, _textBuffer.Content);
                Console.WriteLine("File saved!"); // Feedback
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.O))
            {
                string loadedContent = FileManager.LoadText(_currentFilePath);
                _textBuffer.SetContent(loadedContent); // Replace buffer and clear undo history
                Console.WriteLine("File loaded!");
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.C))
            {
                if (_textBuffer.HasSelection()) Raylib.SetClipboardText(_textBuffer.GetSelectedText());
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.V))
            {
                _textBuffer.InsertText(Raylib.GetClipboardText_());
                _lastActionTime = Raylib.GetTime();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.X))
            {
                if (_textBuffer.HasSelection())
                {
                    Raylib.SetClipboardText(_textBuffer.GetSelectedText());
                    _textBuffer.PerformDelete(false); // Perform a standard deletion of the selection
                    _lastActionTime = Raylib.GetTime();
                }
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.A))
            {
                _textBuffer.SelectAll();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Z))
            {
                if (_textBuffer.CanUndo)
                {
                    _textBuffer.Undo();
                    Console.WriteLine("Undo performed!");
                }
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Y))
            {
                if (_textBuffer.CanRedo)
                {
                    _textBuffer.Redo();
                    Console.WriteLine("Redo performed!");
                }
            }
        }

        private void HandleUndoRedoGrouping()
        {
            // Finalize command groups after a period of inactivity
            double currentTime = Raylib.GetTime();
            if (_lastActionTime > 0 && (currentTime - _lastActionTime) > GroupFinalizationDelay)
            {
                _textBuffer.FinalizeCurrentGroup();
                _lastActionTime = 0.0;
            }
        }

        private void HandleEnterKey()
        {
            // Check if we're in a C# file
            bool isCSharpFile = _currentFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            
            if (isCSharpFile)
            {
                // Get the current line's indentation
                string currentLineIndentation = GetCurrentLineIndentation();
                _textBuffer.InsertText("\n" + currentLineIndentation);
            }
            else
            {
                // Default behavior for non-C# files
                _textBuffer.InsertText("\n");
            }
        }

        private string GetCurrentLineIndentation()
        {
            // Get the current cursor position
            int cursorIndex = _textBuffer.CursorIndex;
            string content = _textBuffer.Content;
            
            // Find the start of the current line
            int lineStart = cursorIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n')
            {
                lineStart--;
            }
            
            // Extract indentation (spaces and tabs at the beginning of the line)
            string indentation = "";
            for (int i = lineStart; i < content.Length && i < cursorIndex; i++)
            {
                char c = content[i];
                if (c == ' ' || c == '\t')
                {
                    indentation += c;
                }
                else
                {
                    break; // Stop at the first non-whitespace character
                }
            }
            
            return indentation;
        }

        private void HandleMouseInput()
        {
            // Scroll with mouse wheel
            float mouseWheel = Raylib.GetMouseWheelMove();
            if (mouseWheel != 0)
            {
                _scrollOffsetLine -= (int)mouseWheel;
                if (_scrollOffsetLine < 0) _scrollOffsetLine = 0;
            }
        }

        public void Draw()
        {
            // Calculate editor area dimensions
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            int gutterWidth = _gutter.Width;
            int lineHeight = FontSize + LineSpacing;
            int scrollY = _scrollOffsetLine * lineHeight;

            // Draw gutter
            _gutter.Draw(0, MarginY, screenHeight - MarginY * 2, _textBuffer, scrollY, lineHeight);

            // Draw selection highlighting first
            if (_textBuffer.HasSelection())
            {
                DrawSelection();
            }

            // Draw text content with syntax highlighting
            DrawTextWithSyntaxHighlighting();

            // Draw cursor
            Vector2 cursorPosition = GetCharacterScreenPosition(_textBuffer.CursorIndex);
            // Make cursor slightly thicker and ensure it's precisely positioned
            Raylib.DrawRectangle((int)Math.Round(cursorPosition.X), (int)Math.Round(cursorPosition.Y), 2, FontSize, Color.White);
            
            // Draw command bar on top of everything
            _commandBar?.Draw();
            
            // Draw auto completion on top of everything else
            _autoCompletion?.Draw();
        }

        private void InitializeCommandSystem()
        {
            _commandRegistry = new CommandRegistry();
            _commandBar = new CommandBar(_commandRegistry, _font);
            
            // Set context to Editor
            _commandRegistry.SetContext(CommandContext.Editor);
            
            // Register built-in commands
            RegisterBuiltInCommands();
            
            // Handle command execution
            _commandBar.OnCommandExecuted += (commandLine) =>
            {
                Console.WriteLine($"Executed command: {commandLine}");
            };
        }
        
        private void RegisterBuiltInCommands()
        {
            // Global commands (available everywhere)
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

            _commandRegistry.RegisterCommand("exit", "Exit the editor", 
                args => {
                    OnBackToMenu?.Invoke();
                }, "Application", new[] { "quit", "q" }, "exit", CommandContext.Global);

            // Editor-specific commands
            _commandRegistry.RegisterCommand("save", "Save the current file", 
                args => {
                    FileManager.SaveText(_currentFilePath, _textBuffer.Content);
                    Console.WriteLine("File saved!");
                }, "File", new[] { "s" }, "save", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("save-as", "Save the file with a new name", 
                args => {
                    string newPath = args.Length > 0 ? args[0] : "new_document.txt";
                    FileManager.SaveText(newPath, _textBuffer.Content);
                    _currentFilePath = newPath;
                    Console.WriteLine($"File saved as '{newPath}'!");
                }, "File", new[] { "sa" }, "save-as <filename>", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("open", "Open a file", 
                args => {
                    string filePath = args.Length > 0 ? args[0] : _currentFilePath;
                    string content = FileManager.LoadText(filePath);
                    _textBuffer.SetContent(content);
                    _currentFilePath = filePath;
                    Console.WriteLine($"File '{filePath}' opened!");
                }, "File", new[] { "o" }, "open [filename]", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("new", "Create a new file", 
                args => {
                    _textBuffer.SetContent("");
                    _currentFilePath = "document.txt";
                    Console.WriteLine("New file created!");
                }, "File", new[] { "n" }, "new", CommandContext.Editor);
            
            // Edit operations (Editor-specific)
            _commandRegistry.RegisterCommand("undo", "Undo the last action", 
                args => {
                    if (_textBuffer.CanUndo)
                    {
                        _textBuffer.Undo();
                        Console.WriteLine("Undo performed!");
                    }
                    else
                    {
                        Console.WriteLine("Nothing to undo!");
                    }
                }, "Edit", new[] { "u" }, "undo", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("redo", "Redo the last undone action", 
                args => {
                    if (_textBuffer.CanRedo)
                    {
                        _textBuffer.Redo();
                        Console.WriteLine("Redo performed!");
                    }
                    else
                    {
                        Console.WriteLine("Nothing to redo!");
                    }
                }, "Edit", new[] { "r" }, "redo", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("select-all", "Select all text", 
                args => {
                    _textBuffer.SelectAll();
                    Console.WriteLine("All text selected!");
                }, "Edit", new[] { "all" }, "select-all", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("copy", "Copy selected text to clipboard", 
                args => {
                    if (_textBuffer.HasSelection())
                    {
                        Raylib.SetClipboardText(_textBuffer.GetSelectedText());
                        Console.WriteLine("Text copied to clipboard!");
                    }
                    else
                    {
                        Console.WriteLine("No text selected!");
                    }
                }, "Edit", new[] { "c" }, "copy", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("paste", "Paste text from clipboard", 
                args => {
                    string clipboardText = Raylib.GetClipboardText_();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        _textBuffer.InsertText(clipboardText);
                        Console.WriteLine("Text pasted from clipboard!");
                    }
                    else
                    {
                        Console.WriteLine("Clipboard is empty!");
                    }
                }, "Edit", new[] { "p", "v" }, "paste", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("cut", "Cut selected text to clipboard", 
                args => {
                    if (_textBuffer.HasSelection())
                    {
                        Raylib.SetClipboardText(_textBuffer.GetSelectedText());
                        _textBuffer.PerformDelete(false);
                        Console.WriteLine("Text cut to clipboard!");
                    }
                    else
                    {
                        Console.WriteLine("No text selected!");
                    }
                }, "Edit", new[] { "x" }, "cut", CommandContext.Editor);

            _commandRegistry.RegisterCommand("find", "Find text in the document", 
                args => {
                    if (args.Length > 0)
                    {
                        string searchText = string.Join(" ", args);
                        // TODO: Implement find functionality
                        Console.WriteLine($"Searching for: {searchText}");
                    }
                    else
                    {
                        Console.WriteLine("Usage: find <text>");
                    }
                }, "Edit", new[] { "f", "search" }, "find <text>", CommandContext.Editor);

            _commandRegistry.RegisterCommand("replace", "Replace text in the document", 
                args => {
                    if (args.Length >= 2)
                    {
                        string findText = args[0];
                        string replaceText = args[1];
                        // TODO: Implement replace functionality
                        Console.WriteLine($"Replacing '{findText}' with '{replaceText}'");
                    }
                    else
                    {
                        Console.WriteLine("Usage: replace <find-text> <replace-text>");
                    }
                }, "Edit", new[] { "rep" }, "replace <find-text> <replace-text>", CommandContext.Editor);
            
            // Navigation (Editor-specific)
            _commandRegistry.RegisterCommand("goto-line", "Go to a specific line number", 
                args => {
                    if (args.Length > 0 && int.TryParse(args[0], out int lineNumber))
                    {
                        GoToLine(lineNumber);
                        Console.WriteLine($"Jumped to line {lineNumber}!");
                    }
                    else
                    {
                        Console.WriteLine("Usage: goto-line <line-number>");
                    }
                }, "Navigation", new[] { "gl", "goto", ":" }, "goto-line <line-number>", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("goto-start", "Go to the beginning of the document", 
                args => {
                    _textBuffer.SetCursorPosition(0);
                    EnsureCursorIsVisible();
                    Console.WriteLine("Jumped to start of document!");
                }, "Navigation", new[] { "start", "home", "top" }, "goto-start", CommandContext.Editor);
                
            _commandRegistry.RegisterCommand("goto-end", "Go to the end of the document", 
                args => {
                    _textBuffer.SetCursorPosition(_textBuffer.Content.Length);
                    EnsureCursorIsVisible();
                    Console.WriteLine("Jumped to end of document!");
                }, "Navigation", new[] { "end", "bottom" }, "goto-end", CommandContext.Editor);

            _commandRegistry.RegisterCommand("word-count", "Show word and character count", 
                args => {
                    string content = _textBuffer.Content;
                    int charCount = content.Length;
                    int wordCount = content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    int lineCount = content.Split('\n').Length;
                    Console.WriteLine($"Characters: {charCount}, Words: {wordCount}, Lines: {lineCount}");
                }, "Info", new[] { "wc", "count", "stats" }, "word-count", CommandContext.Editor);

            _commandRegistry.RegisterCommand("clear", "Clear all text in the document", 
                args => {
                    _textBuffer.SetContent("");
                    Console.WriteLine("Document cleared!");
                }, "Edit", new[] { "cls" }, "clear", CommandContext.Editor);

            _commandRegistry.RegisterCommand("duplicate-line", "Duplicate the current line", 
                args => {
                    // TODO: Implement line duplication
                    Console.WriteLine("Line duplication not yet implemented!");
                }, "Edit", new[] { "dup" }, "duplicate-line", CommandContext.Editor);

            _commandRegistry.RegisterCommand("insert-date", "Insert current date and time", 
                args => {
                    string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    _textBuffer.InsertText(dateTime);
                    Console.WriteLine("Date and time inserted!");
                }, "Insert", new[] { "date", "time" }, "insert-date", CommandContext.Editor);
        }
        
        private void GoToLine(int lineNumber)
        {
            string[] lines = _textBuffer.Content.Split('\n');
            if (lineNumber < 1 || lineNumber > lines.Length) return;
            
            int position = 0;
            for (int i = 0; i < lineNumber - 1; i++)
            {
                position += lines[i].Length + 1; // +1 for newline
            }
            
            _textBuffer.SetCursorPosition(position);
            EnsureCursorIsVisible();
        }

        #region Drawing and Position Helpers

        private void DrawTextWithSyntaxHighlighting()
        {
            string[] lines = _textBuffer.Content.Split('\n');
            int lineHeight = FontSize + LineSpacing;
            bool shouldHighlight = SyntaxHighlighter.ShouldHighlight(_currentFilePath);

            for (int lineIndex = _scrollOffsetLine; lineIndex < lines.Length; lineIndex++)
            {
                int yPos = MarginY + (lineIndex - _scrollOffsetLine) * lineHeight;
                if (yPos > Raylib.GetScreenHeight() - MarginY) break;

                string line = lines[lineIndex];

                // Calculate the starting character index for this line
                int lineStartIndex = 0;
                for (int i = 0; i < lineIndex; i++)
                {
                    lineStartIndex += lines[i].Length + 1; // +1 for the newline character
                }

                float xPos = _gutter.Width + MarginX;

                if (shouldHighlight && !string.IsNullOrEmpty(line))
                {
                    var tokens = SyntaxHighlighter.TokenizeLine(line, lineStartIndex);
                    foreach (var token in tokens)
                    {
                        // Whitespaces jetzt als Token, also nicht mehr überspringen!
                        Raylib.DrawTextEx(_font, token.Text, new Vector2(xPos, yPos), (float)FontSize, 1.0f, token.Color);
                        Vector2 tokenSize = Raylib.MeasureTextEx(_font, token.Text, (float)FontSize, 1.0f);
                        xPos += tokenSize.X;
                    }
                }
                else
                {
                    // Draw without syntax highlighting (default white text)
                    // Replace tabs with spaces for consistent rendering
                    string displayLine = line.Replace("\t", "    ");
                    Raylib.DrawTextEx(_font, displayLine, new Vector2(_gutter.Width + MarginX, yPos), (float)FontSize, 1.0f, Color.White);
                }
            }
        }

        private void DrawSelection()
        {
            var (start, end) = _textBuffer.GetSelectionRange();
            if (start == -1) return;

            Color selectionColor = new Color(100, 149, 237, 100); // Light blue with transparency

            for (int i = start; i < end; i++)
            {
                if (_textBuffer.Content[i] == '\n') continue;

                Vector2 charPos = GetCharacterScreenPosition(i);
                if (charPos.Y >= MarginY && charPos.Y < Raylib.GetScreenHeight() - MarginY)
                {
                    Raylib.DrawRectangle((int)charPos.X, (int)charPos.Y, CharWidth, FontSize, selectionColor);
                }
            }
        }

        private Vector2 GetCharacterScreenPosition(int charIndex)
        {
            float x = _gutter.Width + MarginX;
            float y = MarginY;
            int lineHeight = FontSize + LineSpacing;
            
            string content = _textBuffer.Content;
            if (charIndex > content.Length) charIndex = content.Length;

            // Find which line we're on and the position within that line
            int currentLine = 0;
            int lineStartIndex = 0;
            
            for (int i = 0; i < charIndex; i++)
            {
                if (content[i] == '\n')
                {
                    currentLine++;
                    lineStartIndex = i + 1;
                }
            }
            
            // Calculate Y position
            y += currentLine * lineHeight;
            y -= _scrollOffsetLine * lineHeight;
            
            // Calculate X position by measuring the actual text width from line start to cursor
            if (charIndex > lineStartIndex)
            {
                string textBeforeCursor = content.Substring(lineStartIndex, charIndex - lineStartIndex);
                // Replace tabs with spaces for measurement
                textBeforeCursor = textBeforeCursor.Replace("\t", "    ");
                Vector2 textSize = Raylib.MeasureTextEx(_font, textBeforeCursor, (float)FontSize, 1.0f);
                x += textSize.X;
            }

            return new Vector2(x, y);
        }

        private int GetLineNumber(int charIndex)
        {
            int lineCount = 0;
            for (int i = 0; i < charIndex && i < _textBuffer.Content.Length; i++)
            {
                if (_textBuffer.Content[i] == '\n')
                {
                    lineCount++;
                }
            }
            return lineCount;
        }

        private void MoveCursorLine(int direction)
        {
            int currentLine = GetLineNumber(_textBuffer.CursorIndex);
            int currentLineStart = _textBuffer.Content.LastIndexOf('\n', Math.Max(0, _textBuffer.CursorIndex - 1)) + 1;
            int col = _textBuffer.CursorIndex - currentLineStart;

            int targetLine = currentLine + direction;
            string[] lines = _textBuffer.Content.Split('\n');

            if (targetLine >= 0 && targetLine < lines.Length)
            {
                int targetLineStart = 0;
                for(int i = 0; i < targetLine; i++)
                {
                    targetLineStart += lines[i].Length + 1; // +1 for the '\n'
                }
                int targetCol = Math.Min(col, lines[targetLine].Length);
                _textBuffer.SetCursorPosition(targetLineStart + targetCol);
            }
        }
        
        private void EnsureCursorIsVisible()
        {
            int cursorLine = GetLineNumber(_textBuffer.CursorIndex);
            int visibleLines = (Raylib.GetScreenHeight() - 2 * MarginY) / (FontSize + LineSpacing);

            if (cursorLine < _scrollOffsetLine)
            {
                _scrollOffsetLine = cursorLine;
            }
            else if (cursorLine >= _scrollOffsetLine + visibleLines)
            {
                _scrollOffsetLine = cursorLine - visibleLines + 1;
            }
        }

        #endregion

        #region Auto Completion

        private void HandleAutoCompletionInput()
        {
            // Handle navigation in auto completion
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                _autoCompletion.MoveSelection(-1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                _autoCompletion.MoveSelection(1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                // Accept the selected suggestion
                AcceptAutoCompletion();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                _autoCompletion.Hide();
            }
            else
            {
                // Handle character input while auto completion is visible
                int key = Raylib.GetCharPressed();
                while (key > 0)
                {
                    if ((key >= 32) && (key <= 125))
                    {
                        char c = (char)key;
                        _textBuffer.InsertText(c.ToString());
                        _lastActionTime = Raylib.GetTime();
                        
                        // Update auto completion with new character
                        if (char.IsLetter(c) || c == '_')
                        {
                            string currentWord = GetCurrentWord();
                            if (!string.IsNullOrEmpty(currentWord))
                            {
                                bool isCSharpFile = _currentFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                                _autoCompletion.UpdatePrefix(currentWord, _textBuffer.Content, isCSharpFile);
                            }
                            else
                            {
                                _autoCompletion.Hide();
                            }
                        }
                        else
                        {
                            _autoCompletion.Hide();
                        }
                    }
                    key = Raylib.GetCharPressed();
                }
                
                // Handle backspace
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
                {
                    _textBuffer.PerformBackspace(false);
                    _lastActionTime = Raylib.GetTime();
                    
                    string currentWord = GetCurrentWord();
                    if (string.IsNullOrEmpty(currentWord))
                    {
                        _autoCompletion.Hide();
                    }
                    else
                    {
                        bool isCSharpFile = _currentFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                        _autoCompletion.UpdatePrefix(currentWord, _textBuffer.Content, isCSharpFile);
                    }
                }
            }
        }

        private void TriggerAutoCompletion()
        {
            string currentWord = GetCurrentWord();
            bool isCSharpFile = _currentFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            
            // In C# files, trigger with 1+ characters, in other files with 2+ characters
            int minLength = isCSharpFile ? 1 : 2;
            
            if (!string.IsNullOrEmpty(currentWord) && currentWord.Length >= minLength)
            {
                Vector2 cursorPosition = GetCharacterScreenPosition(_textBuffer.CursorIndex);
                // Position the auto completion below the cursor
                Vector2 completionPosition = new Vector2(cursorPosition.X, cursorPosition.Y + FontSize + 5);
                
                _autoCompletion.Show(currentWord, completionPosition, _textBuffer.Content, isCSharpFile);
            }
        }

        private void AcceptAutoCompletion()
        {
            if (!_autoCompletion.IsVisible) return;
            
            string selectedSuggestion = _autoCompletion.SelectedSuggestion;
            if (string.IsNullOrEmpty(selectedSuggestion)) return;
            
            string currentWord = GetCurrentWord();
            if (!string.IsNullOrEmpty(currentWord))
            {
                // Replace the current word with the selected suggestion
                int wordStart = GetCurrentWordStart();
                int wordLength = currentWord.Length;
                
                // Remove the current word
                _textBuffer.SetCursorPosition(wordStart);
                for (int i = 0; i < wordLength; i++)
                {
                    _textBuffer.PerformDelete(false);
                }
                
                // Insert the selected suggestion
                _textBuffer.InsertText(selectedSuggestion);
                _lastActionTime = Raylib.GetTime();
            }
            
            _autoCompletion.Hide();
        }

        private string GetCurrentWord()
        {
            int cursorIndex = _textBuffer.CursorIndex;
            string content = _textBuffer.Content;
            
            if (cursorIndex == 0 || cursorIndex > content.Length) return "";
            
            // Find the start of the current word
            int wordStart = cursorIndex;
            while (wordStart > 0)
            {
                char c = content[wordStart - 1];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
                wordStart--;
            }
            
            // Find the end of the current word
            int wordEnd = cursorIndex;
            while (wordEnd < content.Length)
            {
                char c = content[wordEnd];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
                wordEnd++;
            }
            
            if (wordStart >= wordEnd) return "";
            
            return content.Substring(wordStart, wordEnd - wordStart);
        }

        private int GetCurrentWordStart()
        {
            int cursorIndex = _textBuffer.CursorIndex;
            string content = _textBuffer.Content;
            
            if (cursorIndex == 0 || cursorIndex > content.Length) return cursorIndex;
            
            // Find the start of the current word
            int wordStart = cursorIndex;
            while (wordStart > 0)
            {
                char c = content[wordStart - 1];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
                wordStart--;
            }
            
            return wordStart;
        }

        #endregion
    }
}
