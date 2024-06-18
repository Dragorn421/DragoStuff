using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z64;

namespace Z64Utils_recreate_avalonia_ui;

public partial class MainWindowViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private Z64Game? _game;

    // this may not be needed on Windows, test eventually
    // bug on Linux? idk if this issue is relevant:
    // https://github.com/AvaloniaUI/Avalonia/issues/2958
    private bool FilePickerActive;

    // Provided by the view
    public Func<Task<IStorageFile?>>? GetOpenROM;
    public Func<Task<int?>>? PickSegmentID;
    public Func<ObjectAnalyzerWindowViewModel>? OpenObjectAnalyzer;
    public Func<DListViewerWindowViewModel>? OpenDListViewer;
    public Func<F3DZEXDisassemblerViewModel>? OpenF3DZEXDisassembler;

    public MainWindowViewModel()
    {
        PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(FilterText):
                    UpdateRomFiles();
                    break;
            }
        };
    }

    public async Task OpenROMCommand()
    {
        try
        {
            Debug.Assert(!FilePickerActive);
            FilePickerActive = true;
            IStorageFile? file;
            try
            {
                Debug.Assert(GetOpenROM != null);
                file = await GetOpenROM();
            }
            finally
            {
                FilePickerActive = false;
            }
            Logger.Debug("file={filePath}", file?.Path.ToString());

            if (file == null)
            {
                // Cancelled file open, do nothing
            }
            else
            {
                // TODO not entirely sure about this IStorageFile -> path
                string path = file.Path.LocalPath;
                OpenROM(path);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "An error occured opening the ROM");
            var ewin = new ErrorWindow();
            ewin.SetMessage("An error occured opening the ROM", e.ToString());
            ewin.Show();
            throw;
        }
    }
    // avalonia bug?: CanOpenROMCommand is not used if it takes no argument
    public bool CanOpenROMCommand(object arg)
    {
        return !FilePickerActive;
    }

    // TODO figure out how to make this method async
    // (currently hidden in Z64Game)
    public void OpenROM(string path)
    {
        ProgressText = $"new Z64Game({path})";
        _game = new Z64Game(path);
        ProgressText = "OpenROM complete";

        UpdateRomFiles();
    }

    // TODO: vvv

    public void ExportFSCommand() { }
    public bool CanExportFSCommand(object arg) { return false; }
    public void SaveAsCommand() { }
    public bool CanSaveAsCommand(object arg) { return false; }
    public void ImportFileNameListCommand() { }
    public bool CanImportFileNameListCommand(object arg) { return false; }
    public void ExportFileNameListCommand() { }
    public bool CanExportFileNameListCommand(object arg) { return false; }

    public void OpenDListViewerCommand()
    {
        Debug.Assert(OpenDListViewer != null);
        OpenDListViewer();
    }
    public void F3DZEXDisassemblerCommand()
    {
        Debug.Assert(OpenF3DZEXDisassembler != null);
        OpenF3DZEXDisassembler();
    }
    public void ROMRAMConversionsCommand() { }
    public bool CanROMRAMConversionsCommand(object arg) { return false; }
    public void TextureViewerCommand() { }
    public bool CanTextureViewerCommand(object arg) { return false; }
    public void ObjectAnalyzerCommand()
    {
        Debug.Assert(OpenObjectAnalyzer != null);
        OpenObjectAnalyzer();
    }

    public void CheckNewReleasesCommand() { }
    public void AboutCommand() { }

    //

    public ObjectAnalyzerWindowViewModel OpenObjectAnalyzerByZ64File(Z64File file, int segment)
    {
        Debug.Assert(_game != null);
        Debug.Assert(OpenObjectAnalyzer != null);
        var objectAnalyzerVM = OpenObjectAnalyzer();
        objectAnalyzerVM.SetFile(_game, file, segment);
        return objectAnalyzerVM;
    }

    public ObjectAnalyzerWindowViewModel? OpenObjectAnalyzerByFileName(string name, int segment)
    {
        Debug.Assert(_game != null);
        var file = _game.GetFileByName(name);
        if (file == null)
            return null;
        else
            return OpenObjectAnalyzerByZ64File(file, segment);
    }

    //

    [ObservableProperty]
    private string _progressText = "hey";
    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    public ObservableCollection<MainWindowViewModelRomFile> _romFiles = new();

    public void UpdateRomFiles()
    {
        RomFiles.Clear();

        if (_game == null)
            return;

        string filterText = FilterText.ToLower();

        for (int i = 0; i < _game.GetFileCount(); i++)
        {
            var file = _game.GetFileFromIndex(i);
            if (!file.Valid())
                continue;

            string name = _game.GetFileName(file.VRomStart);
            string vrom = $"{file.VRomStart:X8}-{file.VRomEnd:X8}";
            string rom = $"{file.RomStart:X8}-{file.RomEnd:X8}";
            string type = $"{_game.GetFileType(file.VRomStart)}";

            if (filterText == ""
                || name.ToLower().Contains(filterText)
                || type.ToLower().Contains(filterText)
            )
            {
                RomFiles.Add(
                    new MainWindowViewModelRomFile(this, name, vrom, rom, type, file)
                );
            }
        }
    }
}

public class MainWindowViewModelRomFile
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public string Name { get; }
    public string VROM { get; }
    public string ROM { get; }
    public string Type { get; }

    public Z64File File { get; }

    private MainWindowViewModel Mwvm { get; }

    public MainWindowViewModelRomFile(MainWindowViewModel mwvm, string name, string vrom, string rom, string type, Z64File file)
    {
        Name = name;
        VROM = vrom;
        ROM = rom;
        Type = type;
        File = file;
        Mwvm = mwvm;
    }

    public async void OpenObjectAnalyzerCommand()
    {
        Debug.Assert(Mwvm.PickSegmentID != null);
        int? segmentID = await Mwvm.PickSegmentID();
        Logger.Debug("segmentID={segmentID}", segmentID);
        if (segmentID != null)
            Mwvm.OpenObjectAnalyzerByZ64File(File, (int)segmentID);
    }
}
