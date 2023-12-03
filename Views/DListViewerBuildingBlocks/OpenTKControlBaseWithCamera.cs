using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering;
using OpenTK;

namespace Z64Utils_recreate_avalonia_ui;

public abstract class OpenTKControlBaseWithCamera : OpenTKControlBase, ICustomHitTest
{
    private CameraHandling _camera;

    protected Matrix4 Proj { get; private set; }
    protected Matrix4 View { get => _camera.View; }

    public OpenTKControlBaseWithCamera()
    {
        Console.WriteLine(Name + "(OpenTKControlBaseWithCamera).ctor");
        _camera = new CameraHandling();
        _camera.PropertyChanged += OnCameraPropertyChanged;

        ClipToBounds = true; // cf HitTest

        UpdateProjectionMatrix();
    }

    private void OnCameraPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_camera.View):
                Console.WriteLine("OnCameraPropertyChanged View" + View);
                RequestNextFrameRenderingIfInitialized();
                break;
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        UpdateProjectionMatrix();

        // OpenGlControlBase already issues a redraw request on resize,
        // so this isn't strictly needed, but it doesn't hurt.
        // TODO test this is true
        RequestNextFrameRenderingIfInitialized();
    }

    private void UpdateProjectionMatrix()
    {
        Console.WriteLine("UpdateProjectionMatrix WxH=" + Bounds.Width + "x" + Bounds.Height);
        double aspectRatio = Bounds.Width / Bounds.Height;
        if (double.IsNaN(aspectRatio) || double.IsInfinity(aspectRatio) || aspectRatio <= 0.0)
        {
            aspectRatio = 1.0;
        }
        Proj = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI / 4), (float)aspectRatio, 1, 500000);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var pos = new Vector2((float)point.Position.X, (float)point.Position.Y);

        if (point.Properties.IsLeftButtonPressed)
            _camera.OnMouseMoveWithLeftClickHeld(pos);
        if (point.Properties.IsRightButtonPressed)
            _camera.OnMouseMoveWithRightClickHeld(pos);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Console.WriteLine("OnPointerReleased");
        _camera.OnMouseUp();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // TODO why is Delta a vector
        Console.WriteLine($"OnPointerWheelChanged e.Delta={e.Delta.X},{e.Delta.Y}");
        _camera.OnMouseWheel((float)e.Delta.Y);
    }

    // Workaround https://github.com/AvaloniaUI/Avalonia/issues/10812
    // Without this, the pointer can't interact with an OpenGlControlBase
    public bool HitTest(Point p)
    {
        return true;
    }
}
