using RayEdit.UI.Views;
using Raylib_cs;

namespace RayEdit.App
{
    /// <summary>
    /// Manages the main application loop and the active view.
    /// </summary>
    public class AppManager
    {
        private IView _currentView;
        private MenuView _menuView;
        private EditorView _editorView;

        public AppManager()
        {
            // Initialize views
            _menuView = new MenuView();
            _editorView = new EditorView();

            // Set up event handlers
            _menuView.OnFileSelected += OnFileSelected;
            _editorView.OnBackToMenu += OnBackToMenu;

            // Set the initial view to be the MenuView
            _currentView = _menuView;
        }

        /// <summary>
        /// Runs the main application loop.
        /// </summary>
        public void Run()
        {
            _currentView.Load();

            while (!Raylib.WindowShouldClose())
            {
                // Update the current view's logic
                _currentView.Update();

                // Draw the current view
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.DarkGray);
                _currentView.Draw();
                Raylib.EndDrawing();
            }

            _currentView.Unload();
        }

        private void OnFileSelected(string filePath)
        {
            // Switch to editor view and load the selected file
            _currentView.Unload();
            _editorView.LoadFile(filePath);
            _currentView = _editorView;
            _currentView.Load();
        }

        private void OnBackToMenu()
        {
            // Switch back to menu view
            _currentView.Unload();
            _currentView = _menuView;
            _currentView.Load();
        }
    }
}