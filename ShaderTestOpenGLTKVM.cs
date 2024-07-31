// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using System.ComponentModel;

namespace avalonia_glsl_investigate;

public class ShaderTestOpenGLTKVM : INotifyPropertyChanged
{
    public string Label { get; }
    public string VertShaderSrc { get; }
    public string FragShaderSrc { get; }
    private string _log = "";
    public string Log { get => _log; set { _log = value; PropertyChanged?.Invoke(this, new(nameof(Log))); } }
    private bool _hasError = false;
    public bool HasError { get => _hasError; set { _hasError = value; PropertyChanged?.Invoke(this, new(nameof(HasError))); } }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ShaderTestOpenGLTKVM(string label, string glslVersion = "330 core")
    {
        Label = label;
        VertShaderSrc = GLTriangleData.GetVertShaderSrc(glslVersion);
        FragShaderSrc = GLTriangleData.GetFragShaderSrc(glslVersion);
    }
}