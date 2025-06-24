// RpaWpfComponents/AdvancedDataGrid/Commands/AsyncRelayCommand.cs
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Configuration;

namespace RpaWpfComponents.AdvancedDataGrid.Commands
{
    internal class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;
        private readonly Action<Exception>? _errorHandler;
        private readonly ILogger<AsyncRelayCommand> _logger;
        private bool _isExecuting = false;

        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null, Action<Exception>? errorHandler = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
            _logger = LoggerFactory.CreateLogger<AsyncRelayCommand>();
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
                _logger.LogDebug("Executing async command");
                await _executeAsync();
                _logger.LogDebug("Async command executed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing async command");

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

    internal class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _executeAsync;
        private readonly Func<T, bool>? _canExecute;
        private readonly Action<Exception>? _errorHandler;
        private readonly ILogger<AsyncRelayCommand<T>> _logger;
        private bool _isExecuting = false;

        public AsyncRelayCommand(Func<T, Task> executeAsync, Func<T, bool>? canExecute = null, Action<Exception>? errorHandler = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
            _logger = LoggerFactory.CreateLogger<AsyncRelayCommand<T>>();
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
                _logger.LogDebug("Executing async command with parameter of type {ParameterType}", typeof(T).Name);
                await _executeAsync(typedParameter);
                _logger.LogDebug("Async command with parameter executed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing async command with parameter");

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