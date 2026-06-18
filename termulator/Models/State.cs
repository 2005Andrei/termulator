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

    public Block? CurrentBlock { get; set; }

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
            else
            {
                return $"\n[ERROR] CRITICAL FAULT: Block {result.NextBlockUid} not found.\n";
            }
        }

        return string.Empty;
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
