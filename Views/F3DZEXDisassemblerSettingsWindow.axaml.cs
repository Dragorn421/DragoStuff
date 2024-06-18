using Avalonia.Controls;

namespace Z64Utils_recreate_avalonia_ui;

public partial class F3DZEXDisassemblerSettingsWindow : Window
{
    public F3DZEXDisassemblerSettingsViewModel ViewModel { get; }

    public F3DZEXDisassemblerSettingsWindow()
    {
        ViewModel = new();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
