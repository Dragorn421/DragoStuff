// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

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