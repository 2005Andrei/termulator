using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace termulator.ViewModels;

public class StatePropertyDefinition
{
    public string key { get; set; } = string.Empty;
    public string hudLabel { get; set; } = string.Empty;
    public int min { get; set; } = 0;
    public int max { get; set; } = 100;
    public int initial { get; set; } = 0;
    public bool visibleInHud { get; set; } = true;
    public int hudOrder { get; set; } = 0;

    public string? onMinBlock { get; set; }
    public string? onMaxBlock { get; set; }
}

public partial class HudStat : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private int _value;

    public string DisplayText => $"{Label.ToUpper()}: {Value}";
}

public class CommandResult
{
    public bool AdvanceToNextBlock { get; set; } = false;
    public string NextBlockUid { get; set; } = string.Empty;
    public Dictionary<string, int> Deltas { get; set; } = new();
    public bool WasCorrectStep { get; set; } = false;
}

public class Decision
{
    public string description { get; set; } = "Unknown Path";
    public List<string> command_sequence { get; set; } = new List<string>();
    public string next_block_uid { get; set; } = string.Empty;

    public Dictionary<string, int> rewards { get; set; } = new();

    [JsonIgnore]
    public int current_step { get; set; } = 0;

    [JsonIgnore]
    public string CommandSequenceDisplay => string.Join(" → ", command_sequence);

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

    public Dictionary<string, int> wrong_command_penalties { get; set; } = new();
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
            int statValue = state.GetVariable(property);

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

public class StorySchema
{
    public string start_block_uid { get; set; } = string.Empty;
    public string death_block_uid { get; set; } = string.Empty;

    public ConditionNode? death_condition { get; set; }

    public List<StatePropertyDefinition> properties { get; set; } = new();

    public List<Block> blocks { get; set; } = new();
}
