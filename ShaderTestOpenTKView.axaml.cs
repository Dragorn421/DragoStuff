// SPDX-FileCopyrightText: 2024 Dragorn421
// SPDX-License-Identifier: CC0-1.0

using Avalonia.Controls;

namespace avalonia_glsl_investigate;

public partial class ShaderTestOpenTKView : UserControl
{
    public ShaderTestOpenTKView()
    {
        // The DataContext is expected to be of a certain type
        // by default / initially the DataContext is inherited from the parent which
        // causes errors since it's not the expected type
        // (even in the presence of a binding to a DataContext of the right type)
        // It's unclear to me at which point it is guaranteed the binding value is
        // applied, so use as here in case it's already set. Otherwise this nulls the
        // DataContext and the binding will apply later
        var viewModel = DataContext as ShaderTestOpenGLTKVM;
        DataContext = viewModel;
        InitializeComponent();
    }
}