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

        // state.
    }

    [RelayCommand]
    public void CancelRunningCommand()
    {
        Console.WriteLine("yes cancelling");
        _commandCts?.Cancel();
        engine.SendInterrupt();
    }

    // menu stuff

    public void LoadState()
    {
        ReturnToStartWindow();
    }

    public void OpenStory()
    {
        ReturnToStartWindow();
    }

    // Centralized method to handle the window swap
    private void ReturnToStartWindow()
    {
        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktopApp
        )
        {
            var currentTerminalWindow = desktopApp.MainWindow;

            // 1. Create a new StartWindow, passing your StartGame logic back into it
            var startWindow = new StartWindow(this.StartGame)
            {
                DataContext = new StartWindowViewModel(),
                SkipIntro = true, // Fast-forward straight to the file picker
            };

            // 2. Set the new window as the application's Main Window
            desktopApp.MainWindow = startWindow;

            // 3. Show the new StartWindow
            startWindow.Show();

            // 4. Close the old Terminal Window
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
