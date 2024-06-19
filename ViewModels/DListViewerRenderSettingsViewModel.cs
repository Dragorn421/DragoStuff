using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using F3DZEX.Render;

namespace Z64Utils_Avalonia;

public partial class DListViewerRenderSettingsViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    // TODO surely there is a better way to handle a ComboBox-enum... (for RenderMode)
    public class RenderModeValue
    {
        public string RenderModeName { get; }
        public RdpVertexDrawer.ModelRenderMode RenderModeEnum { get; }
        public RenderModeValue(RdpVertexDrawer.ModelRenderMode renderModeEnum)
        {
            RenderModeName = Enum.GetName(renderModeEnum) ?? "?";
            RenderModeEnum = renderModeEnum;
        }
    }
    private IEnumerable<RenderModeValue> _renderModeValues = new List<RenderModeValue> {
        new(RdpVertexDrawer.ModelRenderMode.Wireframe),
        new(RdpVertexDrawer.ModelRenderMode.Textured),
        new(RdpVertexDrawer.ModelRenderMode.Surface),
        new(RdpVertexDrawer.ModelRenderMode.Normal),
    };
    public IEnumerable<RenderModeValue> RenderModeValues => _renderModeValues;

    [ObservableProperty] private float _gridScale;
    [ObservableProperty] private bool _showGrid;
    [ObservableProperty] private bool _showAxis;
    [ObservableProperty] private bool _showGLInfo;
    [ObservableProperty] private RdpVertexDrawer.ModelRenderMode _renderMode;
    [ObservableProperty] private bool _enabledLighting;
    [ObservableProperty] private bool _drawNormals;
    [ObservableProperty] private Avalonia.Media.Color _normalColor;
    [ObservableProperty] private Avalonia.Media.Color _highlightColor;
    [ObservableProperty] private Avalonia.Media.Color _wireframeColor;
    [ObservableProperty] private Avalonia.Media.Color _backColor;
    [ObservableProperty] private Avalonia.Media.Color _initialPrimColor;
    [ObservableProperty] private Avalonia.Media.Color _initialEnvColor;
    [ObservableProperty] private Avalonia.Media.Color _initialFogColor;
    [ObservableProperty] private Avalonia.Media.Color _initialBlendColor;

    [ObservableProperty]
    private Renderer.Config _rendererConfig;

    private bool _recursionFlag = false;

    public DListViewerRenderSettingsViewModel()
    {
        PropertyChanged += (sender, e) =>
        {
            // _recursionFlag prevents only partially loading the initial RendererConfig due to recursive propchanged
            // TODO handle better...
            if (_recursionFlag)
                return;
            try
            {
                _recursionFlag = true;
                switch (e.PropertyName)
                {
                    case nameof(GridScale):
                    case nameof(ShowGrid):
                    case nameof(ShowAxis):
                    case nameof(ShowGLInfo):
                    case nameof(RenderMode):
                    case nameof(EnabledLighting):
                    case nameof(DrawNormals):
                    case nameof(NormalColor):
                    case nameof(HighlightColor):
                    case nameof(WireframeColor):
                    case nameof(BackColor):
                    case nameof(InitialPrimColor):
                    case nameof(InitialEnvColor):
                    case nameof(InitialFogColor):
                    case nameof(InitialBlendColor):
                        Debug.WriteLine($"single prop {e.PropertyName} changed");
                        RendererConfig = new()
                        {
                            GridScale = GridScale,
                            ShowGrid = ShowGrid,
                            ShowAxis = ShowAxis,
                            ShowGLInfo = ShowGLInfo,
                            RenderMode = RenderMode,
                            EnabledLighting = EnabledLighting,
                            DrawNormals = DrawNormals,
                            NormalColor = AvaloniaToBuiltinColor(NormalColor),
                            HighlightColor = AvaloniaToBuiltinColor(HighlightColor),
                            WireframeColor = AvaloniaToBuiltinColor(WireframeColor),
                            BackColor = AvaloniaToBuiltinColor(BackColor),
                            InitialPrimColor = AvaloniaToBuiltinColor(InitialPrimColor),
                            InitialEnvColor = AvaloniaToBuiltinColor(InitialEnvColor),
                            InitialFogColor = AvaloniaToBuiltinColor(InitialFogColor),
                            InitialBlendColor = AvaloniaToBuiltinColor(InitialBlendColor),
                        };
                        Debug.WriteLine($"set RendererConfig to {RendererConfig}");
                        break;
                    case nameof(RendererConfig):
                        Debug.WriteLine($"loading from RendererConfig={RendererConfig}");
                        GridScale = RendererConfig.GridScale;
                        ShowGrid = RendererConfig.ShowGrid;
                        ShowAxis = RendererConfig.ShowAxis;
                        ShowGLInfo = RendererConfig.ShowGLInfo;
                        RenderMode = RendererConfig.RenderMode;
                        EnabledLighting = RendererConfig.EnabledLighting;
                        DrawNormals = RendererConfig.DrawNormals;
                        NormalColor = BuiltinToAvaloniaColor(RendererConfig.NormalColor);
                        HighlightColor = BuiltinToAvaloniaColor(RendererConfig.HighlightColor);
                        WireframeColor = BuiltinToAvaloniaColor(RendererConfig.WireframeColor);
                        BackColor = BuiltinToAvaloniaColor(RendererConfig.BackColor);
                        InitialPrimColor = BuiltinToAvaloniaColor(RendererConfig.InitialPrimColor);
                        InitialEnvColor = BuiltinToAvaloniaColor(RendererConfig.InitialEnvColor);
                        InitialFogColor = BuiltinToAvaloniaColor(RendererConfig.InitialFogColor);
                        InitialBlendColor = BuiltinToAvaloniaColor(RendererConfig.InitialBlendColor);
                        Debug.WriteLine("done set from RendererConfig");
                        break;
                }
            }
            finally
            {
                _recursionFlag = false;
            }
        };
        RendererConfig = new();
    }

    public System.Drawing.Color AvaloniaToBuiltinColor(Avalonia.Media.Color c)
    {
        return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
    public Avalonia.Media.Color BuiltinToAvaloniaColor(System.Drawing.Color c)
    {
        return Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
