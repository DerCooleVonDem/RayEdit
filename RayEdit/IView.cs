namespace TextEditor
{
    /// <summary>
    /// Defines the contract for a view within the application.
    /// Each view represents a different screen, like the editor or a menu.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Loads resources needed for the view.
        /// </summary>
        void Load();

        /// <summary>
        /// Updates the view's state and logic for the current frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Draws the view's contents to the screen.
        /// </summary>
        void Draw();

        /// <summary>
        /// Unloads and cleans up resources used by the view.
        /// </summary>
        void Unload();
    }
}