using System.Linq;
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
            desktop.MainWindow = new StartWindow(() =>
            {
                var mainWindow = new MainWindow() { DataContext = new MainWindowViewModel() };
                mainWindow.Show();
                mainWindow.Focus();

                desktop.MainWindow = mainWindow;
            })
            {
                DataContext = new StartWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
