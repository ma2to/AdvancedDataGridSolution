// Commands/AsyncRelayCommand.cs - VYLEPŠENÝ
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Components.AdvancedDataGrid.Commands
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;
        private readonly Action<Exception>? _errorHandler;
        private bool _isExecuting = false;

        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null, Action<Exception>? errorHandler = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _executeAsync();
            }
            catch (Exception ex)
            {
                // Use custom error handler if provided, otherwise log to debug
                if (_errorHandler != null)
                {
                    _errorHandler(ex);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                }
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _executeAsync;
        private readonly Func<T, bool>? _canExecute;
        private readonly Action<Exception>? _errorHandler;
        private bool _isExecuting = false;

        public AsyncRelayCommand(Func<T, Task> executeAsync, Func<T, bool>? canExecute = null, Action<Exception>? errorHandler = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            T typedParameter = parameter is T tp ? tp : default(T)!;
            return !_isExecuting && (_canExecute?.Invoke(typedParameter) ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                T typedParameter = parameter is T tp ? tp : default(T)!;
                await _executeAsync(typedParameter);
            }
            catch (Exception ex)
            {
                // Use custom error handler if provided, otherwise log to debug
                if (_errorHandler != null)
                {
                    _errorHandler(ex);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand<T> error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                }
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}