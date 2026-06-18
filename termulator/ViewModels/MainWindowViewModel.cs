using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using termulator.Views;

namespace termulator.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<TerminalEntry> History { get; } = new();
    public Engine engine = new();
    public State state { get; } = new();

    // graph logic
    public ObservableCollection<GraphNode> GraphNodes { get; } = new();

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

    // // main constructor
    // public MainWindowViewModel()
    // {
    // }

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

    public async Task StartGame(string filePath, string? stateFilePath = null)
    {
        Console.WriteLine("start game");
        loadStory(filePath);
        await StartEngine();
        if (!string.IsNullOrWhiteSpace(stateFilePath))
        {
            loadState(stateFilePath);
        }
    }

    public void loadStory(string filePath)
    {
        state.loadStory(filePath);
        Console.WriteLine("loading story in mainwindowviewmodel");
    }

    public void loadState(string filePath)
    {
        Console.WriteLine("load story");
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

        state.assessCommand(commandToRun);
    }

    [RelayCommand]
    public void CancelRunningCommand()
    {
        Console.WriteLine("yes cancelling");
        _commandCts?.Cancel();
        engine.SendInterrupt();
    }

    // menu stuff
    public async Task LoadState()
    {
        await ReturnToStartWindowAsync();
    }

    public async Task OpenStory()
    {
        await ReturnToStartWindowAsync();
    }

    private async Task ReturnToStartWindowAsync()
    {
        await engine.ShutOffAsync();

        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktopApp
        )
        {
            var currentTerminalWindow = desktopApp.MainWindow;

            // Update this delegate signature as well
            var startWindow = new StartWindow(
                async (filePath, stateFilePath) =>
                {
                    var newViewModel = new MainWindowViewModel();
                    var newMainWindow = new MainWindow { DataContext = newViewModel };

                    desktopApp.MainWindow = newMainWindow;
                    newMainWindow.Show();

                    // Pass both parameters
                    await newViewModel.StartGame(filePath, stateFilePath);
                }
            )
            {
                DataContext = new StartWindowViewModel(),
                SkipIntro = true,
            };

            desktopApp.MainWindow = startWindow;
            startWindow.Show();
            currentTerminalWindow?.Close();
        }
    }

    public void SaveState()
    {
        Console.WriteLine("Save state clicked");
    }

    public void Restart()
    {
        Console.WriteLine("Restart clicked");
    }

    public void ExitAppCommand()
    {
        Environment.Exit(0);
        // if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        // {
        //     desktopApp.Shutdown();
        // }
    }
}
