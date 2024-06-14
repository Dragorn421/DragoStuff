using Avalonia;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Z64Utils_recreate_avalonia_ui;

class Program
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static readonly string Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "UnknownVersion";

    public const int EXIT_SUCCESS = 0;

    public class ParsedArgsData
    {
        public FileInfo? RomFile;
        public string[]? ObjectAnalyzerFileNames;
        public string? DListViewerOHEName;
        public string? SkeletonViewerOHEName;
    }

    public static ParsedArgsData? ParsedArgs = null;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Logger.Info("Starting up Z64Utils Version {Version}...", Version);

            int code = HandleArgs(args);
            if (code != EXIT_SUCCESS)
                return code;

            BuildAvaloniaApp()
               .StartWithClassicDesktopLifetime(args);
            return EXIT_SUCCESS;
        }
        catch (Exception e)
        {
            Logger.Fatal(e);
            throw;
        }
        finally
        {
            NLog.LogManager.Shutdown();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static int HandleArgs(string[] args)
    {
        // To enable tab-completion in your shell see:
        // https://learn.microsoft.com/en-us/dotnet/standard/commandline/tab-completion

        var romFileOption = new Option<FileInfo?>(
            name: "--rom",
            description: "The ROM to load.",
            parseArgument: result =>
            {
                string filePath = result.Tokens.Single().Value;
                var fi = new FileInfo(filePath);
                if (fi.Exists)
                {
                    return fi;
                }
                else
                {
                    result.ErrorMessage = $"ROM \"{fi.FullName}\" does not exist";
                    return null;
                }
            }
        );
        var objectAnalyzerOption = new Option<string[]>(
            name: "--object-analyzer",
            description: "Open files in the ROM, by name, in the object analyzer."
        );
        var dListViewerOption = new Option<string>(
            name: "--dlist-viewer",
            // TODO should really be by offset but it's impractical with the exposed Z64Object info
            description: "Open a DList in the object, by name, in the dlist viewer."
        );
        var skeletonViewerOption = new Option<string>(
            name: "--skeleton-viewer",
            // TODO should really be by offset but it's impractical with the exposed Z64Object info
            description: "Open a skeleton in the object, by name, in the skeleton viewer."
        );

        var rootCommand = new RootCommand("Z64Utils");
        rootCommand.AddOption(romFileOption);
        rootCommand.AddOption(objectAnalyzerOption);
        rootCommand.AddOption(dListViewerOption);
        rootCommand.AddOption(skeletonViewerOption);

        ParsedArgs = new();
        rootCommand.SetHandler(
            (file, names, dlName, skeletonName) =>
            {
                ParsedArgs.RomFile = file;
                ParsedArgs.ObjectAnalyzerFileNames = names;
                ParsedArgs.DListViewerOHEName = dlName;
                ParsedArgs.SkeletonViewerOHEName = skeletonName;
            },
            romFileOption, objectAnalyzerOption, dListViewerOption, skeletonViewerOption);

        int code = rootCommand.Invoke(args);
        return code;
    }
}
