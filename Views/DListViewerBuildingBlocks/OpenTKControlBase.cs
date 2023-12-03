using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace Z64Utils_recreate_avalonia_ui;

public abstract class OpenTKControlBase : OpenGlControlBase
{
    private void CheckError(GlInterface gl)
    {
        int err;
        while ((err = gl.GetError()) != GL_NO_ERROR)
            Console.WriteLine(Name + " GLerror " + err);
    }

    bool _initialized = false;
    // Circumvent what I think is a bug in OpenGlControlBase,
    // RequestNextFrameRendering can set _updateQueued to true
    // without actually calling RequestCompositionUpdate
    // (if _compositor is null), leading to essentially a softlock.
    public void RequestNextFrameRenderingIfInitialized()
    {
        if (_initialized)
            RequestNextFrameRendering();
        // If not initialized yet no need to call RequestNextFrameRendering,
        // OpenGlControlBase will basically do it when it's ready to.
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        _initialized = true;

        Console.WriteLine(Name + "(OpenTKControlBase).OnOpenGlInit");

        CheckError(gl);

        LoadOpenTKBindings(gl);
        CheckError(gl);

        OnOpenTKInit();
        CheckError(gl);
    }

    // FIXME hack
    delegate IntPtr glXGetCurrentContext_delegate();
    private void LoadOpenTKBindings(GlInterface gl)
    {
        GraphicsContext.GetAddressDelegate getAddress =
            function => gl.GetProcAddress(function);

        IntPtr p_glXGetCurrentContext = getAddress("glXGetCurrentContext");
        Debug.Assert(p_glXGetCurrentContext != IntPtr.Zero);
        var glXGetCurrentContext = (glXGetCurrentContext_delegate)Marshal.GetDelegateForFunctionPointer(p_glXGetCurrentContext, typeof(glXGetCurrentContext_delegate));
        GraphicsContext.GetCurrentContextDelegate getCurrent =
            () => new ContextHandle(glXGetCurrentContext());

        new GraphicsContext(ContextHandle.Zero, getAddress, getCurrent);
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _initialized = false;
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        Console.WriteLine(Name + "(OpenTKControlBase).OnOpenGlRender");

        CheckError(gl);

        OnOpenTKRender();
        CheckError(gl);
    }

    protected PixelSize GetPixelSize()
    {
        // VisualRoot is set "if the control is attached to a visual tree".
        // This will always be true in OnOpenGlInit and OnOpenGlRender,
        // as the parent class OpenGlControlBase basically waits for
        // OnAttachedToVisualTree to do anything, plus it later checks for
        // VisualRoot to be non-null before calling child init/render.
        // https://github.com/AvaloniaUI/Avalonia/blob/release/11.0.5/src/Avalonia.OpenGL/Controls/OpenGlControlBase.cs
        Debug.Assert(VisualRoot != null);

        // Copy-paste of OpenGlControlBase.GetPixelSize
        var scaling = VisualRoot.RenderScaling;
        return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
            Math.Max(1, (int)(Bounds.Height * scaling)));
    }

    protected void SetFullViewport()
    {
        var pixelSize = GetPixelSize();
        GL.Viewport(0, 0, pixelSize.Width, pixelSize.Height);
    }

    protected abstract void OnOpenTKInit();
    protected abstract void OnOpenTKRender();
}
