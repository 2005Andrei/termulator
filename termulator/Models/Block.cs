using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace termulator.ViewModels;

public class StatModifiers
{
    public int efficiency { get; set; } = 0;
    public int knowledge { get; set; } = 0;
    public int evidence { get; set; } = 0;
    public int reputation { get; set; } = 0;
}

public class ConditionNode
{
    public string type { get; set; } = "COMPARISON";

    public string property { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string op { get; set; } = "==";
    public int value { get; set; } = 0;

    public List<ConditionNode> children { get; set; } = new();

    public bool Evaluate(State state)
    {
        if (type == "AND")
        {
            if (children.Count == 0)
                return true;
            foreach (var child in children)
                if (!child.Evaluate(state))
                    return false;
            return true;
        }

        if (type == "OR")
        {
            if (children.Count == 0)
                return false;
            foreach (var child in children)
                if (child.Evaluate(state))
                    return true;
            return false;
        }

        if (type == "COMPARISON")
        {
            int statValue = property.ToLower() switch
            {
                "efficiency" => state.Efficiency,
                "knowledge" => state.Knowledge,
                "evidence" => state.Evidence,
                "reputation" => state.Reputation,
                _ => 0,
            };

            return op switch
            {
                ">" => statValue > value,
                "<" => statValue < value,
                ">=" => statValue >= value,
                "<=" => statValue <= value,
                "==" => statValue == value,
                "!=" => statValue != value,
                _ => false,
            };
        }

        return false;
    }
}

public class CommandResult
{
    public bool AdvanceToNextBlock { get; set; } = false;
    public string NextBlockUid { get; set; } = string.Empty;
    public StatModifiers Deltas { get; set; } = new();

    public bool WasCorrectStep { get; set; } = false;
}

public class Decision
{
    public List<string> command_sequence { get; set; } = new List<string>();
    public string next_block_uid { get; set; } = string.Empty;
    public StatModifiers rewards { get; set; } = new();
    public string description { get; set; } = "Unknown Path";

    [JsonIgnore]
    public string CommandSequenceDisplay => string.Join(" → ", command_sequence);

    [JsonIgnore]
    public int current_step { get; set; } = 0;

    public bool IsNextCommand(string command)
    {
        if (current_step < command_sequence.Count)
        {
            return command_sequence[current_step] == command;
        }
        return false;
    }

    public bool IsFinished()
    {
        return current_step >= command_sequence.Count;
    }
}

public class Block
{
    public string g_uid { get; set; } = string.Empty;
    public string instructions { get; set; } = string.Empty;

    public string hint { get; set; } = string.Empty;

    public string ui_color { get; set; } = "Gray";
    public string ui_dashboard_title { get; set; } = "UNKNOWN";
    public string ui_card_title { get; set; } = "BLOCK PENDING";
    public string ui_card_description { get; set; } = "Description missing.";

    public StatModifiers wrong_command_penalties { get; set; } = new();

    public List<Decision> decisions { get; set; } = new List<Decision>();

    public CommandResult commandEntered(string currentCommand)
    {
        var result = new CommandResult();
        bool correctStepTaken = false;

        foreach (var decision in decisions)
        {
            if (decision.IsNextCommand(currentCommand))
            {
                decision.current_step++;
                correctStepTaken = true;

                if (decision.IsFinished())
                {
                    result.AdvanceToNextBlock = true;
                    result.NextBlockUid = decision.next_block_uid;
                    result.Deltas = decision.rewards;

                    return result;
                }
            }
        }

        if (correctStepTaken)
        {
            result.AdvanceToNextBlock = false;
            result.WasCorrectStep = true;
        }
        else
        {
            result.AdvanceToNextBlock = false;
            result.WasCorrectStep = false;
            result.Deltas = wrong_command_penalties;
        }

        return result;
    }
}

public class StorySchema
{
    public string start_block_uid { get; set; } = string.Empty;
    public string death_block_uid { get; set; } = string.Empty;

    public StatModifiers initial_stats { get; set; } = new();
    public StatModifiers min_stats { get; set; } = new();
    public StatModifiers max_stats { get; set; } = new();

    public ConditionNode? death_condition { get; set; }

    public List<Block> blocks { get; set; } = new List<Block>();
}
