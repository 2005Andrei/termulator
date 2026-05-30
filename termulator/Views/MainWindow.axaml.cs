using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace termulator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);

        Command.Focus();
    }

    private void CloseHint_Click(object? sender, RoutedEventArgs e)
    {
        HintPanel.IsVisible = false;
        OpenHintButton.IsVisible = true;
    }

    private void OpenHint_Click(object? sender, RoutedEventArgs e)
    {
        HintPanel.IsVisible = true;
        OpenHintButton.IsVisible = false;
    }
}
