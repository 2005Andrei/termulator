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
