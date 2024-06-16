
using System.Collections.Generic;
using System.Dynamic;

namespace avalonia_contextmenu_xaml;

public class MainWindowViewModel
{
    public class RowItem
    {
        public string _valueCol1;
        public string ValueCol1 => _valueCol1;
        public RowItem(string valueCol1)
        {
            _valueCol1 = valueCol1;
        }
    }

    public List<RowItem> _myDataGridItems = new() { new("a"), new("b"), new("c") };
    public List<RowItem> MyDataGridItems => _myDataGridItems;
}
