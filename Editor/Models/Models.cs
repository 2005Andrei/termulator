using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editor.Models;

public class StorySchema
{
    public string title { get; set; } = "New Story";
    public string start_block_uid { get; set; } = string.Empty;
    public string death_block_uid { get; set; } = string.Empty;

    public List<StatePropertyDefinition> properties { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConditionNode? death_condition { get; set; }

    public List<Block> blocks { get; set; } = new();
}

public class StatePropertyDefinition : ObservableObject
{
    private string _key = "new.property";
    private string _hudLabel = "Label";
    private int _min = 0;
    private int _max = 100;
    private int _initial = 50;
    private bool _visibleInHud = true;
    private int _hudOrder = 0;
    private string _onMinBlock = string.Empty;
    private string _onMaxBlock = string.Empty;

    public string key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }
    public string hudLabel
    {
        get => _hudLabel;
        set => SetProperty(ref _hudLabel, value);
    }
    public int min
    {
        get => _min;
        set => SetProperty(ref _min, value);
    }
    public int max
    {
        get => _max;
        set => SetProperty(ref _max, value);
    }
    public int initial
    {
        get => _initial;
        set => SetProperty(ref _initial, value);
    }
    public bool visibleInHud
    {
        get => _visibleInHud;
        set => SetProperty(ref _visibleInHud, value);
    }
    public int hudOrder
    {
        get => _hudOrder;
        set => SetProperty(ref _hudOrder, value);
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string onMinBlock
    {
        get => _onMinBlock;
        set => SetProperty(ref _onMinBlock, value);
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string onMaxBlock
    {
        get => _onMaxBlock;
        set => SetProperty(ref _onMaxBlock, value);
    }
}

public class Block : ObservableObject
{
    private string _g_uid = "new_block";
    private string _ui_dashboard_title = "NEW STAGE";
    private string _ui_card_title = string.Empty;
    private string _ui_card_description = string.Empty;
    private string _ui_color = "White";
    private string _instructions = "Enter narrative text here...";
    private string _hint = string.Empty;

    public string g_uid
    {
        get => _g_uid;
        set => SetProperty(ref _g_uid, value);
    }
    public string ui_dashboard_title
    {
        get => _ui_dashboard_title;
        set => SetProperty(ref _ui_dashboard_title, value);
    }
    public string ui_card_title
    {
        get => _ui_card_title;
        set => SetProperty(ref _ui_card_title, value);
    }
    public string ui_card_description
    {
        get => _ui_card_description;
        set => SetProperty(ref _ui_card_description, value);
    }
    public string ui_color
    {
        get => _ui_color;
        set => SetProperty(ref _ui_color, value);
    }
    public string instructions
    {
        get => _instructions;
        set => SetProperty(ref _instructions, value);
    }
    public string hint
    {
        get => _hint;
        set => SetProperty(ref _hint, value);
    }

    public Dictionary<string, int> wrong_command_penalties { get; set; } = new();

    public List<Decision> decisions { get; set; } = new();
}

public class Decision : ObservableObject
{
    private string _description = "New Decision";
    private string _next_block_uid = string.Empty;

    public string description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    public string next_block_uid
    {
        get => _next_block_uid;
        set => SetProperty(ref _next_block_uid, value);
    }

    public List<string> command_sequence { get; set; } = new();

    public Dictionary<string, int> rewards { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConditionNode? condition { get; set; }
}

public class ConditionNode : ObservableObject
{
    private string _type = "COMPARISON";
    private string _property = string.Empty;
    private string _op = "==";
    private int _value = 0;

    public string type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }
    public string property
    {
        get => _property;
        set => SetProperty(ref _property, value);
    }

    [JsonPropertyName("operator")]
    public string op
    {
        get => _op;
        set => SetProperty(ref _op, value);
    }
    public int value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public List<ConditionNode> children { get; set; } = new();
}

public partial class TreeNode : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    public object? DataContext { get; set; }
    public ObservableCollection<TreeNode> Children { get; } = new();
}
