// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;

namespace avalonia_glsl_investigate;

public class ShaderTestOpenGLControl : OpenGlControlBase
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

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (VertShaderSrc == null || FragShaderSrc == null)
            return;

        CheckError(gl, "OnOpenGlRender entry", GlConsts.GL_INVALID_ENUM);

        if (!_shadersCompiled)
        {
            Debug.Assert(VertShaderSrc != null);
            Debug.Assert(FragShaderSrc != null);

            Log += "GL_VERSION = " + gl.GetString(GlConsts.GL_VERSION) + "\n";
            Log += "GL_VENDOR = " + gl.GetString(GlConsts.GL_VENDOR) + "\n";
            Log += "GL_RENDERER = " + gl.GetString(GlConsts.GL_RENDERER) + "\n";
            gl.GetIntegerv(GlConsts.GL_CONTEXT_PROFILE_MASK, out int contextProfileMask);
            Log += "GL_CONTEXT_PROFILE_MASK = " + contextProfileMask
                 + " (" + ((contextProfileMask & GlConsts.GL_CONTEXT_CORE_PROFILE_BIT) != 0 ? "GL_CONTEXT_CORE_PROFILE_BIT" : "0") + "|" + ((contextProfileMask & GlConsts.GL_CONTEXT_COMPATIBILITY_PROFILE_BIT) != 0 ? "GL_CONTEXT_COMPATIBILITY_PROFILE_BIT" : "0") + ")"
                 + "\n";
            // /usr/include/GL/glext.h
            const int GL_SHADING_LANGUAGE_VERSION = 0x8B8C;
            const int GL_NUM_SHADING_LANGUAGE_VERSIONS = 0x82E9;
            Log += "GL_SHADING_LANGUAGE_VERSION = " + gl.GetString(GL_SHADING_LANGUAGE_VERSION) + "\n";
            gl.GetIntegerv(GL_NUM_SHADING_LANGUAGE_VERSIONS, out int numShadingLanguageVersions);
            Log += "GL_NUM_SHADING_LANGUAGE_VERSIONS = " + numShadingLanguageVersions + "\n";
            for (int k = 0; k < numShadingLanguageVersions; k++)
                Log += "GL_SHADING_LANGUAGE_VERSION​[" + k + "] = " + gl.GetString(GL_SHADING_LANGUAGE_VERSION​, k) + "\n";

            gl.UseProgram(0);

            _vertexShader = gl.CreateShader(GlConsts.GL_VERTEX_SHADER);
            var vertCompileError = gl.CompileShaderAndGetError(_vertexShader, VertShaderSrc);
            if (vertCompileError == null)
                Log += "vert shader compile OK\n";
            else
            {
                HasError = true;
                Log += ">gl.CompileShaderAndGetError vert:\n"
                     + vertCompileError
                     + "<gl.CompileShaderAndGetError vert\n";
            }

            _fragmentShader = gl.CreateShader(GlConsts.GL_FRAGMENT_SHADER);
            var fragCompileError = gl.CompileShaderAndGetError(_fragmentShader, FragShaderSrc);
            if (fragCompileError == null)
                Log += "frag shader compile OK\n";
            else
            {
                HasError = true;
                Log += ">gl.CompileShaderAndGetError frag:\n"
                     + fragCompileError + "\n"
                     + "<gl.CompileShaderAndGetError frag\n";
            }

            _shaderProgram = gl.CreateProgram();
            gl.AttachShader(_shaderProgram, _vertexShader);
            gl.AttachShader(_shaderProgram, _fragmentShader);
            var progLinkError = gl.LinkProgramAndGetError(_shaderProgram);
            if (progLinkError == null)
                Log += "shader program link OK\n";
            else
            {
                HasError = true;
                Log += ">gl.LinkProgramAndGetError:\n"
                     + progLinkError + "\n"
                     + "<gl.LinkProgramAndGetError\n";
            }

            CheckError(gl, "shaders compiled");

            _shadersCompiled = true;
        }

        if (HasError)
            return;

        if (!_geometryLoaded)
        {
            _vertexArrObj = gl.GenVertexArray();
            gl.BindVertexArray(_vertexArrObj);

            _vertexBufObj = gl.GenBuffer();
            gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, _vertexBufObj);
            unsafe
            {
                fixed (void* pdata = GLTriangleData.points)
                    gl.BufferData(GlConsts.GL_ARRAY_BUFFER, new IntPtr(GLTriangleData.points.Length * sizeof(float)), new IntPtr(pdata), GlConsts.GL_STATIC_DRAW);
            }

            _elemBufObj = gl.GenBuffer();
            gl.BindBuffer(GlConsts.GL_ELEMENT_ARRAY_BUFFER, _elemBufObj);
            unsafe
            {
                fixed (void* pdata = GLTriangleData.elems)
                    gl.BufferData(GlConsts.GL_ELEMENT_ARRAY_BUFFER, new IntPtr(GLTriangleData.elems.Length * sizeof(ushort)), new IntPtr(pdata), GlConsts.GL_STATIC_DRAW);
            }

            // 0 == "location = 0" in the vertex shader
            gl.VertexAttribPointer(0, 3, GlConsts.GL_FLOAT, 0, 3 * sizeof(float), IntPtr.Zero);
            gl.EnableVertexAttribArray(0);

            CheckError(gl, "geometry loaded");

            _geometryLoaded = true;
        }

        // not strictly needed
        gl.BindVertexArray(0); // (must come first)
        gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, 0);
        gl.BindBuffer(GlConsts.GL_ELEMENT_ARRAY_BUFFER, 0);

        gl.ClearColor(0.0f, 0.3f, 0.0f, 1.0f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT);

        // OpenGlControlBase implementation details mean VisualRoot is non-null here
        Debug.Assert(VisualRoot != null);
        // This mimics (private) method OpenGlControlBase.GetPixelSize
        int pixelWidth = (int)(Bounds.Width * VisualRoot.RenderScaling);
        int pixelHeight = (int)(Bounds.Height * VisualRoot.RenderScaling);
        gl.Viewport(0, 0, pixelWidth, pixelHeight);

        gl.BindVertexArray(_vertexArrObj);
        gl.UseProgram(_shaderProgram);
        gl.DrawElements(GlConsts.GL_TRIANGLES, GLTriangleData.elems.Length, GlConsts.GL_UNSIGNED_SHORT, IntPtr.Zero);

        CheckError(gl, "OnOpenGlRender end");
    }

    private void CheckError(GlInterface gl, string ctx, int? ignoredErrorIfFirst = null)
    {
        bool first = true;
        int err;
        while ((err = gl.GetError()) != GlConsts.GL_NO_ERROR)
        {
            if (first && err == ignoredErrorIfFirst)
                continue;
            first = false;
            Log += $"{ctx}: gl.GetError() -> {err}\n";
        }
    }
}
