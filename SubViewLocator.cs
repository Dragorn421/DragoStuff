using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace avalonia_datagrid_perf;

public class SubViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is DataGridViewModel)
            return new DataGridView();
        if (param is DataGridViewModelNoIsVisibleBinding)
            return new DataGridViewNoIsVisibleBinding();
        if (param is TextViewModel)
            return new TextView();
        return null;
    }

    public bool Match(object? data)
    {
        return data is DataGridViewModel || data is DataGridViewModelNoIsVisibleBinding || data is TextViewModel;
    }
}
