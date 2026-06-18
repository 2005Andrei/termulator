using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using termulator.ViewModels;
using termulator.Views;

namespace termulator;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new StartWindow(
                (filePath, stateFilePath) =>
                {
                    var mainWindowViewModel = new MainWindowViewModel();
                    var mainWindow = new MainWindow() { DataContext = mainWindowViewModel };

                    mainWindow.Show();
                    desktop.MainWindow = mainWindow;

                    _ = mainWindowViewModel.StartGame(filePath, stateFilePath);

                    bool isShuttingDown = false;
                    mainWindow.Closing += async (sender, e) =>
                    {
                        if (!isShuttingDown)
                        {
                            e.Cancel = true;
                            isShuttingDown = true;
                            await mainWindowViewModel.engine.DisposeAsync();
                            mainWindow.Close();
                        }
                    };

                    return Task.CompletedTask;
                }
            )
            {
                DataContext = new StartWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
