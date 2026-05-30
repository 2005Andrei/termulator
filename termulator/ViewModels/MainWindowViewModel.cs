using System;
using System.Collections.ObjectModel;
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

        TerminalEntry currentEntry = new TerminalEntry
        {
            Command = commandToRun,
            Output = await engine.executeCommand(commandToRun),
        };

        History.Add(currentEntry);
    }
}
