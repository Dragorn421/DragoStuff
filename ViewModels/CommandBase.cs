using System;
using System.Diagnostics;
using System.Windows.Input;

// TODO MVVM toolkit has RelayCommand which is basically this

public class CommandBase : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public CommandBase(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null)
            return true;
        else
            return _canExecute();
    }

    public void Execute(object? parameter)
    {
        _execute();
    }
}

public class CommandBase<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public CommandBase(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    private T CastParameter(object? parameter)
    {
        Debug.Assert(parameter != null);
        Debug.Assert(parameter is T);
        T parameterT = (T)parameter;
        return parameterT;
    }

    public bool CanExecute(object? parameter)
    {
        T parameterT = CastParameter(parameter);
        if (_canExecute == null)
            return true;
        else
            return _canExecute(parameterT);
    }

    public void Execute(object? parameter)
    {
        T parameterT = CastParameter(parameter);
        _execute(parameterT);
    }
}
