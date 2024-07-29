// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using Avalonia;
using System;
using System.Collections.Generic;

namespace avalonia_glsl_investigate;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .With(new Win32PlatformOptions()
            {
                RenderingMode = new List<Win32RenderingMode>()
                {
                    // Replace using ANGLE with native OpenGL
                    // However this is "not recommended"
                    // https://github.com/AvaloniaUI/Avalonia/discussions/6396
                    // https://github.com/AvaloniaUI/Avalonia/discussions/9393
                    // A better option would be to keep Avalonia rendering with whatever it wants,
                    // and use OpenGL to render to a surface that we would then pass back to Avalonia
                    // https://github.com/AvaloniaUI/Avalonia/discussions/5432
                    // ? https://github.com/AvaloniaUI/Avalonia/discussions/6842
                    // ? https://github.com/AvaloniaUI/Avalonia/pull/9639
                    // https://github.com/AvaloniaUI/Avalonia/discussions/16188
                    Win32RenderingMode.Wgl,
                    Win32RenderingMode.Software,
                }
            })
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
