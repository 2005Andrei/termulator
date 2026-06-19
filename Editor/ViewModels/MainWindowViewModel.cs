using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editor.Models;

namespace Editor.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public StorySchema CurrentStory { get; set; } = new();

    public ObservableCollection<TreeNode> TreeItems { get; } = new();

    [ObservableProperty]
    private object? _selectedContent;

    private TreeNode? _selectedNode;
    public TreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
                SelectedContent = _selectedNode?.DataContext;
        }
    }

    private TreeNode? _propsFolder;
    private TreeNode? _blocksFolder;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public MainWindowViewModel()
    {
        InitializeDummyData();
        BuildTree();
    }

    private bool ValidateStory(out string errorMsg)
    {
        errorMsg = string.Empty;

        var validUids = CurrentStory
            .blocks.Select(b => b.g_uid)
            .Where(uid => !string.IsNullOrWhiteSpace(uid))
            .ToHashSet();

        if (
            !string.IsNullOrWhiteSpace(CurrentStory.start_block_uid)
            && !validUids.Contains(CurrentStory.start_block_uid)
        )
        {
            errorMsg = $"Global Start Block UID '{CurrentStory.start_block_uid}' does not exist.";
            return false;
        }

        if (
            !string.IsNullOrWhiteSpace(CurrentStory.death_block_uid)
            && !validUids.Contains(CurrentStory.death_block_uid)
        )
        {
            errorMsg = $"Global Death Block UID '{CurrentStory.death_block_uid}' does not exist.";
            return false;
        }

        bool hasVictoryNode = false;

        foreach (var block in CurrentStory.blocks)
        {
            foreach (var dec in block.decisions)
            {
                if (string.IsNullOrWhiteSpace(dec.next_block_uid))
                {
                    errorMsg =
                        $"Decision '{dec.description}' in Block '{block.g_uid}' has an empty Target Block UID.";
                    return false;
                }

                if (dec.next_block_uid == "0")
                {
                    hasVictoryNode = true;
                }
                else if (!validUids.Contains(dec.next_block_uid))
                {
                    errorMsg =
                        $"Broken Link: Decision '{dec.description}' in Block '{block.g_uid}' points to a non-existent block: '{dec.next_block_uid}'.";
                    return false;
                }
            }
        }

        if (!hasVictoryNode)
        {
            errorMsg =
                "Unwinnable Game: No victory condition found. At least one decision must have a Target Next Block UID of '0'.";
            return false;
        }

        return true;
    }

    private void InitializeDummyData()
    {
        CurrentStory.title = "New Story";
        CurrentStory.start_block_uid = "block_1";
        CurrentStory.death_block_uid = "block_death";

        CurrentStory.properties.Add(
            new StatePropertyDefinition
            {
                key = "efficiency",
                hudLabel = "Efficiency",
                min = 0,
                max = 100,
                initial = 50,
                visibleInHud = true,
                hudOrder = 1,
                onMinBlock = "block_death",
            }
        );
        CurrentStory.properties.Add(
            new StatePropertyDefinition
            {
                key = "knowledge",
                hudLabel = "Knowledge",
                min = 0,
                max = 100,
                initial = 10,
                visibleInHud = true,
                hudOrder = 2,
            }
        );

        var deathCond = new ConditionNode { type = "OR" };
        deathCond.children.Add(
            new ConditionNode
            {
                type = "COMPARISON",
                property = "efficiency",
                op = "<=",
                value = 15,
            }
        );
        CurrentStory.death_condition = deathCond;

        var blockStart = new Block
        {
            g_uid = "block_1",
            ui_dashboard_title = "STAGE 01",
            ui_card_title = "INITIAL ACCESS",
            ui_card_description = "Connection handshake established.",
            ui_color = "Cyan",
            instructions =
                "INITIALIZING SYSTEM PROXIES...\n\nType 'override sys' to access the mainframe, or 'abort' to disconnect.",
            hint = "Choose your entry vector carefully.",
            wrong_command_penalties = new Dictionary<string, int> { ["efficiency"] = -10 },
        };
        blockStart.decisions.Add(
            new Decision
            {
                description = "Proceed to mainframe",
                command_sequence = new List<string> { "override", "sys" },
                next_block_uid = "block_success",
                rewards = new Dictionary<string, int> { ["knowledge"] = 10 },
            }
        );
        blockStart.decisions.Add(
            new Decision
            {
                description = "Abort connection (Death)",
                command_sequence = new List<string> { "abort" },
                next_block_uid = "block_death",
            }
        );

        var blockDeath = new Block
        {
            g_uid = "block_death",
            ui_dashboard_title = "SYSTEM LOCKOUT",
            ui_card_title = "CONNECTION TERMINATED",
            ui_card_description = "Your signature was detected.",
            ui_color = "Red",
            instructions =
                "The target network has severed your connection. Security protocols have traced your origin.\n\nGAME OVER.",
        };
        var blockSuccess = new Block
        {
            g_uid = "block_success",
            ui_dashboard_title = "MAINFRAME COMPROMISED",
            ui_card_title = "ACCESS GRANTED",
            ui_card_description = "You have root access.",
            ui_color = "Green",
            instructions =
                "You are in. Extract the payload and clean your tracks.\n\nType 'extract payload' to win.",
        };
        blockSuccess.decisions.Add(
            new Decision
            {
                description = "Extract and Win",
                command_sequence = new List<string> { "extract", "payload" },
                next_block_uid = "0",
                rewards = new Dictionary<string, int> { ["reputation"] = 50 },
            }
        );

        CurrentStory.blocks.Add(blockStart);
        CurrentStory.blocks.Add(blockDeath);
        CurrentStory.blocks.Add(blockSuccess);
    }

    private void BuildTree()
    {
        TreeItems.Clear();

        var rootNode = new TreeNode { Name = "Story Metadata", DataContext = CurrentStory };

        if (CurrentStory.death_condition != null)
        {
            var globalDeathNode = BuildConditionTree(CurrentStory.death_condition);
            globalDeathNode.Name = "[Global Death Condition]";
            rootNode.Children.Add(globalDeathNode);
        }

        _propsFolder = new TreeNode { Name = "Properties" };
        foreach (var prop in CurrentStory.properties)
            _propsFolder.Children.Add(MakePropertyNode(prop));

        _blocksFolder = new TreeNode { Name = "Blocks" };
        foreach (var block in CurrentStory.blocks)
            _blocksFolder.Children.Add(MakeBlockNode(block));

        TreeItems.Add(rootNode);
        TreeItems.Add(_propsFolder);
        TreeItems.Add(_blocksFolder);
    }

    private static TreeNode MakePropertyNode(StatePropertyDefinition prop) =>
        new TreeNode { Name = prop.key, DataContext = prop };

    private TreeNode MakeBlockNode(Block block)
    {
        var bNode = new TreeNode { Name = block.g_uid, DataContext = block };
        foreach (var dec in block.decisions)
        {
            var dNode = new TreeNode { Name = dec.description, DataContext = dec };
            if (dec.condition != null)
                dNode.Children.Add(BuildConditionTree(dec.condition));
            bNode.Children.Add(dNode);
        }
        return bNode;
    }

    private TreeNode BuildConditionTree(ConditionNode cond)
    {
        var label =
            cond.type == "COMPARISON"
                ? $"[COMPARISON] {cond.property} {cond.op} {cond.value}"
                : $"[{cond.type}]";

        var node = new TreeNode { Name = label, DataContext = cond };
        foreach (var child in cond.children)
            node.Children.Add(BuildConditionTree(child));
        return node;
    }

    [RelayCommand]
    public void AddBlock()
    {
        int idx = CurrentStory.blocks.Count + 1;
        var block = new Block
        {
            g_uid = $"block_{idx}",
            ui_dashboard_title = $"STAGE {idx:D2}",
            ui_card_title = "New Block",
            ui_card_description = "Description goes here.",
            ui_color = "White",
            instructions = "Narrative text goes here.",
            hint = "Hint goes here.",
        };

        CurrentStory.blocks.Add(block);

        var bNode = MakeBlockNode(block);
        _blocksFolder?.Children.Add(bNode);

        SelectedNode = bNode;
        SelectedContent = block;
    }

    [RelayCommand]
    public void AddProperty()
    {
        int idx = CurrentStory.properties.Count + 1;
        var prop = new StatePropertyDefinition
        {
            key = $"new.property{idx}",
            hudLabel = $"Stat {idx}",
            hudOrder = idx,
        };

        CurrentStory.properties.Add(prop);

        var pNode = MakePropertyNode(prop);
        _propsFolder?.Children.Add(pNode);

        SelectedNode = pNode;
        SelectedContent = prop;
    }

    [RelayCommand]
    public void AddDecision()
    {
        Block? targetBlock = null;
        TreeNode? targetBlockNode = null;

        if (SelectedContent is Block b)
        {
            targetBlock = b;
            targetBlockNode = SelectedNode;
        }
        else if (SelectedContent is Decision || SelectedContent is ConditionNode)
        {
            targetBlockNode = FindParentBlockNode(SelectedNode);
            targetBlock = targetBlockNode?.DataContext as Block;
        }

        if (targetBlock == null || targetBlockNode == null)
            return;

        var dec = new Decision
        {
            description = "New Decision",
            next_block_uid = string.Empty,
            command_sequence = new List<string> { "command" },
        };
        targetBlock.decisions.Add(dec);

        var dNode = new TreeNode { Name = dec.description, DataContext = dec };
        targetBlockNode.Children.Add(dNode);

        SelectedNode = dNode;
        SelectedContent = dec;
    }

    [RelayCommand]
    public void AddConditionNode()
    {
        if (SelectedContent is Decision dec)
        {
            if (dec.condition == null)
            {
                dec.condition = new ConditionNode { type = "COMPARISON" };
                var cNode = new TreeNode { Name = "[COMPARISON]", DataContext = dec.condition };
                SelectedNode?.Children.Add(cNode);
                SelectedNode = cNode;
                SelectedContent = dec.condition;
            }
        }
        else if (SelectedContent is ConditionNode cond && (cond.type == "AND" || cond.type == "OR"))
        {
            var newChild = new ConditionNode { type = "COMPARISON" };
            cond.children.Add(newChild);
            var cNode = new TreeNode { Name = "[COMPARISON]", DataContext = newChild };
            SelectedNode?.Children.Add(cNode);
            SelectedNode = cNode;
            SelectedContent = newChild;
        }
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        if (SelectedNode == null)
            return;

        if (SelectedContent is Block block)
        {
            CurrentStory.blocks.Remove(block);
            _blocksFolder?.Children.Remove(SelectedNode);
        }
        else if (SelectedContent is StatePropertyDefinition prop)
        {
            CurrentStory.properties.Remove(prop);
            _propsFolder?.Children.Remove(SelectedNode);
        }
        else if (SelectedContent is Decision dec)
        {
            var ownerBlock = CurrentStory.blocks.FirstOrDefault(b => b.decisions.Contains(dec));
            ownerBlock?.decisions.Remove(dec);
            FindParentBlockNode(SelectedNode)?.Children.Remove(SelectedNode);
        }
        else if (SelectedContent is ConditionNode cond)
        {
            var parentNode = FindParentTreeNode(TreeItems, SelectedNode);
            if (parentNode != null)
            {
                parentNode.Children.Remove(SelectedNode);

                if (parentNode.DataContext is StorySchema story)
                {
                    story.death_condition = null;
                }
                else if (parentNode.DataContext is Decision decision)
                {
                    decision.condition = null;
                }
                else if (parentNode.DataContext is ConditionNode parentCond)
                {
                    parentCond.children.Remove(cond);
                }
            }
            else if (CurrentStory.death_condition == cond)
            {
                CurrentStory.death_condition = null;
                var root = TreeItems.FirstOrDefault(n => n.Name == "Story Metadata");
                root?.Children.Remove(SelectedNode);
            }
        }

        SelectedNode = null;
        SelectedContent = null;
    }

    private TreeNode? FindParentTreeNode(IEnumerable<TreeNode> nodes, TreeNode target)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Contains(target))
                return node;

            var found = FindParentTreeNode(node.Children, target);
            if (found != null)
                return found;
        }
        return null;
    }

    [RelayCommand]
    public void AddGlobalDeathCondition()
    {
        if (CurrentStory.death_condition == null)
        {
            CurrentStory.death_condition = new ConditionNode { type = "OR" };

            var rootNode = TreeItems.FirstOrDefault(n => n.Name == "Story Metadata");
            if (rootNode != null)
            {
                var condNode = BuildConditionTree(CurrentStory.death_condition);
                condNode.Name = "[Global Death Condition]";
                rootNode.Children.Add(condNode);

                SelectedNode = condNode;
                SelectedContent = CurrentStory.death_condition;
            }
        }
    }

    [RelayCommand]
    public async Task SaveStoryToZip()
    {
        ErrorMessage = string.Empty;

        if (!ValidateStory(out string error))
        {
            ErrorMessage = $"Validation Failed:\n{error}";
            return;
        }

        if (
            Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow == null
        )
            return;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export Story Package",
                DefaultExtension = ".zip",
                SuggestedFileName = $"{CurrentStory.title.Replace(" ", "_")}.zip",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Story Archive") { Patterns = new[] { "*.zip" } },
                },
            }
        );

        if (file == null)
            return;

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(CurrentStory, options);

        using var stream = await file.OpenWriteAsync();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        var entry = archive.CreateEntry("story.json");
        using var es = entry.Open();
        using var writer = new StreamWriter(es);
        await writer.WriteAsync(json);
    }

    private TreeNode? FindParentBlockNode(TreeNode? child)
    {
        if (child == null || _blocksFolder == null)
            return null;
        foreach (var blockNode in _blocksFolder.Children)
            if (ContainsNode(blockNode, child))
                return blockNode;
        return null;
    }

    private static bool ContainsNode(TreeNode parent, TreeNode target)
    {
        foreach (var c in parent.Children)
            if (c == target || ContainsNode(c, target))
                return true;
        return false;
    }
}
