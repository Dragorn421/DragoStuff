using CommunityToolkit.Mvvm.ComponentModel;

namespace Z64Utils_recreate_avalonia_ui;

public partial class HexViewModel : ObservableObject, IObjectHolderEntryDetailsViewModel
{
    [ObservableProperty]
    private byte[]? _dataBytes;
    [ObservableProperty]
    private uint _firstByteAddress;
}
