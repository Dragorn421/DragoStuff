using Avalonia.Controls;
using Z64;

namespace Z64Utils_recreate_avalonia_ui;

public partial class DListViewerWindow : Window
{
    public DListViewerWindowViewModel ViewModel;

    public DListViewerWindow(Z64Game game)
    {
        ViewModel = new DListViewerWindowViewModel(game);
        DataContext = ViewModel;
        InitializeComponent();

        // FIXME
        DLViewerGL.hack_DLVWVM = ViewModel;
    }
}
