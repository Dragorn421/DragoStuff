
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace avalonia_contextmenu_xaml;

public class MainWindowViewModel
{
    public List<MainWindowViewModelRowItem> _myDataGridItems = new() { new("a"), new("b"), new("c") };
    public List<MainWindowViewModelRowItem> MyDataGridItems => _myDataGridItems;
}

public class MainWindowViewModelRowItem
{
    public string _valueCol1;
    public string ValueCol1 => _valueCol1;
    public MainWindowViewModelRowItem(string valueCol1)
    {
        _valueCol1 = valueCol1;
    }
    public void RunThing()
    {
        Debug.WriteLine("RunThing " + ValueCol1);
    }
}
