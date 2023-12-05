using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia;
using F3DZEX.Command;
using F3DZEX.Render;

namespace Z64Utils_recreate_avalonia_ui;

public class DLViewerControl : OpenTKControlBaseWithCamera
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static readonly StyledProperty<Renderer?> RendererProperty =
        AvaloniaProperty.Register<DLViewerControl, Renderer?>(nameof(Renderer), defaultValue: null);
    public Renderer? Renderer
    {
        get => GetValue(RendererProperty);
        set => SetValue(RendererProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<Dlist>> DListsProperty =
        AvaloniaProperty.Register<DLViewerControl, ObservableCollection<Dlist>>(nameof(DLists), defaultValue: new());
    public ObservableCollection<Dlist> DLists
    {
        get => GetValue(DListsProperty);
        set => SetValue(DListsProperty, value);
    }

    public static readonly StyledProperty<string?> RenderErrorProperty =
        AvaloniaProperty.Register<DLViewerControl, string?>(nameof(RenderError), defaultValue: null);
    public string? RenderError
    {
        get => GetValue(RenderErrorProperty);
        set => SetValue(RenderErrorProperty, value);
    }

    public DLViewerControl()
    {
        Logger.Debug("Name={Name}", Name);

        DLists.CollectionChanged += OnDlistsCollectionChanged;

        PropertyChanged += (sender, e) =>
        {
            if (e.Property == RendererProperty)
            {
                RequestNextFrameRenderingIfInitialized();
            }
            if (e.Property == DListsProperty)
            {
                var oldValue = e.GetOldValue<ObservableCollection<Dlist>>();
                if (oldValue != null)
                    oldValue.CollectionChanged -= OnDlistsCollectionChanged;

                var newValue = e.GetNewValue<ObservableCollection<Dlist>>();
                Debug.Assert(newValue != null);
                newValue.CollectionChanged += OnDlistsCollectionChanged;

                RequestNextFrameRenderingIfInitialized();
            }
        };
    }

    private void OnDlistsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RequestNextFrameRenderingIfInitialized();
    }

    protected override void OnOpenTKInit()
    {

    }

    protected override void OnOpenTKRender()
    {
        Logger.Trace("Name={Name}", Name);
        SetFullViewport();

        if (Renderer != null)
        {
            Logger.Trace("Name={Name} RenderStart", Name);

            Renderer.RenderStart(Proj, View);

            foreach (var dList in DLists)
            {
                if (Renderer.RenderFailed())
                    break;

                Logger.Trace("Name={Name} RenderDList({dList})", Name, dList);
                Renderer.RenderDList(dList);
            }


            if (Renderer.RenderFailed())
            {
                var addr = Renderer.RenderErrorAddr;
                var msg = Renderer.ErrorMsg;
                RenderError = $"At 0x{addr:X8}: {msg}";
            }
            else
            {
                RenderError = null;
            }
        }
    }
}
