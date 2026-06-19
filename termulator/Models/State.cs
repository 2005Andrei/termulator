using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace termulator.ViewModels;

public class SaveStateData
{
    public string CurrentBlockUid { get; set; } = string.Empty;
    public Dictionary<string, int> Variables { get; set; } = new();
}

public partial class State : ObservableObject
{
    // list of blocks
    // efficiency, knowledge, evidence, reputation

    // method for adding stats from the computed penalties in each block

    // then probably a method to load stuff from json files

    private Dictionary<string, Block> _blockDict = new();
    public ObservableCollection<GraphNode> GraphNodes { get; } = new();

    private Dictionary<string, StatePropertyDefinition> _propDefs = new();
    private Dictionary<string, int> _variables = new();

    public ObservableCollection<HudStat> HudStats { get; } = new();

    private ConditionNode? _deathCondition;
    private string _deathBlockUid = string.Empty;

    [ObservableProperty]
    public Block? _currentBlock;

    [ObservableProperty]
    private GraphNode? _activeNode;

    [ObservableProperty]
    public string _title;

    // [ObservableProperty]
    // private int _efficiency = 0;
    //
    // [ObservableProperty]
    // private int _knowledge = 0;
    //
    // [ObservableProperty]
    // private int _evidence = 0;
    //
    // [ObservableProperty]
    // private int _reputation = 0;

    public bool IsGameOver { get; private set; } = false;

    public State()
    {
        // SetActiveNode();
    }

    public void SetActiveNode()
    {
        if (GraphNodes.Count == 0)
            return;

        if (ActiveNode == null)
        {
            ActiveNode = GraphNodes[0];
            Console.WriteLine($"started at {ActiveNode.DashboardTitle}");
            return;
        }

        int currentIndex = GraphNodes.IndexOf(ActiveNode);

        if (currentIndex == GraphNodes.Count - 1)
        {
            Console.WriteLine("game done");
        }
        else
        {
            ActiveNode = GraphNodes[currentIndex + 1];
            Console.WriteLine($"moved to {ActiveNode.DashboardTitle}");
        }
    }

    public int GetVariable(string key)
    {
        return _variables.TryGetValue(key, out int val) ? val : 0;
    }

    public string assessCommand(string currentCommand)
    {
        if (CurrentBlock == null)
            return string.Empty;

        var result = CurrentBlock.commandEntered(currentCommand);

        if (result.Deltas != null)
        {
            foreach (var delta in result.Deltas)
            {
                if (_variables.ContainsKey(delta.Key))
                {
                    _variables[delta.Key] += delta.Value;

                    var def = _propDefs[delta.Key];
                    _variables[delta.Key] = Math.Clamp(_variables[delta.Key], def.min, def.max);

                    var hudStat = HudStats.FirstOrDefault(h => h.Key == delta.Key);
                    if (hudStat != null)
                    {
                        hudStat.Value = _variables[delta.Key];
                    }

                    if (_variables[delta.Key] == def.min && !string.IsNullOrEmpty(def.onMinBlock))
                    {
                        if (_blockDict.TryGetValue(def.onMinBlock, out Block? minDeathBlock))
                        {
                            CurrentBlock = minDeathBlock;
                            SetActiveNodeByUid(CurrentBlock.g_uid);
                            IsGameOver = true;
                            return $"\n[SYSTEM ALERT] METRIC {def.hudLabel.ToUpper()} CRITICAL. INITIATING SHUTDOWN...\n";
                        }
                    }

                    if (_variables[delta.Key] == def.max && !string.IsNullOrEmpty(def.onMaxBlock))
                    {
                        if (_blockDict.TryGetValue(def.onMaxBlock, out Block? maxDeathBlock))
                        {
                            CurrentBlock = maxDeathBlock;
                            SetActiveNodeByUid(CurrentBlock.g_uid);
                            IsGameOver = true;
                            return $"\n[SYSTEM ALERT] METRIC {def.hudLabel.ToUpper()} CRITICAL. INITIATING SHUTDOWN...\n";
                        }
                    }
                }
            }
        }

        if (_deathCondition != null && _deathCondition.Evaluate(this))
        {
            if (_blockDict.TryGetValue(_deathBlockUid, out Block? deathBlock))
            {
                CurrentBlock = deathBlock;
                SetActiveNodeByUid(CurrentBlock.g_uid);
                IsGameOver = true;
                return "\n[SYSTEM ALERT] CRITICAL METRICS FAILURE. INITIATING SHUTDOWN SEQUENCE...\n";
            }
        }

        if (result.AdvanceToNextBlock)
        {
            if (result.NextBlockUid == "0")
            {
                IsGameOver = true;
                return "\n>>> SYSTEM OVERRIDE SUCCESSFUL. YOU WON. <<<\n";
            }

            if (_blockDict.TryGetValue(result.NextBlockUid, out Block? nextBlock))
            {
                CurrentBlock = nextBlock;
                SetActiveNodeByUid(CurrentBlock.g_uid);
                return $"\n[SYSTEM] Advanced to {CurrentBlock.ui_dashboard_title}\n";
            }
        }

        return string.Empty;
    }

