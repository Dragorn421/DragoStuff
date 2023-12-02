
using CommunityToolkit.Mvvm.ComponentModel;

namespace Z64Utils_recreate_avalonia_ui;

public partial class TextOHEDViewModel : ObservableObject, IObjectHolderEntryDetailsViewModel
{
    [ObservableProperty]
    private string _text = "";
}
