using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace avalonia_contextmenu_xaml;

public partial class ContextMenuTemplate : ContextMenu
{

    private string _thingOptionName;
    public string ThingOptionName => _thingOptionName;

    public ContextMenuTemplate(string thingOptionName)
    {
        _thingOptionName = thingOptionName;
        AvaloniaXamlLoader.Load(this);
    }
}