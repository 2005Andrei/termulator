using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using termulator.ViewModels;

namespace termulator.Views;

public partial class StartWindow : Window
{
    private readonly Action? _mainAction;

    public StartWindow() { }

    public StartWindow(Action mainAction)
    {
        InitializeComponent();
        _mainAction = mainAction;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        LoadAll();
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

            bool dockerInstalled = true;

            if (!dockerInstalled) {
                viewModel.DockerFail = true;
                await Task.Delay(2000);
                Close();
                // close the app automatically
            }

            viewModel.DockerSuccess = true;

            await Task.Delay(3000);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _mainAction?.Invoke();
                Close();
            });
        }

    }
}
