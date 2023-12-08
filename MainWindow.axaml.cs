using System;
using Avalonia.Controls;

namespace avalonia_gl_minimal;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (App.DoRequestNextFrameRendering)
        {
            Console.WriteLine("GL1.RequestNextFrameRendering()");
            GL1.RequestNextFrameRendering();
        }
    }
}
