// Copied and adapted from https://github.com/AvaloniaUI/Avalonia/blob/release/11.3.0/samples/GpuInterop

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace avalonia_gl_interop;

public class SharpDXInteropControl : Control
{
    private CompositionSurfaceVisual? _visual;
    private Compositor? _compositor;
    private string _info = string.Empty;
    private bool _updateQueued;
    private bool _initialized;

    protected CompositionDrawingSurface? Surface { get; private set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Initialize();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (_initialized)
        {
            Surface?.Dispose();
            FreeGraphicsResources();
        }

        _initialized = false;
        base.OnDetachedFromLogicalTree(e);
    }

    async void Initialize()
    {
        var selfVisual = ElementComposition.GetElementVisual(this)!;
        _compositor = selfVisual.Compositor;

        Surface = _compositor.CreateDrawingSurface();
        _visual = _compositor.CreateSurfaceVisual();
        _visual.Size = new(Bounds.Width, Bounds.Height);
        _visual.Surface = Surface;
        ElementComposition.SetElementChildVisual(this, _visual);
        var (res, info) = await DoInitialize(_compositor, Surface);
        _info = info;
        _initialized = res;
        QueueNextFrame();
    }

    async Task<(bool success, string info)> DoInitialize(
        Compositor compositor,
        CompositionDrawingSurface compositionDrawingSurface
    )
    {
        var interop = await compositor.TryGetCompositionGpuInterop();
        if (interop == null)
            return (false, "Compositor doesn't support interop for the current backend");
        return InitializeGraphicsResources(compositor, compositionDrawingSurface, interop);
    }

    void QueueNextFrame()
    {
        if (_initialized && !_updateQueued && _compositor != null)
        {
            _updateQueued = true;
            _compositor?.RequestCompositionUpdate(UpdateFrame);
        }
    }

    void UpdateFrame()
    {
        _updateQueued = false;
        var root = this as IRenderRoot ?? VisualRoot;
        if (root == null)
            return;

        _visual!.Size = new(Bounds.Width, Bounds.Height);
        var size = PixelSize.FromSize(Bounds.Size, root.RenderScaling);
        RenderFrame(size);
    }

    private Device? _device;
    private D3D11Swapchain? _swapchain;
    private DeviceContext? _context;
    private PixelSize _lastSize;

    protected (bool success, string info) InitializeGraphicsResources(
        Compositor compositor,
        CompositionDrawingSurface surface,
        ICompositionGpuInterop interop
    )
    {
        if (
            interop.SupportedImageHandleTypes.Contains(
                KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle
            ) != true
        )
            return (
                false,
                "DXGI shared handle import is not supported by the current graphics backend"
            );

        var factory = new SharpDX.DXGI.Factory1();
        using var adapter = factory.GetAdapter1(0);
        _device = new Device(
            adapter,
            DeviceCreationFlags.None,
            new[]
            {
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_0,
                FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2,
                FeatureLevel.Level_9_1,
            }
        );
        _swapchain = new D3D11Swapchain(_device, interop, surface);
        _context = _device.ImmediateContext;
        return (true, $"D3D11 ({_device.FeatureLevel}) {adapter.Description1.Description}");
    }

    protected void FreeGraphicsResources()
    {
        if (_swapchain is not null)
        {
            _swapchain.DisposeAsync().GetAwaiter().GetResult();
            _swapchain = null;
        }

        Utilities.Dispose(ref _context);
        Utilities.Dispose(ref _device);
    }

    protected void RenderFrame(PixelSize pixelSize)
    {
        if (pixelSize == default)
            return;
        if (pixelSize != _lastSize)
        {
            _lastSize = pixelSize;
            Resize(pixelSize);
        }
        using (_swapchain!.BeginDraw(pixelSize, out var renderView))
        {
            _device!.ImmediateContext.OutputMerger.SetTargets(renderView);
            var context = _device.ImmediateContext;

            // Clear views
            context.ClearRenderTargetView(renderView, new RawColor4(1, 0, 0, 1));

            _context!.Flush();
        }
    }

    private void Resize(PixelSize size)
    {
        if (_device is null)
            return;

        // Setup targets and viewport for rendering
        _device.ImmediateContext.Rasterizer.SetViewport(
            new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f)
        );
    }
}
