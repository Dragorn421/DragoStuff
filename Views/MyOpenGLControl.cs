using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace Z64Utils_recreate_avalonia_ui;

class MyOpenGLControl : OpenGlControlBase
{

    // FIXME I really just want to get this working as PoC for now 
    public DListViewerWindowViewModel hack_DLVWVM;

    private void CheckError(GlInterface gl)
    {
        int err;
        while ((err = gl.GetError()) != GL_NO_ERROR)
            Console.WriteLine(Name + " AvaloniaGL GLerror " + err);
    }

    private void CheckErrorTK()
    {
        ErrorCode err;
        while ((err = GL.GetError()) != ErrorCode.NoError)
            Console.WriteLine(Name + " OpenTK GLerror " + err);
    }

    static int next_i_color = 0;
    static float[][] colors = new float[][]{
           new float[]{0, 0.3f, 0, 0},
           new float[]{0.3f, 0.3f, 0, 0},
        };

    int i_color;

    public MyOpenGLControl()
    {
        i_color = next_i_color;
        next_i_color++;
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        Console.WriteLine(Name + ".OnOpenGlInit");

        CheckError(gl);

        LoadOpenTKBindings(gl);

        CheckError(gl);

        hack_DLVWVM.OnOpenTKInitialized(() =>
        {
            RequestNextFrameRendering();
        });

        CheckError(gl);
    }

    // FIXME hack
    delegate IntPtr glXGetCurrentContext_delegate();
    private void LoadOpenTKBindings(GlInterface gl)
    {
        GraphicsContext.GetAddressDelegate getAddress =
            function => gl.GetProcAddress(function);

        IntPtr p_glXGetCurrentContext = getAddress("glXGetCurrentContext");
        var glXGetCurrentContext = (glXGetCurrentContext_delegate)Marshal.GetDelegateForFunctionPointer(p_glXGetCurrentContext, typeof(glXGetCurrentContext_delegate));
        GraphicsContext.GetCurrentContextDelegate getCurrent =
            () => new ContextHandle(glXGetCurrentContext());

        new GraphicsContext(ContextHandle.Zero, getAddress, getCurrent);
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        Console.WriteLine(Name + ".OnOpenGlRender");

        GL.ClearColor(0.3f, 0, 0, 1);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        // using VisualRoot.RenderScaling based on OpenGlControlBase
        Debug.Assert(VisualRoot != null);
        Console.WriteLine(Name + ".VisualRoot.RenderScaling=" + VisualRoot.RenderScaling);
        int w = (int)(Bounds.Width * VisualRoot.RenderScaling);
        int h = (int)(Bounds.Height * VisualRoot.RenderScaling);
        Console.WriteLine(Name + $" glViewport {w}x{h}");
        gl.Viewport(0, 0, w, h);

        CheckError(gl);
        CheckErrorTK();
        hack_DLVWVM.Render();
        CheckError(gl);
        CheckErrorTK();

        if (false)
        {
            float[] vertices = {
                -0.5f, -0.5f, 0.0f,
                0.5f, -0.5f, 0.0f,
                0.0f,  0.5f, 0.0f
            };


            int VAO;
            gl.GenVertexArrays(1, &VAO);

            gl.BindVertexArray(VAO);


            int VBO;
            gl.GenBuffers(1, &VBO);

            gl.BindBuffer(GL_ARRAY_BUFFER, VBO);
            fixed (void* pdata = vertices)
                gl.BufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(float) * vertices.Length), new IntPtr(pdata), GL_STATIC_DRAW);

            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPos;
                void main()
                {
                   gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
                }";

            int vertexShader;
            vertexShader = gl.CreateShader(GL_VERTEX_SHADER);

            //gl.ShaderSource(vertexShader, 1, &vertexShaderSource, NULL);
            //gl.CompileShader(vertexShader);
            Console.WriteLine(gl.CompileShaderAndGetError(vertexShader, vertexShaderSource));

            CheckError(gl);

            string fragmentShaderSource = @"
                #version 330 core
                out vec4 FragColor;

                void main()
                {
                    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
                }
                ";

            int fragmentShader;
            fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
            Console.WriteLine(gl.CompileShaderAndGetError(fragmentShader, fragmentShaderSource));

            CheckError(gl);

            int shaderProgram;
            shaderProgram = gl.CreateProgram();

            gl.AttachShader(shaderProgram, vertexShader);
            gl.AttachShader(shaderProgram, fragmentShader);
            Console.WriteLine(gl.LinkProgramAndGetError(shaderProgram));

            CheckError(gl);

            gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 3 * sizeof(float), IntPtr.Zero);
            gl.EnableVertexAttribArray(0);

            gl.UseProgram(shaderProgram);
            gl.BindVertexArray(VAO);
            gl.DrawArrays(GL_TRIANGLES, 0, new IntPtr(3));

            CheckError(gl);
        }
    }
}
