using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IsblCheck.Common.Mvvm
{
  /// <summary>
  /// View-model base with the notification method name used by the existing UI.
  /// </summary>
  public abstract class ViewModelBase : ObservableObject
  {
  }

  /// <summary>
  /// Observable object with the notification method name used by the existing UI.
  /// </summary>
  public abstract class ObservableObject : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
  {
    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.OnPropertyChanged(propertyName);
    }
  }

  /// <summary>
  /// WPF command wrapper that preserves CommandManager based requery behavior.
  /// </summary>
  public sealed class RelayCommand : ICommand
  {
    private readonly CommunityToolkit.Mvvm.Input.RelayCommand command;

    public RelayCommand(Action execute) : this(execute, null)
    {
    }

    public RelayCommand(Action execute, Func<bool> canExecute)
    {
      this.command = canExecute == null
        ? new CommunityToolkit.Mvvm.Input.RelayCommand(execute)
        : new CommunityToolkit.Mvvm.Input.RelayCommand(execute, canExecute);
    }

    public bool CanExecute(object parameter) => this.command.CanExecute(parameter);

    public void Execute(object parameter) => this.command.Execute(parameter);

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }
  }

  /// <summary>
  /// Generic WPF command wrapper that preserves CommandManager based requery behavior.
  /// </summary>
  public sealed class RelayCommand<T> : ICommand
  {
    private readonly CommunityToolkit.Mvvm.Input.RelayCommand<T> command;

    public RelayCommand(Action<T> execute) : this(execute, null)
    {
    }

    public RelayCommand(Action<T> execute, Predicate<T> canExecute)
    {
      this.command = canExecute == null
        ? new CommunityToolkit.Mvvm.Input.RelayCommand<T>(execute)
        : new CommunityToolkit.Mvvm.Input.RelayCommand<T>(execute, canExecute);
    }

    public bool CanExecute(object parameter) => this.command.CanExecute(parameter);

    public void Execute(object parameter) => this.command.Execute(parameter);

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }
  }
}
