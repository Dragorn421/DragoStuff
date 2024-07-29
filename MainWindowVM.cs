namespace avalonia_glsl_investigate;

public class MainWindowVM
{
    private ShaderTestOpenGLVM _shaderTestOpenGLDataContext = new("Avalonia GL #version 330 core", "330 core");
    public ShaderTestOpenGLVM ShaderTestOpenGLDataContext => _shaderTestOpenGLDataContext;
}