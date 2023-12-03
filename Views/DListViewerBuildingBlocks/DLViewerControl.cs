using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia;
using F3DZEX.Command;
using F3DZEX.Render;
using OpenTK;

namespace Z64Utils_recreate_avalonia_ui;

public class DLViewerControl : OpenTKControlBaseWithCamera
{
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
        Console.WriteLine(Name + "(DLViewerControl).ctor");

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
        Console.WriteLine(Name + "(DLViewerControl).OnOpenTKRender");
        SetFullViewport();

        if (Renderer != null)
        {
            Console.WriteLine(Name + "(DLViewerControl).OnOpenTKRender RenderStart");

            Renderer.RenderStart(Proj, View);

            foreach (var dList in DLists)
            {
                if (Renderer.RenderFailed())
                    break;

                Console.WriteLine(Name + "(DLViewerControl).OnOpenTKRender RenderDList " + dList);
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
