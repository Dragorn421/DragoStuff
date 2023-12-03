using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Z64;

namespace Z64Utils_recreate_avalonia_ui;

public partial class DListViewerWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _someTextForNow = "hey";
    [ObservableProperty]
    private F3DZEX.Render.Renderer _renderer;
    [ObservableProperty]
    private ObservableCollection<F3DZEX.Command.Dlist> _dLists = new();
    [ObservableProperty]
    private string? _decodeError;
    [ObservableProperty]
    private string? _renderError;

    private Z64Game _game;

    public DListViewerWindowViewModel(Z64Game game)
    {
        _game = game;
        Renderer = new F3DZEX.Render.Renderer(_game, new F3DZEX.Render.Renderer.Config());
    }

    public void SetSegment(int index, F3DZEX.Memory.Segment segment)
    {
        if (index >= 0 && index < F3DZEX.Memory.Segment.COUNT)
        {
            Renderer.Memory.Segments[index] = segment;

            // TODO redecode dlist, rerender
        }
    }

    public void SetSingleDlist(uint vaddr)
    {
        Console.WriteLine("DListViewerWindowViewModel.SetSingleDlist");

        F3DZEX.Command.Dlist? dList;
        try
        {
            dList = Renderer.GetDlist(vaddr);
        }
        catch (Exception e)
        {
            DecodeError = $"Could not decode DL 0x{vaddr:X8}: {e.Message}";
            dList = null;
        }
        if (dList != null)
        {
            DLists.Clear();
            DLists.Add(dList);
        }
    }
}
