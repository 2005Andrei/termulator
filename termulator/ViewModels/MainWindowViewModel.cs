using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace termulator.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<TerminalEntry> History { get; } = new();

    [ObservableProperty]
    private string _currentCommand = string.Empty;


    [RelayCommand]
    public void ExecuteCommand()
    {
        if (CurrentCommand == "clear" || CurrentCommand == "reset") {
            History.Clear();
            CurrentCommand = string.Empty;
            return;
        }

        TerminalEntry currentEntry = new TerminalEntry{
            Command = CurrentCommand,
            Output = "Mock Output"
        };
        History.Add(currentEntry);

        CurrentCommand = string.Empty;
    }
}
