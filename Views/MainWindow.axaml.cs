using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Z64Utils_recreate_avalonia_ui;

public partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel;

    public MainWindow()
    {
        ViewModel = new MainWindowViewModel()
        {
            GetOpenROM = ShowDialogOpenROMAsync,
            PickSegmentID = OpenPickSegmentID,
            OpenObjectAnalyzer = OpenObjectAnalyzer,
            OpenDListViewer = OpenDListViewer,
        };
        DataContext = ViewModel;
        InitializeComponent();
    }

    private async Task<IStorageFile?> ShowDialogOpenROMAsync()
    {
        Debug.Assert(StorageProvider.CanOpen);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open ROM",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>() {
                 new FilePickerFileType("N64 ROM image") {
                    Patterns = new[] { "*.z64" },
                    MimeTypes = new[] { "application/x-n64-rom" }
                }
            }
        });

        if (files.Count == 0)
        {
            return null;
        }
        else
        {
            Debug.Assert(files.Count == 1);
            return files[0];
        }
    }

    private async Task<int?> OpenPickSegmentID()
    {
        var pickSegmentIDWin = new PickSegmentIDWindow();
        var dialogResultTask = pickSegmentIDWin.ShowDialog<int?>(this);
        int? segmentID = await dialogResultTask;
        return segmentID;
    }

    private ObjectAnalyzerWindowViewModel OpenObjectAnalyzer()
    {
        var win = new ObjectAnalyzerWindow();
        win.Show();
        return win.ViewModel;
    }

    private DListViewerWindowViewModel OpenDListViewer()
    {
        var win = new DListViewerWindow();
        win.Show();
        return win.ViewModel;
    }

    public void OnRomFileDataGridLoadingRow(object? sender, DataGridRowEventArgs ev)
    {
        Debug.Assert(ev.Row.DataContext != null);
        Debug.Assert(ev.Row.DataContext is MainWindowViewModel.RomFile);
        var rowRomFile = (MainWindowViewModel.RomFile)ev.Row.DataContext;

        // TODO should use some kind of "template" xaml thing for this, not code?
        var cm = new ContextMenu();
        Debug.Assert(rowRomFile != null);
        cm.Items.Add(
            new MenuItem()
            {
                Header = "Open Object Analyzer",
                Command = ViewModel.OpenObjectAnalyzerRomFileCommand,
                CommandParameter = rowRomFile,
            }
        );
        ev.Row.ContextMenu = cm;
    }
}
