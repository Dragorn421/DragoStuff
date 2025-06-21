// Copied and adapted from https://github.com/AvaloniaUI/Avalonia/blob/release/11.3.0/samples/GpuInterop

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
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

    public SharpDXInteropControl()
    {
        SizeChanged += (sender, e) =>
        {
            Console.WriteLine("SizeChanged");
            QueueNextFrame();
        };
    }

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
        var interop = await _compositor.TryGetCompositionGpuInterop();
        bool res;
        string info;
        if (interop == null)
            (res, info) = (false, "Compositor doesn't support interop for the current backend");
        else
            (res, info) = InitializeGraphicsResources(Surface, interop);
        Console.WriteLine(info);
        _info = info;
        _initialized = res;
        QueueNextFrame();
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

    private NativeWindow? _openTKWindow;

    protected (bool success, string info) InitializeGraphicsResources(
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

        _openTKWindow = new NativeWindow(
            new() { StartVisible = false, ClientSize = new(100, 100) }
        );

        return (true, $"D3D11 ({_device.FeatureLevel}) {adapter.Description1.Description}");
    }

    protected void FreeGraphicsResources()
    {
        Console.WriteLine("FreeGraphicsResources");

        if (_swapchain is not null)
        {
            _swapchain.DisposeAsync().GetAwaiter().GetResult();
            _swapchain = null;
        }

        Utilities.Dispose(ref _context);
        Utilities.Dispose(ref _device);

        _openTKWindow?.Dispose();
        _openTKWindow = null;
    }

    [DllImport("opengl32.dll")]
    private static extern IntPtr wglGetCurrentDC();

    protected void RenderFrame(PixelSize pixelSize)
    {
        if (pixelSize == default)
            return;
        if (pixelSize != _lastSize)
        {
            _lastSize = pixelSize;
            Resize(pixelSize);
        }
        using (_swapchain!.BeginDraw(pixelSize, out var image))
        {
            _device!.ImmediateContext.OutputMerger.SetTargets(image.RenderTargetView);
            var context = _device.ImmediateContext;

            // Clear views
            context.ClearRenderTargetView(image.RenderTargetView, new RawColor4(1, 0, 0, 1));

            _openTKWindow!.Context.MakeCurrent();

            GL.DebugMessageCallback(MyGLDebugMessageCallback, IntPtr.Zero);

            Wgl.LoadBindings(new GLFWBindingsContext());

            IntPtr hDC = wglGetCurrentDC();
            if (hDC == IntPtr.Zero)
                throw new InvalidOperationException(
                    "No current hDC. Make sure OpenGL context is current."
                );
            Console.WriteLine(Wgl.Arb.GetExtensionsString(hDC));
            string[] extensions = Wgl
                .Arb.GetExtensionsString(hDC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasInterop = extensions.Contains("WGL_NV_DX_interop");
            Console.WriteLine($"NV_DX_interop supported? {hasInterop}");
            if (!hasInterop)
                throw new PlatformNotSupportedException(
                    "NV_DX_interop not available on this device."
                );

            Console.WriteLine("DXOpenDeviceNV");

            var hDevice = Wgl.DXOpenDeviceNV(_device.NativePointer);

            if (hDevice == IntPtr.Zero)
            {
                throw new Exception("DXOpenDeviceNV failed");
            }

            GL.GenTextures(1, out uint gl_name);

            var hCfb = Wgl.DXRegisterObjectNV(
                hDevice,
                image.Texture.NativePointer, // wrong?
                gl_name,
                (int)TextureTarget2d.Texture2D,
                WGL_NV_DX_interop.AccessReadWrite
            );

            if (hCfb == IntPtr.Zero)
            {
                throw new Exception("DXRegisterObjectNV failed");
            }

            var lockResult = Wgl.DXLockObjectsNV(hDevice, 1, new[] { hCfb });
            if (!lockResult)
            {
                throw new Exception($"DXLockObjectsNV failed {GetLastError()}");
            }

            var framebufferName = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferName);
            GL.FramebufferTexture(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                gl_name,
                0
            );
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            var fbStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (fbStatus != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"incomplete framebuffer: {fbStatus}");
            }

            GL.Viewport(0, 0, 100, 100); // TODO
            GL.ClearColor(0, 1, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var unlockResult = Wgl.DXUnlockObjectsNV(hDevice, 1, new[] { hCfb });
            if (!unlockResult)
            {
                throw new Exception($"DXUnlockObjectsNV failed {GetLastError()}");
            }

            Wgl.DXUnregisterObjectNV(hDevice, hCfb);

            Wgl.DXCloseDeviceNV(hDevice);

            _openTKWindow.Context.MakeNoneCurrent();

            _context!.Flush();
        }
    }

    [DllImport("Kernel32.dll")]
    public static extern int GetLastError();

    private void MyGLDebugMessageCallback(
        DebugSource source,
        DebugType type,
        int id,
        DebugSeverity severity,
        int length,
        IntPtr messagePtr,
        IntPtr userParam
    )
    {
        string message = Marshal.PtrToStringAnsi(messagePtr, length);
        Console.WriteLine($"{source} {type} {id} {severity} {message}");
    }

    private void Resize(PixelSize size)
    {
        Console.WriteLine($"Resize {size.Width}x{size.Height}");
        if (_device is null)
            return;

        // Setup targets and viewport for rendering
        _device.ImmediateContext.Rasterizer.SetViewport(
            new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f)
        );

        _openTKWindow!.ClientSize = (size.Width, size.Height);
        _openTKWindow.Context.MakeCurrent();
        GL.Viewport(0, 0, size.Width, size.Height);
        _openTKWindow.Context.MakeNoneCurrent();
    }
}
