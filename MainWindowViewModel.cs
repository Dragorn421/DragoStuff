using System;
using System.ComponentModel;

namespace avalonia_datagrid_perf;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private string _text = "";
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            if (PropertyChanged != null)
                PropertyChanged(this, new(nameof(Text)));
        }
    }

    private object? _subViewModel;
    public object? SubViewModel
    {
        get => _subViewModel;
        set
        {
            _subViewModel = value;
            if (PropertyChanged != null)
                PropertyChanged(this, new(nameof(SubViewModel)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowData()
    {
        var t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        SubViewModel = new DataGridViewModel();
        var t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var dt = t2 - t1;
        Text = $"data in {dt}ms";
    }
    public void ShowDataNoIsVisibleBinding()
    {
        var t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        SubViewModel = new DataGridViewModelNoIsVisibleBinding();
        var t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var dt = t2 - t1;
        Text = $"data NoIsVisibleBinding in {dt}ms";
    }

    public void ShowText()
    {
        var t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        SubViewModel = new TextViewModel();
        var t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var dt = t2 - t1;
        Text = $"text in {dt}ms";
    }
}
