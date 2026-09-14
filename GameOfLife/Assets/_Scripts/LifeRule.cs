using System;
using System.Collections.Generic;

/// <summary>
/// Represents the rules for a single species (or the whole game for now).
/// Serialized to JSON.
/// </summary>
[Serializable]
public class LifeRule
{
    // B3/S23 notation
    // "B" means Birth counts (e.g., 3)
    // "S" means Survival counts (e.g., 2, 3)
    public string RuleString = "B3/S23";

    // We will parse this string into these lists once, 
    // so we don't parse strings during the simulation loop (too slow).
    [NonSerialized]
    public HashSet<int> BirthCounts;
    [NonSerialized]
    public HashSet<int> SurvivalCounts;

    /// <summary>
    /// Parses "B3/S23" into sets of integers.
    /// </summary>
    public void Parse()
    {
        BirthCounts = new HashSet<int>();
        SurvivalCounts = new HashSet<int>();

        // Simple parser: Split by '/'
        string[] parts = RuleString.Split('/');

        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            char type = part[0];
            string numbers = part.Substring(1);

            // Parse each digit
            foreach (char c in numbers)
            {
                if (char.IsDigit(c))
                {
                    int val = c - '0'; // Convert char '3' to int 3
                    if (type == 'B') BirthCounts.Add(val);
                    else if (type == 'S') SurvivalCounts.Add(val);
                }
            }
        }
    }
}