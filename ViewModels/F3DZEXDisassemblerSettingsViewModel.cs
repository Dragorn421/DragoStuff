using System;
using System.Diagnostics;
using System.IO;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using F3DZEX.Command;

namespace Z64Utils_recreate_avalonia_ui;

public partial class F3DZEXDisassemblerSettingsViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private bool _showAddress;
    [ObservableProperty]
    private bool _relativeAddress;
    [ObservableProperty]
    private bool _disasMultiCmdMacro;
    [ObservableProperty]
    private bool _addressLiteral;
    [ObservableProperty]
    private bool _static;

    [ObservableProperty]
    private F3DZEX.Disassembler.Config _disasConfig;

    [ObservableProperty]
    private string _outputDisasPreview = "";

    public F3DZEXDisassemblerSettingsViewModel()
    {
        PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(ShowAddress):
                case nameof(RelativeAddress):
                case nameof(DisasMultiCmdMacro):
                case nameof(AddressLiteral):
                case nameof(Static):
                    DisasConfig = new()
                    {
                        ShowAddress = ShowAddress,
                        RelativeAddress = RelativeAddress,
                        DisasMultiCmdMacro = DisasMultiCmdMacro,
                        AddressLiteral = AddressLiteral,
                        Static = Static,
                    };
                    UpdateDisassemblyPreview();
                    break;
                case nameof(DisasConfig):
                    ShowAddress = DisasConfig.ShowAddress;
                    RelativeAddress = DisasConfig.RelativeAddress;
                    DisasMultiCmdMacro = DisasConfig.DisasMultiCmdMacro;
                    AddressLiteral = DisasConfig.AddressLiteral;
                    Static = DisasConfig.Static;
                    UpdateDisassemblyPreview();
                    break;
            }
        };
        DisasConfig = new();
    }

    public void UpdateDisassemblyPreview()
    {
        var dlistBytes = new byte[] { 0x01, 0x01, 0x20, 0x24, 0x06, 0x00, 0x0F, 0xC8 };
        var dlist = new Dlist(dlistBytes, 0x060002C8);
        F3DZEX.Disassembler disas = new F3DZEX.Disassembler(dlist, DisasConfig);
        var lines = disas.Disassemble();
        StringWriter sw = new StringWriter();
        foreach (var line in lines)
            sw.Write($"{line}\n");

        OutputDisasPreview = sw.ToString();
    }
}
