// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using System.IO;

namespace avalonia_glsl_investigate;

public static class GLTriangleData
{
    public static float[] points = {
         0.5f,  0.5f, 0.0f,
         0.5f, -0.5f, 0.0f,
        -0.5f,  0.5f, 0.0f,
    };
    public static ushort[] elems = {
        0, 1, 2
    };

    private static string VertShaderSrcTemplate = File.ReadAllText("shaders/shader.vert");
    private static string FragShaderSrcTemplate = File.ReadAllText("shaders/shader.frag");

    public static string GetVertShaderSrc(string version)
    {
        return VertShaderSrcTemplate.Replace("MAGIC_VERSION", version);
    }
    public static string GetFragShaderSrc(string version)
    {
        return FragShaderSrcTemplate.Replace("MAGIC_VERSION", version);
    }
}
