using System.IO;

namespace TextEditor
{
    /// <summary>
    /// Provides static methods for loading from and saving to text files.
    /// </summary>
    public static class FileManager
    {
        public static string LoadText(string filePath)
        {
            if (!File.Exists(filePath))
                return "";

            try
            {
                // In FileManager.LoadText method
                string content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                return content.Replace("\r\n", "\n").Replace("\r", "\n");
            }
            catch (IOException e)
            {
                System.Console.WriteLine($"Error loading file: {e.Message}");
                return "";
            }
        }

        public static void SaveText(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
            }
            catch (IOException e)
            {
                System.Console.WriteLine($"Error saving file: {e.Message}");
            }
        }
    }
}