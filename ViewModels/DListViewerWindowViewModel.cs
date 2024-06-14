using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Z64Utils_recreate_avalonia_ui;

public partial class DListViewerWindowViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private string _someTextForNow = "hey";
    [ObservableProperty]
    public F3DZEX.Render.Renderer? _renderer;
    [ObservableProperty]
    private ObservableCollection<F3DZEX.Command.Dlist> _dLists = new();
    [ObservableProperty]
    private string? _decodeError;
    [ObservableProperty]
    private string? _renderError;

    public DListViewerWindowViewModel()
    {
        PropertyChanging += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Renderer):
                    if (Renderer != null)
                        Renderer.PropertyChanged -= OnRendererPropertyChanged;
                    DLists.Clear();
                    DecodeError = null;
                    RenderError = null;
                    break;
            }
        };
        PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Renderer):
                    if (Renderer != null)
                        Renderer.PropertyChanged += OnRendererPropertyChanged;
                    break;
            }
        };
    }

    private void OnRendererPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.Assert(Renderer != null);
        switch (e.PropertyName)
        {
            case nameof(Renderer.HasError):
                if (Renderer.HasError)
                {
                    RenderError = $"RENDER ERROR AT 0x{Renderer.RenderErrorAddr:X8}! ({Renderer.ErrorMsg})";
                }
                else
                {
                    RenderError = null;
                }
                break;
        }
    }

    public void SetSegment(int index, F3DZEX.Memory.Segment segment)
    {
        if (Renderer == null)
            throw new Exception("Renderer is null");

        if (index >= 0 && index < F3DZEX.Memory.Segment.COUNT)
        {
            Renderer.Memory.Segments[index] = segment;

            // TODO redecode dlist, rerender
        }
    }

    public void SetSingleDlist(uint vaddr)
    {
        if (Renderer == null)
            throw new Exception("Renderer is null");

        Logger.Debug("vaddr={vaddr}", vaddr);

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
