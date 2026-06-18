using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace termulator.ViewModels;

public partial class GraphNode : ObservableObject
{
    public string Uid { get; set; } = string.Empty;

    [ObservableProperty]
    private string _nodeColor = "Gray";

    [ObservableProperty]
    private string _strokeColor = "#555";

    [ObservableProperty]
    private string _dashboardTitle = "UNKNOWN";

    [ObservableProperty]
    private string _cardTitle = "BLOCK PENDING";

    [ObservableProperty]
    private string _cardDescription =
        "This block has not been reached yet. Continue execution to unlock.";
}
