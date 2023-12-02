using System;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using F3DZEX.Command;
using OpenTK;
using Z64;

namespace Z64Utils_recreate_avalonia_ui;

public partial class DListViewerWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _someTextForNow = "hey";

    private class RenderRoutine
    {
        public uint Address;
        public int X;
        public int Y;
        public int Z;
        public Dlist? Dlist;

        public RenderRoutine(uint addr, int x = 0, int y = 0, int z = 0)
        {
            Address = addr;
            X = x;
            Y = y;
            Z = z;
            Dlist = null;
        }

        public override string ToString() => $"{Address:X8} [{X};{Y};{Z}]";
    }

    private Z64Game _game;
    private F3DZEX.Render.Renderer _renderer;
    private RenderRoutine? _curRenderRoutine;

    public DListViewerWindowViewModel(Z64Game game)
    {
        _game = game;
        _renderer = new F3DZEX.Render.Renderer(_game, new F3DZEX.Render.Renderer.Config());

        PropertyChanged += HandlePropertyChanged;
    }

    Action? RequestRenderImpl;

    private void RequestRender()
    {
        if (RequestRenderImpl != null)
        {
            RequestRenderImpl();
        }
    }

    public void OnOpenTKInitialized(Action requestRenderImpl)
    {
        Console.WriteLine("DListViewerWindowViewModel.OnOpenTKInitialized");

        // what to put here?

        RequestRenderImpl = requestRenderImpl;
    }

    [ObservableProperty]
    private float _viewRotX = -40;
    [ObservableProperty]
    private float _viewRotY = 180 * (1 + 1.0f / 8);
    [ObservableProperty]
    private float _viewPosX = 1500;
    [ObservableProperty]
    private float _viewPosY = 500;
    [ObservableProperty]
    private float _viewPosZ = 1000;

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewRotX):
            case nameof(ViewRotY):
            case nameof(ViewPosX):
            case nameof(ViewPosY):
            case nameof(ViewPosZ):
                RequestRender();
                break;
        }
    }

    public void Render()
    {
        if (_curRenderRoutine == null)
            return;

        // ModelViewerControl.HandleCamera
        float aspectRatio = 1.5f;// TODO
        var view = Matrix4.Identity;
        view *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(ViewRotY));
        view *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(ViewRotX));
        view *= Matrix4.CreateTranslation(ViewPosX, ViewPosY, ViewPosZ);
        var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 500000);

        _renderer.RenderStart(proj, view);

        _renderer.ModelMtxStack.Push(Matrix4.CreateTranslation(_curRenderRoutine.X, _curRenderRoutine.Y, _curRenderRoutine.Z));
        Debug.Assert(_curRenderRoutine.Dlist != null);
        _renderer.RenderDList(_curRenderRoutine.Dlist);
        _renderer.ModelMtxStack.Pop();

        if (_renderer.RenderFailed())
            Console.WriteLine($"RENDER ERROR AT 0x{_renderer.RenderErrorAddr:X8}! ({_renderer.ErrorMsg})");
    }

    public void SetSegment(int index, F3DZEX.Memory.Segment segment)
    {
        if (index >= 0 && index < F3DZEX.Memory.Segment.COUNT)
        {
            _renderer.Memory.Segments[index] = segment;

            // TODO redecode dlist
            RequestRender();
        }
    }

    public void SetSingleDlist(uint vaddr, int x = 0, int y = 0, int z = 0)
    {
        Console.WriteLine("DListViewerWindowViewModel.SetSingleDlist");

        _curRenderRoutine = new RenderRoutine(vaddr);

        _renderer.ClearErrors();
        _curRenderRoutine.Dlist = _renderer.GetDlist(_curRenderRoutine.Address);

        RequestRender();
    }
}
