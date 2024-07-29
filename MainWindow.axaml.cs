using Avalonia.Controls;

namespace avalonia_glsl_investigate;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = new MainWindowVM();
        InitializeComponent();
    }
}