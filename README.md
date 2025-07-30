# RayEdit - Proof-of-Concept Code Editor

**⚠️ This is a completed proof-of-concept project with no further development planned.**

A demonstration of how to build a functional code editor using Raylib-cs and C#. This project serves as an educational example and technical showcase, illustrating the implementation of core text editing features specifically for C# development.

## 🚀 Features

### ✨ Core Functionality
- **Complete text editing** with cursor navigation and text selection
- **Syntax highlighting** for C# code with keyword recognition
- **Automatic indentation** when pressing Enter in C# files
- **Auto-completion** with C# keywords and previously written words
- **Undo/Redo system** with intelligent action grouping
- **File management** (Open, Save, New file creation)

### 🎯 C#-Specific Features
- **Intelligent Auto-Completion**: Suggests C# keywords and previously used terms
- **Automatic bracket completion**: `()`, `{}`, `[]`, `""`, `''`
- **Smart Indentation**: Maintains indentation on new lines
- **Syntax Highlighting**: Recognizes C# keywords, strings, comments and more

### ⌨️ Keyboard Shortcuts
- **Ctrl + S**: Save file
- **Ctrl + O**: Open file
- **Ctrl + C/V/X**: Copy/Paste/Cut
- **Ctrl + A**: Select all
- **Ctrl + Z/Y**: Undo/Redo
- **Ctrl + P**: Open Command Palette
- **Ctrl + Space**: Manually trigger auto-completion
- **Tab**: Insert 4 spaces
- **Escape**: Return to main menu

### 🎮 Command Palette
Advanced command palette with over 20 integrated commands:
- **File Operations**: `save`, `open`, `new`, `save-as`
- **Editing**: `undo`, `redo`, `copy`, `paste`, `cut`, `select-all`
- **Navigation**: `goto-line`, `goto-start`, `goto-end`
- **Search**: `find`, `replace` (planned)
- **Utilities**: `word-count`, `clear`, `insert-date`

## 🛠️ Technical Details

### Architecture
- **Frontend**: Raylib-cs for rendering and input handling
- **Backend**: C# .NET for logic and data processing
- **Modular Design**: Separate components for different functionalities

### Main Components
- **`EditorView`**: Main view with input handling and rendering
- **`TextBuffer`**: Manages text content, cursor and selection
- **`AutoCompletion`**: Intelligent completion for C# and text documents
- **`CommandRegistry`**: Extensible command system
- **`SyntaxHighlighter`**: Syntax highlighting for various file types
- **`UndoRedoManager`**: Undo/Redo functionality with action grouping

### Performance Optimizations
- **Efficient text rendering** with scrolling support
- **Intelligent cursor positioning** with precise text measurement
- **Optimized auto-completion** with limited suggestions
- **Lazy loading** for large files

## 🎯 Purpose & Target Audience

This **completed proof-of-concept** is intended for:
- **Developers** studying text editor implementation techniques
- **Students** learning about text processing and GUI development
- **Raylib enthusiasts** exploring desktop application development
- **Educators** looking for practical examples of C# and Raylib-cs integration

**Note**: This project is not intended for production use and will not receive updates or new features.

## 🚦 Installation & Usage

### Prerequisites
- .NET 6.0 or higher
- Windows, macOS or Linux
- Raylib-cs NuGet package (automatically installed)

### Quick Start
```bash
# Clone repository
git clone <repository-url>
cd RayEdit

# Build project
dotnet build

# Start editor
dotnet run
```

### Development
```bash
# Start in development mode
dotnet run --configuration Debug

# Run tests (if available)
dotnet test
```

## 📁 Project Structure

```
RayEdit/
├── RayEdit/
│   ├── EditorView.cs          # Main editor view
│   ├── TextBuffer.cs          # Text buffer management
│   ├── AutoCompletion.cs      # Auto-completion system
│   ├── CommandRegistry.cs     # Command system
│   ├── CommandBar.cs          # Command Palette UI
│   ├── SyntaxHighlighter.cs   # Syntax highlighting
│   ├── UndoRedoManager.cs     # Undo/Redo system
│   ├── FileManager.cs         # File I/O
│   ├── AppManager.cs          # Application logic
│   └── Program.cs             # Entry point
├── README.md                  # This file
└── RayEdit.csproj            # Project file
```

## 🔮 Project Status

**This proof-of-concept is complete and no further features will be implemented.**

The following features were considered during development but are **not planned for implementation**:
- Search & Replace functionality
- Multiple tabs support
- Line numbering
- Extended mouse support
- Additional language syntax highlighting
- Code folding
- Minimap
- Themes and customization
- IntelliSense-like features
- Plugin system
- Git integration
- Debugging support

This project serves as a **learning resource** and **technical demonstration** only.

## 🤝 Contributing

**This project is a completed proof-of-concept and is not accepting contributions or pull requests.**

However, you are welcome to:
- **Fork** the repository for your own learning and experimentation
- **Study** the code to understand text editor implementation
- **Use** this project as a starting point for your own editor
- **Reference** the techniques demonstrated here in your projects

Please note that issues and pull requests will not be reviewed or merged.

## 📝 License

This project is licensed under the MIT License. See `LICENSE` file for details.

## 🙏 Acknowledgments

- **Raylib** for the fantastic C library
- **Raylib-cs** for the C# bindings
- **FiraCode** for the monospaced font
- The **C# Community** for inspiration and feedback

## 📞 Contact

**This project is archived and not actively maintained.**

For educational purposes or questions about the implementation:
- Review the **source code** and comments for understanding
- Use this project as a **reference** for your own implementations
- Fork and modify for your **learning projects**

---

**RayEdit** - A proof-of-concept demonstrating text editor implementation with Raylib-cs! 📚