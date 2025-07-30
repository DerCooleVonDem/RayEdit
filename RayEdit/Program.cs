using Raylib_cs;

namespace TextEditor
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    public class Program
    {
        public static void Main()
        {
            // This commant is from this code.
            // Configure and initialize the main window
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(1024, 768, "RayEdit - Text Editor");
            Raylib.SetTargetFPS(60);
            Raylib.MaximizeWindow();            

            // Create and run the application manager
            AppManager appManager = new AppManager();
            Raylib.SetExitKey(KeyboardKey.KpDecimal);
            appManager.Run();

            // Clean up and close the window
            Raylib.CloseWindow();
        }
    }
}