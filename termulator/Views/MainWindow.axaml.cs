using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
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

    private void OpenDecisions_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DecisionsPanel.IsVisible = true;
        DecisionsPanel.Opacity = 1;

        HintPanel.Opacity = 0;
        HintPanel.IsVisible = false;
    }

    private void CloseDecisions_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DecisionsPanel.Opacity = 0;
    }

    private void OpenHint_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HintPanel.IsVisible = true;
        HintPanel.Opacity = 1;

        DecisionsPanel.Opacity = 0;
        DecisionsPanel.IsVisible = false;
    }

    private void CloseHint_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HintPanel.Opacity = 0;
    }

    // graph stuff

    private void CloseNodeDetails_Click(object sender, RoutedEventArgs e)
    {
        CloseNodeDetails();
    }

    // scrollable graph logic
    private ViewModels.GraphNode? _expandedNode = null;

    private void Node_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ViewModels.GraphNode clickedNode)
        {
            if (NodeDetailsCard.Opacity > 0 && _expandedNode == clickedNode)
            {
                CloseNodeDetails();
            }
            else
            {
                UpdateAndShowNodeDetails(clickedNode);
            }
        }
    }

    private void ClearNodeHighlights()
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            foreach (var node in vm.GraphNodes)
            {
                node.StrokeColor = "#555";
            }
        }
    }

    private void UpdateAndShowNodeDetails(ViewModels.GraphNode node)
    {
        _expandedNode = node;

        ClearNodeHighlights();
        node.StrokeColor = "White";

        NodeDetailsCard.Margin = new Avalonia.Thickness(0, 0, 0, 0);

        var nodeColorBrush = Avalonia.Media.SolidColorBrush.Parse(node.NodeColor);

        NodeDetailsIcon.Fill = nodeColorBrush;
        NodeInfoTitle.Text = node.CardTitle;
        NodeInfoTitle.Foreground = nodeColorBrush;
        NodeInfoDescription.Text = node.CardDescription;

        NodeDetailsCard.IsHitTestVisible = true;
        NodeDetailsCard.Opacity = 1;
        NodeDetailsCard.Margin = new Avalonia.Thickness(0, 15, 0, 0);
    }

    private void CloseNodeDetails()
    {
        _expandedNode = null;
        ClearNodeHighlights();

        NodeDetailsCard.IsHitTestVisible = false;
        NodeDetailsCard.Opacity = 0;
        NodeDetailsCard.Margin = new Avalonia.Thickness(0, 0, 0, 0);
    }
}