    // private void ClampStats()
    // {
    //     Efficiency = Math.Clamp(Efficiency, _minStats.efficiency, _maxStats.efficiency);
    //     Knowledge = Math.Clamp(Knowledge, _minStats.knowledge, _maxStats.knowledge);
    //     Evidence = Math.Clamp(Evidence, _minStats.evidence, _maxStats.evidence);
    //     Reputation = Math.Clamp(Reputation, _minStats.reputation, _maxStats.reputation);
    // }

    public void setPenalties()
    {
        // get the dict from the block class and modify the state variables
    }

    public void loadStory(string filePath) // filepath is json string but I'm lazy to change its occurances in the file
    {
        Console.WriteLine($"Loading story from: {filePath}");
        try
        {
            IsGameOver = false;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var storyData = System.Text.Json.JsonSerializer.Deserialize<StorySchema>(
                filePath,
                options
            );

            if (storyData == null || storyData.blocks.Count == 0)
                throw new Exception("Story data is empty or invalid.");

            _blockDict.Clear();
            GraphNodes.Clear();
            _propDefs.Clear();
            _variables.Clear();
            HudStats.Clear();

            _deathCondition = storyData.death_condition;
            _deathBlockUid = storyData.death_block_uid;
            Title = storyData.title;

            var visibleStats = new List<HudStat>();

            foreach (var prop in storyData.properties)
            {
                _propDefs[prop.key] = prop;
                _variables[prop.key] = prop.initial;

                if (prop.visibleInHud)
                {
                    visibleStats.Add(
                        new HudStat
                        {
                            Key = prop.key,
                            Label = prop.hudLabel,
                            Value = prop.initial,
                            Order = prop.hudOrder,
                        }
                    );
                }
            }

            foreach (var stat in visibleStats.OrderBy(s => s.Order))
            {
                HudStats.Add(stat);
            }

            foreach (var block in storyData.blocks)
            {
                _blockDict[block.g_uid] = block;
                GraphNodes.Add(CreateGraphNodeFromBlock(block));
            }

            if (_blockDict.TryGetValue(storyData.start_block_uid, out Block? startBlock))
            {
                CurrentBlock = startBlock;
                SetActiveNodeByUid(CurrentBlock.g_uid);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load story: {ex.Message}");
        }
    }

    private void PopulateGraphNodeFromBlock(GraphNode node, Block block)
    {
        node.NodeColor = block.ui_color;
        node.DashboardTitle = block.ui_dashboard_title;
        node.CardTitle = block.ui_card_title;
        node.CardDescription = block.ui_card_description;
    }

    private GraphNode CreateGraphNodeFromBlock(Block block)
    {
        var node = new GraphNode();
        node.Uid = block.g_uid;
        node.NodeColor = block.ui_color;
        node.DashboardTitle = block.ui_dashboard_title;
        node.CardTitle = block.ui_card_title;
        node.CardDescription = block.ui_card_description;
        return node;
    }

    public void SetActiveNodeByUid(string uid)
    {
        foreach (var node in GraphNodes)
        {
            if (node.Uid == uid)
            {
                ActiveNode = node;
                return;
            }
        }
    }

    public string GenerateSaveStateJson()
    {
        var data = new SaveStateData
        {
            CurrentBlockUid = CurrentBlock?.g_uid ?? string.Empty,
            Variables = new Dictionary<string, int>(_variables), // Clone the runtime dictionary
        };

        return System.Text.Json.JsonSerializer.Serialize(
            data,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );
    }

    public void loadStateFromJson(string jsonString)
    {
        Console.WriteLine("Applying saved state overrides...");
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var savedData = System.Text.Json.JsonSerializer.Deserialize<SaveStateData>(
                jsonString,
                options
            );

            if (savedData == null)
                return;

            // 1. Restore the Variables and Update the HUD
            if (savedData.Variables != null)
            {
                foreach (var kvp in savedData.Variables)
                {
                    // Only apply if the property still exists in the current story schema
                    if (_variables.ContainsKey(kvp.Key))
                    {
                        _variables[kvp.Key] = kvp.Value;

                        // Force the UI HUD to update instantly
                        var hudStat = HudStats.FirstOrDefault(h => h.Key == kvp.Key);
                        if (hudStat != null)
                        {
                            hudStat.Value = kvp.Value;
                        }
                    }
                }
            }

            // 2. Restore the Timeline / Current Block
            if (!string.IsNullOrEmpty(savedData.CurrentBlockUid))
            {
                if (_blockDict.TryGetValue(savedData.CurrentBlockUid, out Block? savedBlock))
                {
                    CurrentBlock = savedBlock;
                    SetActiveNodeByUid(CurrentBlock.g_uid);
                    Console.WriteLine(
                        $"State restored! Jumped to: {CurrentBlock.ui_dashboard_title}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"WARNING: Saved block '{savedData.CurrentBlockUid}' does not exist in this story file."
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to apply save state: {ex.Message}");
        }
    }
}
