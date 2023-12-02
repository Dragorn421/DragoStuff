using System;
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

    private int VAO;
    private int ShaderProgram;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        Debug.WriteLine(Name + ".OnOpenGlInit");
        CheckError(gl);

        // Setup for drawing a triangle

        float[] vertices = {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.0f,  0.5f, 0.0f
        };

        unsafe
        {
            fixed (int* p_VAO = &VAO)
            {
                gl.GenVertexArrays(1, p_VAO);
            }
        };

        gl.BindVertexArray(VAO);


        int VBO;
        unsafe
        {
            gl.GenBuffers(1, &VBO);
        }

        gl.BindBuffer(GL_ARRAY_BUFFER, VBO);
        unsafe
        {
            fixed (void* p_vertices = vertices)
            {
                gl.BufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(float) * vertices.Length), new IntPtr(p_vertices), GL_STATIC_DRAW);
            }
        }

        string vertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 aPos;
            void main()
            {
                gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
            }
        ";

        int vertexShader = gl.CreateShader(GL_VERTEX_SHADER);

        var vertexShaderError = gl.CompileShaderAndGetError(vertexShader, vertexShaderSource);
        Debug.WriteLine("vertexShaderError: " + vertexShaderError);

        string fragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
            }
        ";

        int fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);

        var fragmentShaderError = gl.CompileShaderAndGetError(fragmentShader, fragmentShaderSource);
        Debug.WriteLine("fragmentShaderError: " + fragmentShaderError);

        ShaderProgram = gl.CreateProgram();

        gl.AttachShader(ShaderProgram, vertexShader);
        gl.AttachShader(ShaderProgram, fragmentShader);

        var shaderProgramError = gl.LinkProgramAndGetError(ShaderProgram);
        Debug.WriteLine("shaderProgramError: " + shaderProgramError);

        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 3 * sizeof(float), IntPtr.Zero);
        gl.EnableVertexAttribArray(0);

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

        // Set viewport to full size
        // Running this in OnOpenGlRender also means the drawing is stretched to the window size on resize

        // OpenGlControlBase uses VisualRoot as non-null before calling OnOpenGlRender,
        // so it will always be non-null.
        Debug.Assert(VisualRoot != null);
        // This mimics (private) method OpenGlControlBase.GetPixelSize
        int pixelWidth = (int)(Bounds.Width * VisualRoot.RenderScaling);
        int pixelHeight = (int)(Bounds.Height * VisualRoot.RenderScaling);
        Debug.WriteLine($"pixelSize={pixelWidth}x{pixelHeight}");
        gl.Viewport(0, 0, pixelWidth, pixelHeight);

        // Draw triangle

        gl.UseProgram(ShaderProgram);
        gl.BindVertexArray(VAO);
        gl.DrawArrays(GL_TRIANGLES, 0, new IntPtr(3));

        CheckError(gl);
    }
}
