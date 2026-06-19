using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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

    private string _rawStoryJson = string.Empty;

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
        string jsonContent = string.Empty;
        try
        {
            using (ZipArchive archive = ZipFile.OpenRead(filePath))
            {
                ZipArchiveEntry? storyEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.Equals("story.json", StringComparison.OrdinalIgnoreCase)
                );
                if (storyEntry != null)
                {
                    using (StreamReader reader = new StreamReader(storyEntry.Open()))
                    {
                        jsonContent = await reader.ReadToEndAsync();
                    }
                }
                else
                {
                    Console.WriteLine(
                        "CRITICAL ERROR: story.json not found in the root of the zip archive."
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading zip archive: {ex.Message}");
        }

        if (!string.IsNullOrWhiteSpace(jsonContent))
        {
            _rawStoryJson = jsonContent;
            state.loadStory(jsonContent);
        }

        // loadStory(filePath);
        await StartEngine();
        if (!string.IsNullOrWhiteSpace(stateFilePath))
        {
            loadState(stateFilePath);
        }
    }

    // public void loadStory(string filePath)
    // {
    //     Console.WriteLine("loading story in mainwindowviewmodel");
    //     state.loadStory(filePath);
    // }

    public void loadState(string filePath)
    {
        Console.WriteLine($"Loading saved state from archive: {filePath}");
        string jsonContent = string.Empty;

        try
        {
            using (var archive = System.IO.Compression.ZipFile.OpenRead(filePath))
            {
                var stateEntry = archive.GetEntry("state.json");
                if (stateEntry != null)
                {
                    using (var reader = new System.IO.StreamReader(stateEntry.Open()))
                    {
                        jsonContent = reader.ReadToEnd();
                    }
                }
                else
                {
                    Console.WriteLine("CRITICAL ERROR: state.json not found in the save archive.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading state archive: {ex.Message}");
        }

        if (!string.IsNullOrWhiteSpace(jsonContent))
        {
            state.loadStateFromJson(jsonContent);
        }
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

        string stateFeedback = state.assessCommand(commandToRun);

        if (!string.IsNullOrWhiteSpace(stateFeedback))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine(stateFeedback);

            entry.Output = sb.ToString().TrimEnd();
        }

        if (state.IsGameOver)
        {
            InitiateShutdownSequence();
        }
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

            var startWindow = new StartWindow(
                async (filePath, stateFilePath) =>
                {
                    var newViewModel = new MainWindowViewModel();
                    var newMainWindow = new MainWindow { DataContext = newViewModel };

                    desktopApp.MainWindow = newMainWindow;
                    newMainWindow.Show();

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

    public async Task SaveState()
    {
        Console.WriteLine("Save state clicked");

        if (
            Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktopApp
            && desktopApp.MainWindow != null
        )
        {
            var storageProvider = desktopApp.MainWindow.StorageProvider;

            var file = await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save Game State",
                    DefaultExtension = ".zip",
                    SuggestedFileName = "savegame.zip",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Save Archive") { Patterns = new[] { "*.zip" } },
                    },
                }
            );

            if (file != null)
            {
                string jsonState = state.GenerateSaveStateJson();

                using (var stream = await file.OpenWriteAsync())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                    {
                        var entry = archive.CreateEntry("state.json");
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            await writer.WriteAsync(jsonState);
                        }
                    }
                }

                Console.WriteLine("State successfully packaged and saved!");
            }
        }
    }

    public void Restart()
    {
        Console.WriteLine("Restart clicked - Soft Rebooting System...");

        // 1. Clear the terminal history
        History.Clear();
        CurrentCommand = string.Empty;

        // 2. Reload the base story from our cached JSON string
        // This instantly resets the HUD, active nodes, and variables to their initial state!
        if (!string.IsNullOrWhiteSpace(_rawStoryJson))
        {
            state.loadStory(_rawStoryJson);
        }

        // 3. Unlock the terminal (in case they were previously dead or won)
        DockerLoaded = true;

        // 4. Print a cool system message so they know it worked
        History.Add(
            new TerminalEntry
            {
                Command = "sys-alert",
                Output =
                    "\n[SYSTEM] Simulation restarted. All metrics reset to initial parameters.\n",
            }
        );
    }

    public void ExitAppCommand()
    {
        Environment.Exit(0);
        // if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        // {
        //     desktopApp.Shutdown();
        // }
    }

    private async void InitiateShutdownSequence()
    {
        Console.WriteLine("Initiating shutdown sequence...");

        DockerLoaded = false;
        _commandCts?.Cancel();
        engine.SendInterrupt();

        await Task.Delay(50000);
        Console.WriteLine("Goodbye.");

        Environment.Exit(0);
    }
}
