using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace avalonia_gl_minimal;

public partial class App : Application
{
    public static bool DoRequestNextFrameRendering = false;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Debug.Assert(desktop.Args != null);
            Debug.Assert(desktop.Args.Length == 1);
            switch (desktop.Args[0])
            {
                case "RequestNextFrameRenderingYes":
                    DoRequestNextFrameRendering = true;
                    break;
                case "RequestNextFrameRenderingNo":
                    DoRequestNextFrameRendering = false;
                    break;
                default:
                    throw new Exception("unknown argument");
            }
            Console.WriteLine($"DoRequestNextFrameRendering={DoRequestNextFrameRendering}");

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
