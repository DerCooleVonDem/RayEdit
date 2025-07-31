using System;
using System.Collections.Generic;
using System.Linq;

namespace TextEditor
{
    /// <summary>
    /// Represents a command that can be executed from the command bar
    /// </summary>
    public class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public Action<string[]> Action { get; set; }
        public string[] Aliases { get; set; }
        public string Usage { get; set; }

        public Command(string name, string description, Action<string[]> action, string category = "General", string[] aliases = null, string usage = null)
        {
            Name = name;
            Description = description;
            Action = action;
            Category = category;
            Aliases = aliases ?? new string[0];
            Usage = usage ?? name;
        }

        public bool MatchesQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            
            query = query.ToLower();
            
            // Check name
            if (Name.ToLower().Contains(query)) return true;
            
            // Check aliases
            if (Aliases.Any(alias => alias.ToLower().Contains(query))) return true;
            
            // Check description
            if (Description.ToLower().Contains(query)) return true;
            
            // Check category
            if (Category.ToLower().Contains(query)) return true;
            
            return false;
        }

        public int GetMatchScore(string query)
        {
            if (string.IsNullOrEmpty(query)) return 0;
            
            query = query.ToLower();
            int score = 0;
            
            // Exact name match gets highest score
            if (Name.ToLower() == query) score += 1000;
            else if (Name.ToLower().StartsWith(query)) score += 500;
            else if (Name.ToLower().Contains(query)) score += 100;
            
            // Alias matches
            foreach (var alias in Aliases)
            {
                if (alias.ToLower() == query) score += 800;
                else if (alias.ToLower().StartsWith(query)) score += 400;
                else if (alias.ToLower().Contains(query)) score += 80;
            }
            
            // Description matches
            if (Description.ToLower().Contains(query)) score += 50;
            
            return score;
        }
    }

    /// <summary>
    /// Represents different contexts where commands can be executed
    /// </summary>
    public enum CommandContext
    {
        Editor,
        FileExplorer,
        Global
    }

    /// <summary>
    /// Manages all available commands and provides search functionality
    /// </summary>
    public class CommandRegistry
    {
        private readonly Dictionary<CommandContext, List<Command>> _contextCommands = new Dictionary<CommandContext, List<Command>>();
        private CommandContext _currentContext = CommandContext.Global;

        public CommandRegistry()
        {
            _contextCommands[CommandContext.Global] = new List<Command>();
            _contextCommands[CommandContext.Editor] = new List<Command>();
            _contextCommands[CommandContext.FileExplorer] = new List<Command>();
        }

        public void SetContext(CommandContext context)
        {
            _currentContext = context;
        }

        public CommandContext GetCurrentContext()
        {
            return _currentContext;
        }
        
        public void RegisterCommand(Command command, CommandContext context = CommandContext.Global)
        {
            _contextCommands[context].Add(command);
        }
        
        public void RegisterCommand(string name, string description, Action<string[]> action, string category = "General", string[] aliases = null, string usage = null, CommandContext context = CommandContext.Global)
        {
            RegisterCommand(new Command(name, description, action, category, aliases, usage), context);
        }
        
        public List<Command> SearchCommands(string query, int maxResults = 10)
        {
            var availableCommands = GetAvailableCommands();
            
            if (string.IsNullOrEmpty(query))
            {
                return availableCommands.Take(maxResults).ToList();
            }
            
            return availableCommands
                .Where(cmd => cmd.MatchesQuery(query))
                .OrderByDescending(cmd => cmd.GetMatchScore(query))
                .Take(maxResults)
                .ToList();
        }

        private List<Command> GetAvailableCommands()
        {
            var commands = new List<Command>();
            
            // Always include global commands
            commands.AddRange(_contextCommands[CommandContext.Global]);
            
            // Add context-specific commands
            if (_currentContext != CommandContext.Global)
            {
                commands.AddRange(_contextCommands[_currentContext]);
            }
            
            return commands;
        }
        
        public Command? FindCommand(string name)
        {
            var availableCommands = GetAvailableCommands();
            return availableCommands.FirstOrDefault(cmd => 
                cmd.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                cmd.Aliases.Any(alias => alias.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
        
        public bool ExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return false;
            
            var parts = ParseCommandLine(commandLine);
            if (parts.Length == 0) return false;
            
            var command = FindCommand(parts[0]);
            if (command == null) return false;
            
            try
            {
                var args = parts.Skip(1).ToArray();
                command.Action(args);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command '{parts[0]}': {ex.Message}");
                return false;
            }
        }
        
        private string[] ParseCommandLine(string commandLine)
        {
            var parts = new List<string>();
            var current = "";
            bool inQuotes = false;
            
            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                
                if (c == '"' && (i == 0 || commandLine[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(current))
                    {
                        parts.Add(current);
                        current = "";
                    }
                }
                else
                {
                    current += c;
                }
            }
            
            if (!string.IsNullOrEmpty(current))
            {
                parts.Add(current);
            }
            
            return parts.ToArray();
        }
        
        public List<string> GetCategories()
        {
            var availableCommands = GetAvailableCommands();
            return availableCommands.Select(cmd => cmd.Category).Distinct().OrderBy(c => c).ToList();
        }
        
        public List<Command> GetCommandsByCategory(string category)
        {
            var availableCommands = GetAvailableCommands();
            return availableCommands.Where(cmd => cmd.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}