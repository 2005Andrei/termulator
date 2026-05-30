using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace termulator.ViewModels;

public partial class StartWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Welcome to Termulator!";

    [ObservableProperty]
    private double _textOpacity = 1.0;

    // state stuff from here
    [ObservableProperty]
    private bool _isIntroVisible = true;

    [ObservableProperty]
    private bool _checkDocker = false;

    [ObservableProperty]
    private bool _dockerFail = false;

    [ObservableProperty]
    private bool _dockerSuccess = false;

    [ObservableProperty]
    private bool _loadZip = false;

    [ObservableProperty]
    private bool _loadEngine = false;

    // to here

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _hasFile;

    public void AddFile(string filePath)
    {
        HasFile = true;
        FilePath = filePath;
    }
}
