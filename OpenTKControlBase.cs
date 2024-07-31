// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace avalonia_glsl_investigate;

// this is more kinda vaguely based on samples/GpuInterop/DrawingSurfaceDemoBase.cs now
public abstract class OpenTKControlBase : ContentControl
{
    private Compositor? _compositor;
    private readonly Action _update;
    private bool _updateQueued;
    private bool _initialized;
    private Image _contentImage;

    public OpenTKControlBase()
    {
        _update = UpdateFrame;
        _contentImage = new();
        Content = _contentImage;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var selfVisual = ElementComposition.GetElementVisual(this)!;
        _compositor = selfVisual.Compositor;
        Init();
        _initialized = true;
        QueueNextFrame();
    }
    void QueueNextFrame()
    {
        if (_initialized && !_updateQueued && _compositor != null)
        {
            _updateQueued = true;
            _compositor?.RequestCompositionUpdate(_update);
        }
    }

    void UpdateFrame()
    {
        _updateQueued = false;
        Render();
        _win.ProcessEvents(0.001);
    }

    private NativeWindow? _win = null;

    private void Init()
    {
        Debug.WriteLine($"Name={Name} in");

        //*
        _win = new NativeWindow(
            new()
            {
                //APIVersion = new(3, 3), // doesn't work?
                //AutoLoadBindings = false, // doesn't prevent context switch (lol)
                StartVisible = false,
            });
        //*/
        _win.MakeCurrent(); // inside new NativeWindow anyway
        _win.FramebufferResize += e =>
        {
            Debug.WriteLine("_win.FramebufferResize");
            Dispatcher.UIThread.Invoke(QueueNextFrame);
        };
        SizeChanged += (sender, e) =>
        {
            Debug.WriteLine("OpenTKControlBase.SizeChanged");
            PixelSize pixelSize = GetPixelSize();
            Vector2i pixelSizeVec2 = new(pixelSize.Width, pixelSize.Height);
            _win.ClientSize = pixelSizeVec2;
            QueueNextFrame();
        };

        CheckError();

        try
        {
            OnOpenTKInit();
        }
        catch (Exception e)
        {
            Debug.WriteLine("Unhandled exception raised from OnOpenTKInit");
            Debug.WriteLine(e);
            throw;
        }
        CheckError();

        Debug.WriteLine("Name={Name} out", Name);
    }

    private DateTime _lastScreenshot = DateTime.Now - TimeSpan.FromHours(1);

    private void Render()
    {
        Debug.Assert(_win is not null);
        _win.MakeCurrent();
        CheckError();
        PixelSize pixelSize = GetPixelSize();
        Vector2i pixelSizeVec2 = new(pixelSize.Width, pixelSize.Height);
        if (_win.ClientSize != pixelSizeVec2)
        {
            _win.ClientSize = pixelSizeVec2;
        }
        if (_win.FramebufferSize != pixelSizeVec2)
        {
            // wait for the framebuffer to be the right size
            // see _win.FramebufferResize usage above
            QueueNextFrame();
            return;
        }

        // render twice to bypass triple buffering, I think
        for (int i = 0; i < 2; i++)
        {
            CheckError();

            try
            {
                OnOpenTKRender();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debug.WriteLine("Unhandled exception raised from OnOpenTKRender");
                throw;
            }
            CheckError();

            if (_lastScreenshot < DateTime.Now - TimeSpan.FromMilliseconds(10))
            {
                Debug.WriteLine(">screenshot");
                _lastScreenshot = DateTime.Now;
                Debug.WriteLine($"pixelSize.Width * pixelSize.Height * 4 = {pixelSize.Width * pixelSize.Height * 4}");
                var data = new byte[pixelSize.Width * pixelSize.Height * 4];
                Debug.WriteLine($"data.Length = {data.Length}");
                for (int j = 0; j < data.Length; j++)
                    data[j] = 0x42;
                unsafe
                {
                    fixed (byte* dataP = data)
                    {
                        GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                        GL.ReadPixels(0, 0, pixelSize.Width, pixelSize.Height, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, new(dataP));
                        //GL.ReadnPixels(0, 0, pixelSize.Width, pixelSize.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Length, new(dataP));
                    }
                }
                /*
                for (int j = 0; j < data.Length; j++)
                    if (data[j] == 0x42)
                    {
                        bool allMagic = true;
                        for (int k = j; k < data.Length; k++)
                            if (data[k] != 0x42)
                            {
                                allMagic = false;
                                break;
                            }
                        if (allMagic)
                        {
                            Debug.WriteLine($"allMagic starting at {j}");
                            File.WriteAllBytes("data_all.bin", data[..j]);
                            break;
                        }
                    }
                */
                /*
                Debug.WriteLine(sizePx);
                File.WriteAllBytes("screenshot.rgb", data);
                //*/

                var dataMirroredRows = data;

                //*
                Bitmap bitmap;
                unsafe
                {
                    fixed (byte* dataP = dataMirroredRows)
                    {
                        Debug.Assert(VisualRoot is not null);
                        bitmap = new(Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque, new(dataP), pixelSize,
                                     new(96, 96), // TODO still haven't figure this out (see Z64Utils_Avalonia ToAvaloniaBitmap)
                                     pixelSize.Width * 4);
                    }
                }
                var prevImageSource = _contentImage.Source;
                _contentImage.Source = null;
                (prevImageSource as Bitmap)?.Dispose();
                _contentImage.Source = bitmap;
                //bitmap.Save("avaloniabitmap_screenshot.png");
                //*/

                CheckError();
                // >~100 ms not even fullscreen lol
                Debug.WriteLine("<screenshot " + (DateTime.Now - _lastScreenshot));
            }

            // not needed when window is invisible?
            _win.Context.SwapBuffers();
            CheckError();
        }
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

        return PixelSize.FromSize(Bounds.Size, VisualRoot.RenderScaling);
    }

    protected void SetFullViewport()
    {
        var pixelSize = GetPixelSize();
        GL.Viewport(0, 0, pixelSize.Width, pixelSize.Height);
    }

    private void CheckError(ErrorCode[]? ignoredErrors = null)
    {
        ErrorCode err;
        while ((err = GL.GetError()) != ErrorCode.NoError)
        {
            Debug.WriteLine($"Name={Name} GLerror {err}");
#if DEBUG
            if (ignoredErrors != null && ignoredErrors.Contains(err))
            {
                // ignore
            }
            else
            {
                throw new Exception($"Name={Name} GLerror {err}");
            }
#endif
        }
    }

    protected abstract void OnOpenTKInit();
    protected abstract void OnOpenTKRender();
}
