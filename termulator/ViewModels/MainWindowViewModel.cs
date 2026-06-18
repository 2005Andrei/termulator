using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace termulator.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<TerminalEntry> History { get; } = new();
    public Engine engine = new();

    // graph logic
    public ObservableCollection<GraphNode> GraphNodes { get; } = new();

    [ObservableProperty]
    private GraphNode? _activeNode;

    [ObservableProperty]
    private string _currentCommand = string.Empty;

    [ObservableProperty]
    private bool _dockerLoaded = false;

    [ObservableProperty]
    private int _startupProgress = 0;

    [ObservableProperty]
    private string _startupMessage = "Initializing";

    [ObservableProperty]
    private bool _isCommandRunning = false;

    private CancellationTokenSource? _commandCts;

    // main constructor
    public MainWindowViewModel()
    {
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

    // engine
    public async Task StartEngine()
    {
        var progress = new Progress<(int value, string message)>(p =>
        {
            StartupProgress = p.value;
            StartupMessage = p.message;
        });

        await engine.Initialize(progress);

        await Task.Delay(600);

        DockerLoaded = true;
    }

    public async Task StartGame(string filePath)
    {
        Console.WriteLine("start game");
        loadStory(filePath);
        await StartEngine();
    }

    public void loadStory(string filePath)
    {
        Console.WriteLine("load game state");
    }

    [RelayCommand]
    public async Task Execute()
    {
        Console.WriteLine(
            $"[Execute] IsCommandRunning={IsCommandRunning}, command={CurrentCommand}"
        );
        if (IsCommandRunning)
        {
            CancelRunningCommand();
            return;
        }

        string commandToRun = CurrentCommand?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(commandToRun))
            return;

        if (commandToRun == "clear" || commandToRun == "reset")
        {
            History.Clear();
            CurrentCommand = string.Empty;
            return;
        }

        CurrentCommand = string.Empty;
        IsCommandRunning = true;

        _commandCts = new CancellationTokenSource();

        var entry = new TerminalEntry { Command = commandToRun, Output = "" };
        History.Add(entry);

        var sb = new StringBuilder();
        try
        {
            await foreach (var line in engine.StreamCommand(commandToRun, _commandCts.Token))
            {
                sb.AppendLine(line);
                entry.Output = sb.ToString().TrimEnd();
                await Task.Yield();
            }
        }
        catch (OperationCanceledException)
        {
            entry.Output = (sb.Length > 0 ? sb + "\n" : "") + "^C";
        }
        finally
        {
            _commandCts.Dispose();
            _commandCts = null;
            IsCommandRunning = false;
        }
    }

    [RelayCommand]
    public void CancelRunningCommand()
    {
        Console.WriteLine("yes cancelling");
        _commandCts?.Cancel();
        engine.SendInterrupt();
    }
}
