using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace avalonia_command_exception;

public partial class MainWindowViewModel
{
    public void ClickMeCommand()
    {
        throw new Exception();
    }

    public async void ClickMeAsyncVoidCommand()
    {
        throw new Exception();
    }

    public async Task ClickMeAsyncTaskCommand()
    {
        throw new Exception();
    }

    public async void ClickMeAsyncVoidWaitCommand()
    {
        await Task.Delay(100);
        throw new Exception();
    }

    public async Task ClickMeAsyncTaskWaitCommand()
    {
        await Task.Delay(100);
        throw new Exception();
    }

    [RelayCommand]
    public async Task ClickMeAsyncICommand()
    {
        await Task.Delay(100);
        throw new Exception();
    }
}
