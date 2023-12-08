using System.Collections.ObjectModel;

namespace avalonia_datagrid_perf;

public class DataGridViewModelNoIsVisibleBinding
{
    public class ValsDataEntry
    {
        public int Val0 { get => 0; }
    }
    private ObservableCollection<ValsDataEntry> _valsData = new();
    public ObservableCollection<ValsDataEntry> ValsData { get => _valsData; }
}
