using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Z64Utils_recreate_avalonia_ui;

public partial class PickSegmentIDWindow : Window
{
    public PickSegmentIDWindowViewModel ViewModel;

    public PickSegmentIDWindow()
    {
        ViewModel = new PickSegmentIDWindowViewModel();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public void OnOKButtonClick(object? sender, RoutedEventArgs args)
    {
        Close(ViewModel.SegmentID);
    }
}
