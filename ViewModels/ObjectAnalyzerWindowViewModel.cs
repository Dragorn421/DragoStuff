
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using RDP;
using Z64;

namespace Z64Utils_recreate_avalonia_ui;

public partial class ObjectAnalyzerWindowViewModel : ObservableObject
{
    public Z64Game? _game; // FIXME private after cleanup
    private Z64File? _file;
    private int _segment;
    private Z64Object? _object;

    static public string DEFAULT_WINDOW_TITLE = "Object Analyzer";
    [ObservableProperty]
    public string _windowTitle = DEFAULT_WINDOW_TITLE;

    // Provided by the view
    public Func<DListViewerWindowViewModel>? OpenDListViewer;

    public ICommand OpenDListViewerObjectHolderEntryCommand;

    public ObjectAnalyzerWindowViewModel()
    {
        OpenDListViewerObjectHolderEntryCommand = new CommandBase<ObjectHolderEntry>(OpenDListViewerObjectHolderEntryCommandExecute);
    }

    // TODO: vvv

    public void FindDListsCommand()
    {
        Debug.Assert(HasFile());
        Debug.Assert(_object != null);
        Debug.Assert(_file != null);

        var config = new Z64ObjectAnalyzer.Config(); // TODO
        Z64ObjectAnalyzer.FindDlists(_object, _file.Data, _segment, config);
        UpdateMap();
    }
    public bool CanFindDListsCommand(object? parameter)
    {
        return HasFile();
    }

    public void AnalyzeDListsCommand()
    {
        Debug.Assert(HasFile());
        Debug.Assert(_object != null);
        Debug.Assert(_file != null);

        Z64ObjectAnalyzer.AnalyzeDlists(_object, _file.Data, _segment);
        UpdateMap();
    }
    public bool CanAnalyzeDListsCommand(object? parameter)
    {
        return HasFile();
    }

    public void ImportJSONCommand() { }
    public void ExportJSONCommand() { }

    public void ResetCommand()
    {
        Debug.Assert(HasFile());
        Debug.Assert(_object != null);
        Debug.Assert(_file != null);

        _object.Entries.Clear();
        _object.AddUnknow(_file.Data.Length);
        _object.SetData(_file.Data);
        UpdateMap();
    }
    public bool CanResetCommand(object? parameter)
    {
        return HasFile();
    }

    public void DisassemblySettingsCommand() { }

    //

    [ObservableProperty]
    public ObservableCollection<ObjectHolderEntry> _objectHolderEntries = new(new List<ObjectHolderEntry>());

    public class ObjectHolderEntry
    {
        public string Offset { get; }
        public string Name { get; }
        public string Type { get; }
        public Z64Object.ObjectHolder ObjectHolder { get; }

        public ObjectHolderEntry(string offset, string name, string type, Z64Object.ObjectHolder objectHolder)
        {
            Offset = offset;
            Name = name;
            Type = type;
            ObjectHolder = objectHolder;
        }
    }

    [ObservableProperty]
    private IObjectHolderEntryDetailsViewModel? _objectHolderEntryDetailsViewModel = new EmptyOHEDViewModel();

    public void ClearFile()
    {
        WindowTitle = DEFAULT_WINDOW_TITLE;
        ObjectHolderEntryDetailsViewModel = new EmptyOHEDViewModel();
        ObjectHolderEntries.Clear();
        _game = null;
        _file = null;
        _segment = 0;
        _object = null;
    }

    public bool HasFile()
    {
        if (_file == null)
        {
            Debug.Assert(_game == null);
            Debug.Assert(_object == null);
            return false;
        }
        else
        {
            Debug.Assert(_game != null);
            Debug.Assert(_object != null);
            return true;
        }
    }

    public void SetFile(Z64Game game, Z64File file, int segment)
    {
        ClearFile();

        try
        {
            string fileName = game.GetFileName(file.VRomStart);
            WindowTitle = $"\"{fileName}\" ({file.VRomStart:X8}-{file.VRomEnd:X8})";

            _game = game;
            _file = file;
            _segment = segment;

            _object = new Z64Object(game, file.Data, fileName);

            UpdateMap();
        }
        catch
        {
            ClearFile();
            throw;
        }
    }

    private void UpdateMap()
    {
        Debug.Assert(_object != null);

        // TODO handle this better
        ObjectHolderEntryDetailsViewModel = new EmptyOHEDViewModel();

        var newObjectHolderEntries = new List<ObjectHolderEntry>();

        for (int i = 0; i < _object.Entries.Count; i++)
        {
            var entry = _object.Entries[i];
            var addr = new SegmentedAddress(_segment, _object.OffsetOf(entry));
            string addrStr = $"{addr.VAddr:X8}";

            newObjectHolderEntries.Add(
                new ObjectHolderEntry(
                    offset: addrStr,
                    name: entry.Name,
                    type: entry.GetEntryType().ToString(),
                    objectHolder: entry
                )
            );
        }

        ObjectHolderEntries = new(newObjectHolderEntries);
    }

    public void OnObjectHolderEntrySelected(ObjectHolderEntry ohe)
    {
        switch (ohe.ObjectHolder.GetEntryType())
        {
            case Z64Object.EntryType.Texture:
                var textureHolder = (Z64Object.TextureHolder)ohe.ObjectHolder;
                var bitmap = textureHolder.GetBitmap().ToAvaloniaBitmap();
                var imageVM = new ImageOHEDViewModel()
                {
                    Image = bitmap,
                };
                ObjectHolderEntryDetailsViewModel = imageVM;
                break;

            default:
                var textVM = new TextOHEDViewModel()
                {
                    Text = "OnObjectHolderEntrySelected " + ohe.ObjectHolder.Name,
                };
                ObjectHolderEntryDetailsViewModel = textVM;
                break;
        }
    }

    static private F3DZEX.Memory.Segment EMPTY_DLIST_SEGMENT = F3DZEX.Memory.Segment.FromFill("Empty Dlist", new byte[] { 0xDF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

    private void OpenDListViewerObjectHolderEntryCommandExecute(ObjectHolderEntry ohe)
    {
        Debug.Assert(OpenDListViewer != null);
        var dlvVM = OpenDListViewer();
        // TODO
        dlvVM.SomeTextForNow = "soon tm view of DL " + ohe.ObjectHolder.Name;
        Debug.Assert(_file != null);
        dlvVM.SetSegment(6, F3DZEX.Memory.Segment.FromBytes("[this object]", _file.Data));
        dlvVM.SetSegment(8, EMPTY_DLIST_SEGMENT);
        Debug.Assert(_object != null);
        dlvVM.SetSingleDlist(new SegmentedAddress(6, _object.OffsetOf(ohe.ObjectHolder)).VAddr);
    }
}
