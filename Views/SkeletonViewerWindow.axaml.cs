using System.Diagnostics;
using Avalonia.Controls;

namespace Z64Utils_recreate_avalonia_ui;

public partial class SkeletonViewerWindow : Window
{
    public SkeletonViewerWindowViewModel ViewModel;

    public SkeletonViewerWindow()
    {
        ViewModel = new();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public void OnAnimationEntriesDataGridSelectionChanged(object? sender, SelectionChangedEventArgs ev)
    {
        var selectedItem = AnimationEntriesDataGrid.SelectedItem;
        if (selectedItem == null)
            return;
        Debug.Assert(selectedItem is SkeletonViewerWindowViewModel.AnimationEntry);
        var animationEntry = (SkeletonViewerWindowViewModel.AnimationEntry)selectedItem;
        ViewModel.OnAnimationEntrySelected(animationEntry);
    }
}
