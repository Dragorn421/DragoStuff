// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using System;
using System.Diagnostics;
using Avalonia;
using OpenTK.Graphics.OpenGL;

namespace avalonia_glsl_investigate;

public class ShaderTestOpenTKControl : OpenTKControlBase
{
    public static readonly StyledProperty<string> VertShaderSrcProperty =
        AvaloniaProperty.Register<ShaderTestOpenGLControl, string>(nameof(VertShaderSrc));
    public string VertShaderSrc
    {
        get => GetValue(VertShaderSrcProperty);
        set => SetValue(VertShaderSrcProperty, value);
    }

    public static readonly StyledProperty<string> FragShaderSrcProperty =
        AvaloniaProperty.Register<ShaderTestOpenGLControl, string>(nameof(FragShaderSrc));
    public string FragShaderSrc
    {
        get => GetValue(FragShaderSrcProperty);
        set => SetValue(FragShaderSrcProperty, value);
    }

    public static readonly StyledProperty<string> LogProperty =
        AvaloniaProperty.Register<ShaderTestOpenGLControl, string>(nameof(Log), defaultValue: "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public string Log
    {
        get => GetValue(LogProperty);
        set => SetValue(LogProperty, value);
    }

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ShaderTestOpenGLControl, bool>(nameof(HasError), defaultValue: false, defaultBindingMode: Avalonia.Data.BindingMode.OneWayToSource);
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    private bool _shadersCompiled = false;
    private int _vertexShader, _fragmentShader, _shaderProgram;
    private bool _geometryLoaded = false;
    private int _vertexBufObj, _elemBufObj, _vertexArrObj;

    protected override void OnOpenTKRender()
    {
        if (VertShaderSrc == null || FragShaderSrc == null)
            return;

        CheckError("OnOpenTKRender entry", ErrorCode.InvalidEnum);

        if (!_shadersCompiled)
        {
            Debug.Assert(VertShaderSrc != null);
            Debug.Assert(FragShaderSrc != null);

            Log += "GL_VERSION = " + GL.GetString(StringName.Version) + "\n";
            Log += "GL_VENDOR = " + GL.GetString(StringName.Vendor) + "\n";
            Log += "GL_RENDERER = " + GL.GetString(StringName.Renderer) + "\n";
            /*
            // OpenTK does not have GL_CONTEXT_PROFILE_MASK ?
            GL.GetIntegerv(GlConsts.GL_CONTEXT_PROFILE_MASK, out int contextProfileMask);
            Log += "GL_CONTEXT_PROFILE_MASK = " + contextProfileMask
                 + " (" + ((contextProfileMask & GlConsts.GL_CONTEXT_CORE_PROFILE_BIT) != 0 ? "GL_CONTEXT_CORE_PROFILE_BIT" : "0") + "|" + ((contextProfileMask & GlConsts.GL_CONTEXT_COMPATIBILITY_PROFILE_BIT) != 0 ? "GL_CONTEXT_COMPATIBILITY_PROFILE_BIT" : "0") + ")"
                 + "\n";
            //*/
            Log += "GL_SHADING_LANGUAGE_VERSION = " + GL.GetString(StringName.ShadingLanguageVersion) + "\n";
            /*
            // OpenTK does not have GL_NUM_SHADING_LANGUAGE_VERSIONS ?
            // /usr/include/GL/glext.h
            const int GL_NUM_SHADING_LANGUAGE_VERSIONS = 0x82E9;
            GL.GetInteger( GL_NUM_SHADING_LANGUAGE_VERSIONS);
            Log += "GL_NUM_SHADING_LANGUAGE_VERSIONS = " + numShadingLanguageVersions + "\n";
            for (int k = 0; k < numShadingLanguageVersions; k++)
                Log += "GL_SHADING_LANGUAGE_VERSION​[" + k + "] = " + GL.GetString(StringNameIndexed.ShadingLanguageVersion, k) + "\n";
            //*/

            GL.UseProgram(0);

            _vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShader, VertShaderSrc);
            GL.CompileShader(_vertexShader);
            var vertCompileError = GL.GetShaderInfoLog(_vertexShader);
            if (vertCompileError == "")
                Log += "vert shader compile OK\n";
            else
            {
                HasError = true;
                Log += ">GL.CompileShaderAndGetError vert:\n"
                     + vertCompileError
                     + "<GL.CompileShaderAndGetError vert\n";
            }

            _fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_fragmentShader, FragShaderSrc);
            GL.CompileShader(_fragmentShader);
            var fragCompileError = GL.GetShaderInfoLog(_fragmentShader);
            if (fragCompileError == "")
                Log += "frag shader compile OK\n";
            else
            {
                HasError = true;
                Log += ">GL.CompileShaderAndGetError frag:\n"
                     + fragCompileError + "\n"
                     + "<GL.CompileShaderAndGetError frag\n";
            }

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, _vertexShader);
            GL.AttachShader(_shaderProgram, _fragmentShader);
            GL.LinkProgram(_shaderProgram);
            var progLinkError = GL.GetProgramInfoLog(_shaderProgram);
            if (progLinkError == "")
                Log += "shader program link OK\n";
            else
            {
                HasError = true;
                Log += ">GL.GetProgramInfoLog:\n"
                     + progLinkError + "\n"
                     + "<GL.GetProgramInfoLog\n";
            }

            CheckError("shaders compiled");

            _shadersCompiled = true;
        }

        if (HasError)
            return;

        if (!_geometryLoaded)
        {
            _vertexArrObj = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrObj);

            _vertexBufObj = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufObj);
            unsafe
            {
                fixed (void* pdata = GLTriangleData.points)
                    GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(GLTriangleData.points.Length * sizeof(float)), new IntPtr(pdata), BufferUsageHint.StaticDraw);
            }

            _elemBufObj = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elemBufObj);
            unsafe
            {
                fixed (void* pdata = GLTriangleData.elems)
                    GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(GLTriangleData.elems.Length * sizeof(ushort)), new IntPtr(pdata), BufferUsageHint.StaticDraw);
            }

            // 0 == "location = 0" in the vertex shader
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), IntPtr.Zero);
            GL.EnableVertexAttribArray(0);

            CheckError("geometry loaded");

            _geometryLoaded = true;
        }

        // not strictly needed
        GL.BindVertexArray(0); // (must come first)
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        GL.ClearColor(0.0f, 0.3f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // OpenGlControlBase implementation details mean VisualRoot is non-null here
        Debug.Assert(VisualRoot != null);
        // This mimics (private) method OpenGlControlBase.GetPixelSize
        int pixelWidth = (int)(Bounds.Width * VisualRoot.RenderScaling);
        int pixelHeight = (int)(Bounds.Height * VisualRoot.RenderScaling);
        GL.Viewport(0, 0, pixelWidth, pixelHeight);

        GL.BindVertexArray(_vertexArrObj);
        GL.UseProgram(_shaderProgram);
        GL.DrawElements(BeginMode.Triangles, GLTriangleData.elems.Length, DrawElementsType.UnsignedShort, 0);

        CheckError("OnOpenGlRender end");
    }

    private void CheckError(string ctx, ErrorCode? ignoredErrorIfFirst = null)
    {
        bool first = true;
        ErrorCode err;
        while ((err = GL.GetError()) != ErrorCode.NoError)
        {
            if (first && err == ignoredErrorIfFirst)
                continue;
            first = false;
            Log += $"{ctx}: GL.GetError() -> {err}\n";
        }
    }

    protected override void OnOpenTKInit()
    {

    }
}
