using System.Windows.Input;

namespace DppDashboard.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = p => { execute(p); return Task.CompletedTask; };
            _canExecute = canExecute;
        }

        public RelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _executeAsync = _ => { execute(); return Task.CompletedTask; };
            _canExecute = canExecute != null ? _ => canExecute() : null;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter) => await _executeAsync(parameter);
    }
}
