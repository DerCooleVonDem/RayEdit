using System;
using System.Collections.Generic;
using System.Linq;

namespace TextEditor
{
    /// <summary>
    /// Represents a single edit operation that can be undone/redone
    /// </summary>
    public class EditCommand
    {
        public string Type { get; set; }
        public int Position { get; set; }
        public string OldText { get; set; }
        public string NewText { get; set; }
        public int OldCursorPosition { get; set; }
        public int NewCursorPosition { get; set; }
        public DateTime Timestamp { get; set; }
        
        public EditCommand(string type, int position, string oldText, string newText, int oldCursorPos, int newCursorPos)
        {
            Type = type;
            Position = position;
            OldText = oldText ?? "";
            NewText = newText ?? "";
            OldCursorPosition = oldCursorPos;
            NewCursorPosition = newCursorPos;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Represents a group of commands that should be undone/redone together
    /// </summary>
    public class CommandGroup
    {
        public List<EditCommand> Commands { get; set; } = new List<EditCommand>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string GroupType { get; set; }

        public CommandGroup(string groupType)
        {
            GroupType = groupType;
            StartTime = DateTime.Now;
        }

        public void AddCommand(EditCommand command)
        {
            Commands.Add(command);
            EndTime = DateTime.Now;
        }

        public bool IsEmpty => Commands.Count == 0;
    }

    /// <summary>
    /// Manages undo/redo operations with intelligent grouping like in modern IDEs
    /// </summary>
    public class UndoRedoManager
    {
        private readonly List<CommandGroup> _undoStack = new List<CommandGroup>();
        private readonly List<CommandGroup> _redoStack = new List<CommandGroup>();
        private CommandGroup _currentGroup = null;
        private DateTime _lastActionTime = DateTime.MinValue;
        
        // Configuration for intelligent grouping
        private const double GroupingTimeoutMs = 1000; // 1 second timeout for grouping
        private const int MaxUndoStackSize = 100;
        
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Records a new edit command and handles intelligent grouping
        /// </summary>
        public void RecordCommand(string type, int position, string oldText, string newText, int oldCursorPos, int newCursorPos)
        {
            var command = new EditCommand(type, position, oldText, newText, oldCursorPos, newCursorPos);
            
            // Clear redo stack when new command is recorded
            _redoStack.Clear();
            
            // Determine if we should start a new group or continue the current one
            bool shouldStartNewGroup = ShouldStartNewGroup(command);
            
            if (shouldStartNewGroup || _currentGroup == null)
            {
                // Finalize current group if it exists
                if (_currentGroup != null && !_currentGroup.IsEmpty)
                {
                    _undoStack.Add(_currentGroup);
                }
                
                // Start new group
                _currentGroup = new CommandGroup(DetermineGroupType(command));
            }
            
            _currentGroup.AddCommand(command);
            _lastActionTime = DateTime.Now;
            
            // Limit undo stack size
            while (_undoStack.Count > MaxUndoStackSize)
            {
                _undoStack.RemoveAt(0);
            }
        }

        /// <summary>
        /// Finalizes the current command group (called when user stops typing)
        /// </summary>
        public void FinalizeCurrentGroup()
        {
            if (_currentGroup != null && !_currentGroup.IsEmpty)
            {
                _undoStack.Add(_currentGroup);
                _currentGroup = null;
            }
        }

        /// <summary>
        /// Performs undo operation and returns the text state to restore
        /// </summary>
        public (string content, int cursorPosition) Undo(string currentContent, int currentCursorPos)
        {
            FinalizeCurrentGroup();
            
            if (!CanUndo) return (currentContent, currentCursorPos);
            
            var group = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(group);
            
            // Apply commands in reverse order
            string content = currentContent;
            int cursorPos = currentCursorPos;
            
            for (int i = group.Commands.Count - 1; i >= 0; i--)
            {
                var command = group.Commands[i];
                content = ApplyUndoCommand(content, command);
                cursorPos = command.OldCursorPosition;
            }
            
            return (content, cursorPos);
        }

        /// <summary>
        /// Performs redo operation and returns the text state to restore
        /// </summary>
        public (string content, int cursorPosition) Redo(string currentContent, int currentCursorPos)
        {
            if (!CanRedo) return (currentContent, currentCursorPos);
            
            var group = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(group);
            
            // Apply commands in forward order
            string content = currentContent;
            int cursorPos = currentCursorPos;
            
            foreach (var command in group.Commands)
            {
                content = ApplyRedoCommand(content, command);
                cursorPos = command.NewCursorPosition;
            }
            
            return (content, cursorPos);
        }

        /// <summary>
        /// Clears all undo/redo history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentGroup = null;
            _lastActionTime = DateTime.MinValue;
        }

        private bool ShouldStartNewGroup(EditCommand command)
        {
            if (_currentGroup == null) return true;
            
            // Time-based grouping
            var timeSinceLastAction = (DateTime.Now - _lastActionTime).TotalMilliseconds;
            if (timeSinceLastAction > GroupingTimeoutMs) return true;
            
            // Type-based grouping
            string currentGroupType = _currentGroup.GroupType;
            string newCommandType = DetermineGroupType(command);
            
            if (currentGroupType != newCommandType) return true;
            
            // Position-based grouping for typing
            if (command.Type == "Insert" && currentGroupType == "Typing")
            {
                var lastCommand = _currentGroup.Commands.LastOrDefault();
                if (lastCommand != null)
                {
                    // Continue typing group if inserting at consecutive positions
                    return Math.Abs(command.Position - (lastCommand.Position + lastCommand.NewText.Length)) > 1;
                }
            }
            
            if (command.Type == "Delete" && currentGroupType == "Deletion")
            {
                var lastCommand = _currentGroup.Commands.LastOrDefault();
                if (lastCommand != null)
                {
                    // Continue deletion group if deleting at consecutive positions
                    return Math.Abs(command.Position - lastCommand.Position) > 1;
                }
            }
            
            return false;
        }

        private string DetermineGroupType(EditCommand command)
        {
            switch (command.Type)
            {
                case "Insert":
                    if (command.NewText.Length == 1 && char.IsLetterOrDigit(command.NewText[0]))
                        return "Typing";
                    if (command.NewText == "\n")
                        return "NewLine";
                    if (command.NewText.Length > 1)
                        return "Paste";
                    return "Insert";
                
                case "Delete":
                case "Backspace":
                    return "Deletion";
                
                case "Replace":
                    return "Replace";
                
                default:
                    return "Other";
            }
        }

        private string ApplyUndoCommand(string content, EditCommand command)
        {
            switch (command.Type)
            {
                case "Insert":
                    // Remove the inserted text
                    if (command.Position <= content.Length && 
                        command.Position + command.NewText.Length <= content.Length)
                    {
                        return content.Remove(command.Position, command.NewText.Length);
                    }
                    break;
                
                case "Delete":
                case "Backspace":
                    // Restore the deleted text
                    if (command.Position <= content.Length)
                    {
                        return content.Insert(command.Position, command.OldText);
                    }
                    break;
                
                case "Replace":
                    // Restore the old text
                    if (command.Position <= content.Length && 
                        command.Position + command.NewText.Length <= content.Length)
                    {
                        return content.Remove(command.Position, command.NewText.Length)
                                     .Insert(command.Position, command.OldText);
                    }
                    break;
            }
            
            return content;
        }

        private string ApplyRedoCommand(string content, EditCommand command)
        {
            switch (command.Type)
            {
                case "Insert":
                    // Re-insert the text
                    if (command.Position <= content.Length)
                    {
                        return content.Insert(command.Position, command.NewText);
                    }
                    break;
                
                case "Delete":
                case "Backspace":
                    // Re-delete the text
                    if (command.Position <= content.Length && 
                        command.Position + command.OldText.Length <= content.Length)
                    {
                        return content.Remove(command.Position, command.OldText.Length);
                    }
                    break;
                
                case "Replace":
                    // Re-apply the replacement
                    if (command.Position <= content.Length && 
                        command.Position + command.OldText.Length <= content.Length)
                    {
                        return content.Remove(command.Position, command.OldText.Length)
                                     .Insert(command.Position, command.NewText);
                    }
                    break;
            }
            
            return content;
        }
    }
}