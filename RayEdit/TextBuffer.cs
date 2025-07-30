using System;

namespace TextEditor
{
    /// <summary>
    /// Represents the data model for the text editor.
    /// It holds the text content, cursor position, and selection state,
    /// and contains all logic for manipulating the text.
    /// </summary>
    public class TextBuffer
    {
        public string Content { get; private set; } = "";
        public int CursorIndex { get; private set; } = 0;
        public int SelectionStart { get; private set; } = -1;
        public int SelectionEnd { get; private set; } = -1;
        public bool IsSelecting { get; private set; } = false;
        
        // Undo/Redo functionality
        private readonly UndoRedoManager _undoRedoManager = new UndoRedoManager();
        public bool CanUndo => _undoRedoManager.CanUndo;
        public bool CanRedo => _undoRedoManager.CanRedo;

        public TextBuffer(string initialContent = "")
        {
            SetContent(initialContent);
        }

        public bool HasSelection() => SelectionStart != -1 && SelectionEnd != -1 && SelectionStart != SelectionEnd;

        public (int start, int end) GetSelectionRange()
        {
            if (!HasSelection()) return (-1, -1);
            return (Math.Min(SelectionStart, SelectionEnd), Math.Max(SelectionStart, SelectionEnd));
        }

        public string GetSelectedText()
        {
            if (!HasSelection()) return "";
            var (start, end) = GetSelectionRange();
            return Content.Substring(start, end - start);
        }

        public void ClearSelection()
        {
            SelectionStart = -1;
            SelectionEnd = -1;
            IsSelecting = false;
        }

        public void StartSelection()
        {
            SelectionStart = CursorIndex;
            SelectionEnd = CursorIndex;
            IsSelecting = true;
        }

        public void UpdateSelection()
        {
            if (IsSelecting)
            {
                SelectionEnd = CursorIndex;
            }
        }

        public void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = Content.Length;
            CursorIndex = Content.Length;
            IsSelecting = true;
        }

        public void InsertText(string text)
        {
            int oldCursorPos = CursorIndex;
            string oldContent = Content;
            
            if (HasSelection())
            {
                DeleteSelectedText();
            }
            
            // Record the insert command for undo/redo
            _undoRedoManager.RecordCommand("Insert", CursorIndex, "", text, oldCursorPos, CursorIndex + text.Length);
            
            Content = Content.Insert(CursorIndex, text);
            CursorIndex += text.Length;
            ClearSelection();
        }

        public void PerformBackspace(bool isCtrlPressed)
        {
            if (HasSelection())
            {
                DeleteSelectedText();
            }
            else if (isCtrlPressed)
            {
                DeleteWordBackward();
            }
            else if (CursorIndex > 0)
            {
                int oldCursorPos = CursorIndex;
                string deletedChar = Content.Substring(CursorIndex - 1, 1);
                
                // Record the backspace command for undo/redo
                _undoRedoManager.RecordCommand("Backspace", CursorIndex - 1, deletedChar, "", oldCursorPos, CursorIndex - 1);
                
                Content = Content.Remove(CursorIndex - 1, 1);
                MoveCursor(-1);
            }
        }

        public void PerformDelete(bool isCtrlPressed)
        {
            if (HasSelection())
            {
                DeleteSelectedText();
            }
            else if (isCtrlPressed)
            {
                DeleteWordForward();
            }
            else if (CursorIndex < Content.Length)
            {
                int oldCursorPos = CursorIndex;
                string deletedChar = Content.Substring(CursorIndex, 1);
                
                // Record the delete command for undo/redo
                _undoRedoManager.RecordCommand("Delete", CursorIndex, deletedChar, "", oldCursorPos, CursorIndex);
                
                Content = Content.Remove(CursorIndex, 1);
            }
        }

        public void MoveCursor(int delta)
        {
            CursorIndex = Math.Clamp(CursorIndex + delta, 0, Content.Length);
        }

        public void SetCursorPosition(int position)
        {
            CursorIndex = Math.Clamp(position, 0, Content.Length);
        }

