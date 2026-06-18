using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using termulator.ViewModels;

namespace termulator.Views;

public partial class StartWindow : Window
{
    private readonly Func<string, string?, Task>? _mainAction;

    public bool SkipIntro { get; set; } = true; // REMEMBER TO CHECK BACK TO FALSE

    public StartWindow() { }

    public StartWindow(Func<string, string?, Task>? mainAction)
    {
        InitializeComponent();
        _mainAction = mainAction;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        // LoadAll();
        if (SkipIntro)
        {
            if (DataContext is StartWindowViewModel viewModel)
            {
                viewModel.IsIntroVisible = false;
                viewModel.LoadZip = true;
            }
        }
        else
        {
            LoadAll();
        }
    }

    private async void LoadAll()
    {
        await Task.Delay(2500);

        if (DataContext is StartWindowViewModel viewModel)
        {
            // insane way to do fade ins what the hell
            viewModel.TextOpacity = 0.0;
            await Task.Delay(2500);

            viewModel.Title = "This is a cute app to learn about using linux.";
            viewModel.TextOpacity = 1.0;

            await Task.Delay(2500);
            viewModel.TextOpacity = 0.0;

            await Task.Delay(2500);
            viewModel.Title = "You will get comfortable with the command line :)";

            viewModel.TextOpacity = 1.0;
            await Task.Delay(2500);

            // move to docker phase
            viewModel.IsIntroVisible = false;
            viewModel.CheckDocker = true;

            await Task.Delay(2000);

            // check for docker on the system here: todo

            viewModel.CheckDocker = false;

            bool dockerInstalled = await CheckForDockerAsync();

            if (!dockerInstalled)
            {
                viewModel.DockerFail = true;
                await Task.Delay(2000);
                Close();
                return;
            }

            viewModel.DockerSuccess = true;

            await Task.Delay(3000);

            viewModel.DockerSuccess = false;

            await Task.Delay(200);

            // start the engine here

            viewModel.LoadZip = true;

            viewModel.LoadEngine = true;
        }
    }

    // file functionality
    private async void fileBtn(object? sender, RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null)
            return;

        var files = await storageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Zips") { Patterns = new[] { "*.txt" } }, // txt atm
                    FilePickerFileTypes.All,
                },
            }
        );
        if (files.Count >= 1)
        {
            string filePath = files[0].Path.LocalPath;

            if (DataContext is StartWindowViewModel vm)
            {
                vm.AddFile(filePath);
            }
        }
    }

    private async void submitBtn(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StartWindowViewModel viewModel)
        {
            if (viewModel.HasFile)
            {
                string filePath = viewModel.FilePath;

                // Determine if we should send a state file based on the UI mode
                // (Assuming Index 1 is "Load State" and the ViewModel has the state file)
                string? statePath = null;
                if (
                    BootModeComboBox != null
                    && BootModeComboBox.SelectedIndex == 1
                    && viewModel.HasStateFile
                )
                {
                    statePath = viewModel.StateFilePath;
                }

                // Invoke with both arguments!
                await _mainAction!.Invoke(filePath, statePath);
                Close();
            }
        }
    }

    private void BootMode_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (StateFilePanel != null && BootModeComboBox != null)
        {
            StateFilePanel.IsVisible = BootModeComboBox.SelectedIndex == 1;
        }
    }

    // Handles the second file browser button
    private async void stateFileBtn(object? sender, RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null)
            return;

        var files = await storageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select State File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    FilePickerFileTypes.All,
                },
            }
        );

        if (files.Count >= 1)
        {
            string filePath = files[0].Path.LocalPath;

            if (DataContext is StartWindowViewModel vm)
            {
                vm.AddStateFile(filePath);
            }
        }
    }

    // private async void submitBtn(object? sender, RoutedEventArgs e)
    // {
    //     if (DataContext is StartWindowViewModel viewModel)
    //     {
    //         if (viewModel.HasFile)
    //         {
    //             string filePath = viewModel.FilePath;
    //             await _mainAction.Invoke(filePath);
    //             Close();
    //
    //             // await Dispatcher.UIThread.InvokeAsync(async () =>
    //             // {
    //             //     await _mainAction.Invoke(filePath);
    //             //     Close();
    //             // });
    //         }
    //     }
    // }

    private async Task<bool> CheckForDockerAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = processInfo };

            process.Start();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var output = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync(cts.Token);

            return process.ExitCode == 0
                && output.Contains("version", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
            when (ex is OperationCanceledException || ex is System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
