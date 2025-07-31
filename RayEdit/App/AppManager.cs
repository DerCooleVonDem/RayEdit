using RayEdit.Core;
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
        private StartScreenView _startScreenView;
        private MenuView _menuView;
        private EditorView _editorView;
        private RecentFilesManager _recentFilesManager;

        public AppManager()
        {
            // Initialize shared RecentFilesManager
            _recentFilesManager = new RecentFilesManager();
            
            // Initialize views
            _startScreenView = new StartScreenView();
            _menuView = new MenuView();
            _editorView = new EditorView();

            // Share RecentFilesManager with views
            _startScreenView.SetRecentFilesManager(_recentFilesManager);
            _editorView.SetRecentFilesManager(_recentFilesManager);

            // Set up event handlers
            _startScreenView.OnFileSelected += OnFileSelected;
            _startScreenView.OnBrowseFiles += OnBrowseFiles;
            _startScreenView.OnBrowseDirectory += OnBrowseDirectory;
            _menuView.OnFileSelected += OnFileSelected;
            _menuView.OnBackToStartScreen += OnBackToStartScreen;
            _editorView.OnBackToMenu += OnBackToStartScreen;

            // Set the initial view to be the StartScreenView
            _currentView = _startScreenView;
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

        private void OnBrowseFiles()
        {
            // Switch to file browser (MenuView)
            _currentView.Unload();
            _currentView = _menuView;
            _currentView.Load();
        }

        private void OnBrowseDirectory(string directoryPath)
        {
            // Switch to file browser (MenuView) and set the directory
            _currentView.Unload();
            _menuView.SetDirectory(directoryPath);
            _currentView = _menuView;
            _currentView.Load();
        }

        private void OnBackToStartScreen()
        {
            // Switch back to start screen
            _currentView.Unload();
            _currentView = _startScreenView;
            _currentView.Load();
        }
    }
}