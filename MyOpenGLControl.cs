using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using static Avalonia.OpenGL.GlConsts;

namespace avalonia_gl_minimal;

// https://github.com/AvaloniaUI/Avalonia/blob/release/11.0.5/src/Avalonia.OpenGL/Controls/OpenGlControlBase.cs
public class MyOpenGLControl : OpenGlControlBase
{
    private void CheckError(GlInterface gl)
    {
        int err;
        bool anyError = false;
        while ((err = gl.GetError()) != GL_NO_ERROR)
        {
            Debug.WriteLine(Name + ".CheckError " + err);
            anyError = true;
        }
        if (!anyError)
        {
            Debug.WriteLine(Name + ".CheckError OK");
        }
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        Debug.WriteLine(Name + ".OnOpenGlInit");
        CheckError(gl);
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        Debug.WriteLine(Name + ".OnOpenGlDeinit");
        CheckError(gl);
    }

    protected override void OnOpenGlLost()
    {
        Debug.WriteLine(Name + ".OnOpenGlLost");
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        Debug.WriteLine(Name + ".OnOpenGlRender");
        CheckError(gl);

        gl.ClearColor(0.0f, 0.3f, 0.0f, 1.0f);
        gl.Clear(GL_COLOR_BUFFER_BIT);

        CheckError(gl);
    }
}
