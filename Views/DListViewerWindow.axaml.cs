using Avalonia.Controls;

namespace Z64Utils_Avalonia;

public partial class DListViewerWindow : Window
{
    public DListViewerWindowViewModel ViewModel;

    private DListViewerRenderSettingsWindow? _currentRenderSettingsWindow;

    public DListViewerWindow()
    {
        ViewModel = new()
        {
            OpenDListViewerRenderSettings = OpenDListViewerRenderSettings
        };
        DataContext = ViewModel;
        InitializeComponent();
        ViewModel.RenderContextChanged += (sender, e) =>
        {
            DLViewerGL.RequestNextFrameRenderingIfInitialized();
        };
    }

    private DListViewerRenderSettingsViewModel? OpenDListViewerRenderSettings()
    {
        if (_currentRenderSettingsWindow != null)
        {
            _currentRenderSettingsWindow.Activate();
            return null;
        }

        _currentRenderSettingsWindow = new DListViewerRenderSettingsWindow();
        _currentRenderSettingsWindow.Closed += (sender, e) =>
        {
            _currentRenderSettingsWindow = null;
        };
        _currentRenderSettingsWindow.Show();
        return _currentRenderSettingsWindow.ViewModel;
    }
}
