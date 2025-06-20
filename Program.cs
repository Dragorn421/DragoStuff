using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace avalonia_gl_interop;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int AllocConsole();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AllocConsole();
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Console.Write(e);
            Console.ReadLine();
            throw;
        }
        Console.ReadLine();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
