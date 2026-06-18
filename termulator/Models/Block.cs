using System.Collections.Generic;
using System.Text.Json;

namespace termulator.ViewModels;

public class Decision
{
    public List<string> command_sequence { get; set; } = new List<string>();
    public string next_block_uid { get; set; }
    public int counter { get; set; } = 0; // increase it each time a command runs

    public bool finished => counter == command_sequence.Count;

    public bool isCommandHere(string command)
    {
        foreach (string c in command_sequence)
        {
            if (c.Equals(command))
            {
                return true;
            }
        }
        return false;
    }

    public bool checkDockerContainer()
    {
        bool hasKey = false;
        // execute a docker command
        // or return a docker command to check for something
        return hasKey;
    }
}

public class Block
{
    public required string g_uid { get; set; }
    public List<Decision> decisions { get; set; } = new List<Decision>();

    public string instructions { get; set; }
    public string[] penalties { get; set; }

    public void setDecisions() { // json object and check for conditions that satisfy the current player stats
    }

    public Dictionary<string, int> commandEntered(string currentCommand) // returns next block if current decision is completed
    {
        // won't check for order, or not yet at least
        int found = decisions.FindIndex(d => d.isCommandHere(currentCommand));

        // this is horrible, I know
        if (found != -1)
        {
            var foundRez = new Dictionary<string, int>
            {
                { "nextBlock", 1 },
                { decisions[found].next_block_uid, 0 },
            };
            var addition = enforcePenalty(found, currentCommand);

            // need to merge the dicts
            return foundRez;
        }
        return enforcePenalty(currentCommand);
    }

    public Dictionary<string, int> enforcePenalty(string currentCommand) // only for good
    {
        var computed_penalties = new Dictionary<string, int>
        {
            { "efficiency", 0 },
            { "knowledge", 0 },
            { "evidence", 0 },
            { "reputation", 0 },
        };

        // here i search in the block to return the current subtractions/additions, and I add/subtract them in mainwindowviewmodel

        return computed_penalties;
    }

    public Dictionary<string, int> enforcePenalty(int blockId, string currentCommand)
    {
        var computed_penalties = new Dictionary<string, int>
        {
            { "efficiency", 0 },
            { "knowledge", 0 },
            { "evidence", 0 },
            { "reputation", 0 },
        };

        return computed_penalties;
    }
}
