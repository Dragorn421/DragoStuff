
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Z64Utils_recreate_avalonia_ui;

public partial class ImageOHEDViewModel : ObservableObject, IObjectHolderEntryDetailsViewModel
{
    [ObservableProperty]
    private IImage? _image;
}
