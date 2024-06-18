using Avalonia.Controls;

namespace Z64Utils_recreate_avalonia_ui;

public partial class DListViewerRenderSettingsWindow : Window
{
    public DListViewerRenderSettingsViewModel ViewModel { get; }

    public DListViewerRenderSettingsWindow()
    {
        ViewModel = new();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
