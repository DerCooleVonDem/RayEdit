using System.Numerics;
using Raylib_cs;

namespace RayEdit.App
{
    /// <summary>
    /// Handles input events for the editor
    /// </summary>
    public class EditorInputHandler
    {
        private bool _isShiftPressed = false;
        private bool _isCtrlPressed = false;
        private bool _isAltPressed = false;

        public delegate void TextInputHandler(string text);
        public delegate void KeyInputHandler(KeyboardKey key, bool isCtrlPressed, bool isShiftPressed, bool isAltPressed);
        public delegate void MouseInputHandler(Vector2 mousePosition, bool isClicked, bool isDragging);

        public event TextInputHandler OnTextInput;
        public event KeyInputHandler OnKeyPressed;
        public event MouseInputHandler OnMouseInput;

        public void Update()
        {
            // Update modifier key states
            _isShiftPressed = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
            _isCtrlPressed = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
            _isAltPressed = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);

            // Handle text input
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                // Convert key to string if it's a printable character
                if (key >= 32 && key <= 126)
                {
                    OnTextInput?.Invoke(((char)key).ToString());
                }
                key = Raylib.GetCharPressed();
            }

            // Handle special keys
            HandleSpecialKeys();

            // Handle mouse input
            HandleMouseInput();
        }

        private void HandleSpecialKeys()
        {
            var keys = new[]
            {
                KeyboardKey.Backspace,
                KeyboardKey.Delete,
                KeyboardKey.Enter,
                KeyboardKey.Tab,
                KeyboardKey.Left,
                KeyboardKey.Right,
                KeyboardKey.Up,
                KeyboardKey.Down,
                KeyboardKey.Home,
                KeyboardKey.End,
                KeyboardKey.PageUp,
                KeyboardKey.PageDown,
                KeyboardKey.Escape,
                KeyboardKey.F1,
                KeyboardKey.F2,
                KeyboardKey.F3,
                KeyboardKey.F4,
                KeyboardKey.F5,
                KeyboardKey.F6,
                KeyboardKey.F7,
                KeyboardKey.F8,
                KeyboardKey.F9,
                KeyboardKey.F10,
                KeyboardKey.F11,
                KeyboardKey.F12,
                KeyboardKey.A,
                KeyboardKey.C,
                KeyboardKey.V,
                KeyboardKey.X,
                KeyboardKey.Z,
                KeyboardKey.Y,
                KeyboardKey.S,
                KeyboardKey.O,
                KeyboardKey.N,
                KeyboardKey.W,
                KeyboardKey.F
            };

            foreach (var key in keys)
            {
                if (Raylib.IsKeyPressed(key))
                {
                    OnKeyPressed?.Invoke(key, _isCtrlPressed, _isShiftPressed, _isAltPressed);
                }
            }
        }

        private void HandleMouseInput()
        {
            Vector2 mousePosition = Raylib.GetMousePosition();
            bool isClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
            bool isDragging = Raylib.IsMouseButtonDown(MouseButton.Left);

            if (isClicked || isDragging)
            {
                OnMouseInput?.Invoke(mousePosition, isClicked, isDragging);
            }
        }

        public bool IsModifierPressed(KeyboardKey modifier)
        {
            return modifier switch
            {
                KeyboardKey.LeftShift or KeyboardKey.RightShift => _isShiftPressed,
                KeyboardKey.LeftControl or KeyboardKey.RightControl => _isCtrlPressed,
                KeyboardKey.LeftAlt or KeyboardKey.RightAlt => _isAltPressed,
                _ => false
            };
        }

        public bool IsShiftPressed => _isShiftPressed;
        public bool IsCtrlPressed => _isCtrlPressed;
        public bool IsAltPressed => _isAltPressed;
    }
}