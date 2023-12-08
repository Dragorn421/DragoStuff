using System;
using System.Diagnostics;
using Avalonia.Controls;

namespace Z64Utils_recreate_avalonia_ui;

public partial class ObjectAnalyzerWindow : Window
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObjectAnalyzerWindowViewModel ViewModel;

    public ObjectAnalyzerWindow()
    {
        ViewModel = new ObjectAnalyzerWindowViewModel()
        {
            OpenDListViewer = OpenDListViewer,
        };
        DataContext = ViewModel;
        InitializeComponent();
    }

    private DListViewerWindowViewModel OpenDListViewer()
    {
        var win = new DListViewerWindow();
        win.Show();
        return win.ViewModel;
    }

    public void OnObjectHolderEntriesDataGridSelectionChanged(object? sender, SelectionChangedEventArgs ev)
    {
        var selectedItem = ObjectHolderEntriesDataGrid.SelectedItem;
        if (selectedItem == null)
            return;
        Debug.Assert(selectedItem is ObjectAnalyzerWindowViewModel.ObjectHolderEntry);
        var ohe = (ObjectAnalyzerWindowViewModel.ObjectHolderEntry)selectedItem;
        var t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        ViewModel.OnObjectHolderEntrySelected(ohe);
        var t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        Logger.Trace("ViewModel.OnObjectHolderEntrySelected(ohe); t2-t1={0}ms", t2-t1);
    }

    public void OnObjectHolderEntriesDataGridLoadingRow(object? sender, DataGridRowEventArgs ev)
    {
        Debug.Assert(ev.Row.DataContext != null);
        Debug.Assert(ev.Row.DataContext is ObjectAnalyzerWindowViewModel.ObjectHolderEntry);
        var rowObjectHolderEntry = (ObjectAnalyzerWindowViewModel.ObjectHolderEntry)ev.Row.DataContext;

        // TODO should use some kind of "template" xaml thing for this, not code?
        var cm = new ContextMenu();
        // TODO this is very Model stuff, may not be correct to put in View code
        switch (rowObjectHolderEntry.ObjectHolder.GetEntryType())
        {
            case Z64.Z64Object.EntryType.DList:
                Debug.Assert(rowObjectHolderEntry != null);
                cm.Items.Add(
                    new MenuItem()
                    {
                        Header = "Open in DList Viewer",
                        Command = ViewModel.OpenDListViewerObjectHolderEntryCommand,
                        CommandParameter = rowObjectHolderEntry,
                    }
                );
                break;
        }
        cm.Items.Add(
            new MenuItem()
            {
                Header = "(TODO)"
            }
        );
        ev.Row.ContextMenu = cm;
    }
}