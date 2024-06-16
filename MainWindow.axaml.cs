using System.Collections.Generic;
using Avalonia.Controls;

namespace avalonia_contextmenu_xaml;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        DataContext = new MainWindowViewModel();
        InitializeComponent();
    }

    public void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        var cm = new ContextMenu();
        var ri = e.Row.DataContext as MainWindowViewModel.RowItem;
        cm.Items.Add(new MenuItem() { Header = "thing " + ri.ValueCol1 });
        cm.Items.Add(new MenuItem() { Header = "hey" });
        e.Row.ContextMenu = cm;
    }

    public void OnDataGridWTemplateAttemptLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        var ri = e.Row.DataContext as MainWindowViewModel.RowItem;
        e.Row.ContextMenu = new ContextMenuTemplate("thing " + ri.ValueCol1);
    }
}