        private void DeleteSelectedText()
        {
            if (!HasSelection()) return;
            var (start, end) = GetSelectionRange();
            
            int oldCursorPos = CursorIndex;
            string deletedText = Content.Substring(start, end - start);
            
            // Record the delete selection command for undo/redo
            _undoRedoManager.RecordCommand("Delete", start, deletedText, "", oldCursorPos, start);
            
            Content = Content.Remove(start, end - start);
            CursorIndex = start;
            ClearSelection();
        }

        // Word navigation and deletion logic
        private bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

        public void MoveToWordStart()
        {
            if (CursorIndex <= 0) return;
            int newIndex = CursorIndex;
            while (newIndex > 0 && !IsWordCharacter(Content[newIndex - 1])) newIndex--;
            while (newIndex > 0 && IsWordCharacter(Content[newIndex - 1])) newIndex--;
            CursorIndex = newIndex;
        }

        public void MoveToWordEnd()
        {
            if (CursorIndex >= Content.Length) return;
            int newIndex = CursorIndex;

            // Finde das Ende der aktuellen Zeile
            int lineEnd = Content.IndexOf('\n', newIndex);
            if (lineEnd == -1) lineEnd = Content.Length;

            // Springe zum Ende des aktuellen Wortes innerhalb der Zeile
            while (newIndex < lineEnd && IsWordCharacter(Content[newIndex])) newIndex++;
            // Überspringe ggf. folgende Nicht-Wort-Zeichen, aber nicht über das Zeilenende hinaus
            while (newIndex < lineEnd && !IsWordCharacter(Content[newIndex])) newIndex++;

            // Wenn wir am Ende der Zeile sind, Cursor auf das letzte Zeichen der Zeile setzen
            if (newIndex >= lineEnd)
                CursorIndex = lineEnd;
            else
                CursorIndex = newIndex;
        }

        private void DeleteWordBackward()
        {
            if (CursorIndex <= 0) return;
            int startPos = CursorIndex;
            int oldCursorPos = CursorIndex;
            MoveToWordStart();
            
            string deletedText = Content.Substring(CursorIndex, startPos - CursorIndex);
            
            // Record the delete word backward command for undo/redo
            _undoRedoManager.RecordCommand("Delete", CursorIndex, deletedText, "", oldCursorPos, CursorIndex);
            
            Content = Content.Remove(CursorIndex, startPos - CursorIndex);
        }

        private void DeleteWordForward()
        {
            if (CursorIndex >= Content.Length) return;
            int startPos = CursorIndex;
            int oldCursorPos = CursorIndex;
            MoveToWordEnd();
            
            string deletedText = Content.Substring(startPos, CursorIndex - startPos);
            
            // Record the delete word forward command for undo/redo
            _undoRedoManager.RecordCommand("Delete", startPos, deletedText, "", oldCursorPos, startPos);
            
            Content = Content.Remove(startPos, CursorIndex - startPos);
            CursorIndex = startPos;
        }

        /// <summary>
        /// Performs undo operation
        /// </summary>
        public void Undo()
        {
            var (newContent, newCursorPos) = _undoRedoManager.Undo(Content, CursorIndex);
            Content = newContent;
            CursorIndex = newCursorPos;
            ClearSelection();
        }

        /// <summary>
        /// Performs redo operation
        /// </summary>
        public void Redo()
        {
            var (newContent, newCursorPos) = _undoRedoManager.Redo(Content, CursorIndex);
            Content = newContent;
            CursorIndex = newCursorPos;
            ClearSelection();
        }

        /// <summary>
        /// Finalizes the current command group (called when user stops typing)
        /// </summary>
        public void FinalizeCurrentGroup()
        {
            _undoRedoManager.FinalizeCurrentGroup();
        }

        /// <summary>
        /// Clears all undo/redo history
        /// </summary>
        public void ClearUndoHistory()
        {
            _undoRedoManager.Clear();
        }

        /// <summary>
        /// Sets new content and clears undo history (used when loading files)
        /// </summary>
        public void SetContent(string content)
        {
            Content = content;
            CursorIndex = 0;
            ClearSelection();
            ClearUndoHistory();
        }
    }
}
