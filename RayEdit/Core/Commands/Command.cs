namespace RayEdit.Core.Commands
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
}