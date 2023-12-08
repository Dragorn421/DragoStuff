using Avalonia.Controls;
using Avalonia.Interactivity;

namespace avalonia_datagrid_perf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    public void DataButtonClick(object? sender, RoutedEventArgs args)
    {
        (DataContext as MainWindowViewModel).ShowData();
    }
    public void DataNoIsVisibleBindingButtonClick(object? sender, RoutedEventArgs args)
    {
        (DataContext as MainWindowViewModel).ShowDataNoIsVisibleBinding();
    }
    public void TextButtonClick(object? sender, RoutedEventArgs args)
    {
        (DataContext as MainWindowViewModel).ShowText();
    }
}
