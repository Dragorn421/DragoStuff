using Avalonia.Controls;

namespace Z64Utils_recreate_avalonia_ui;

public partial class DListViewerWindow : Window
{
    public DListViewerWindowViewModel ViewModel;

    public DListViewerWindow(DListViewerWindowViewModel vm)
    {
        ViewModel = vm;
        DataContext = ViewModel;
        InitializeComponent();
    }
}
