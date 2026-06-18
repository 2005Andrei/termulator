using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace termulator.ViewModels;

public partial class State : ObservableObject
{
    // list of blocks
    // efficiency, knowledge, evidence, reputation

    // method for adding stats from the computed penalties in each block

    // then probably a method to load stuff from json files

    private Dictionary<string, Block> _blockDict = new();
    public ObservableCollection<GraphNode> GraphNodes { get; } = new();

    private StatModifiers _minStats = new();
    private StatModifiers _maxStats = new();
    private ConditionNode? _deathCondition;
    private string _deathBlockUid = string.Empty;

    [ObservableProperty]
    public Block? _currentBlock;

    [ObservableProperty]
    private GraphNode? _activeNode;

    [ObservableProperty]
    private int _efficiency = 0;

    [ObservableProperty]
    private int _knowledge = 0;

    [ObservableProperty]
    private int _evidence = 0;

    [ObservableProperty]
    private int _reputation = 0;

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

    public string assessCommand(string currentCommand)
    {
        // go to current block
        // get the penalties for the current block
        // add the penalties with the current metrics

        var result = CurrentBlock.commandEntered(currentCommand);

        Efficiency += result.Deltas.efficiency;
        Knowledge += result.Deltas.knowledge;
        Evidence += result.Deltas.evidence;
        Reputation += result.Deltas.reputation;

        ClampStats();

        if (_deathCondition != null && _deathCondition.Evaluate(this))
        {
            if (_blockDict.TryGetValue(_deathBlockUid, out Block? deathBlock))
            {
                CurrentBlock = deathBlock;
                SetActiveNodeByUid(CurrentBlock.g_uid);
                IsGameOver = true;
                return "\n[SYSTEM ALERT] CRITICAL METRICS FAILURE. INITIATING SHUTDOWN SEQUENCE...\n In other words: YOU LOST";
            }
        }

        if (result.AdvanceToNextBlock)
        {
            if (result.NextBlockUid == "0")
            {
                return "\n>>> SYSTEM OVERRIDE SUCCESSFUL. YOU WON. <<<\n";
            }

            if (_blockDict.TryGetValue(result.NextBlockUid, out Block? nextBlock))
            {
                CurrentBlock = nextBlock;
                SetActiveNodeByUid(CurrentBlock.g_uid);
                return $"\n[SYSTEM] Advanced to {CurrentBlock.ui_dashboard_title}\n";
            }
            else // shouldn't happen
            {
                return $"\n[ERROR] CRITICAL FAULT: Block {result.NextBlockUid} not found.\n";
            }
        }

        return string.Empty;
    }

    private void ClampStats()
    {
        Efficiency = Math.Clamp(Efficiency, _minStats.efficiency, _maxStats.efficiency);
        Knowledge = Math.Clamp(Knowledge, _minStats.knowledge, _maxStats.knowledge);
        Evidence = Math.Clamp(Evidence, _minStats.evidence, _maxStats.evidence);
        Reputation = Math.Clamp(Reputation, _minStats.reputation, _maxStats.reputation);
    }

    public void setPenalties()
    {
        // get the dict from the block class and modify the state variables
    }

    public void loadStory(string filePath)
    {
        Console.WriteLine($"Loading story from: {filePath}");

        try
        {
            string jsonString = System.IO.File.ReadAllText(filePath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var storyData = System.Text.Json.JsonSerializer.Deserialize<StorySchema>(
                jsonString,
                options
            );

            if (storyData == null || storyData.blocks.Count == 0)
                throw new Exception("Story data is empty or invalid.");

            _blockDict.Clear();
            GraphNodes.Clear();

            _minStats = storyData.min_stats ?? new StatModifiers();
            _maxStats = storyData.max_stats ?? new StatModifiers();
            _deathCondition = storyData.death_condition;
            _deathBlockUid = storyData.death_block_uid;

            Efficiency = storyData.initial_stats?.efficiency ?? 50;
            Knowledge = storyData.initial_stats?.knowledge ?? 10;
            Evidence = storyData.initial_stats?.evidence ?? 0;
            Reputation = storyData.initial_stats?.reputation ?? 0;

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
            else
            {
                Console.WriteLine("cirticial error");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"fah {ex.Message}");
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
}
