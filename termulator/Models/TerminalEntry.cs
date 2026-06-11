using CommunityToolkit.Mvvm.ComponentModel;

namespace termulator.ViewModels;

public partial class TerminalEntry : ObservableObject
{
    public string? Command { get; set; }

    [ObservableProperty]
    public string? _output = string.Empty;
}
