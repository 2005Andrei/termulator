using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace termulator.ViewModels;

public partial class State : ObservableObject
{
    public Collection<Block> blocks { get; } = new();

    // list of blocks
    // efficiency, knowledge, evidence, reputation

    // method for adding stats from the computed penalties in each block

    // then probably a method to load stuff from json files

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
        // graph nodes should also have conditions in them
        GraphNodes.Add(
            new GraphNode
            {
                NodeColor = "Cyan",
                DashboardTitle = "BLOCK 1",
                CardTitle = "BLOCK 1 // SECURE",
                CardDescription =
                    "System execution normal. Command processed successfully without anomalies.",
            }
        );

        GraphNodes.Add(
            new GraphNode
            {
                NodeColor = "Red",
                DashboardTitle = "BLOCK 2",
                CardTitle = "BLOCK 2 // HALTED",
                CardDescription =
                    "Manual override required. Check HINTS for further instructions on how to bypass the security wall.",
            }
        );

        GraphNodes.Add(
            new GraphNode
            {
                NodeColor = "Purple",
                DashboardTitle = "BLOCK 3",
                CardTitle = "BLOCK 3 // ENCRYPTED",
                CardDescription =
                    "Data stream is heavily encrypted. Awaiting decryption key from terminal input.",
            }
        );

        GraphNodes.Add(new GraphNode());
        GraphNodes.Add(new GraphNode());

        SetActiveNode();

        CurrentBlock = new Block
        {
            g_uid = "block_1",
            decisions = new List<Decision>
            {
                new Decision
                {
                    command_sequence = new List<string> { "override sys" },
                    next_block_uid = "block_2",
                },
            },
        };
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

    public void assessCommand(string currentCommand)
    {
        // go to current block
        // get the penalties for the current block
        // add the penalties with the current metrics

        var result = CurrentBlock?.commandEntered(currentCommand);

        Efficiency += result.EfficiencyDelta;
        Knowledge += result.KnowledgeDelta;

        Console.WriteLine($"Stats updated: Efficiency={Efficiency}, Knowledge={Knowledge}");

        if (result.AdvanceToNextBlock)
        {
            Console.WriteLine($"Decision correct! Moving to {result.NextBlockUid}");

            SetActiveNode();
        }
    }

    public void setPenalties()
    {
        // get the dict from the block class and modify the state variables
    }
}
