using System;
using System.Diagnostics;
using System.Windows.Input;

namespace IRI.Maptor.Jab.Common
{
    /// <summary>
    /// A command whose sole purpose is to 
    /// relay its functionality to other
    /// objects by invoking delegates. The
    /// default return value for the CanExecute
    /// method is 'true'.
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Fields

        readonly Action<object?> _execute;
        readonly Predicate<object?>? _canExecute;
        private EventHandler? _canExecuteChanged;

        #endregion // Fields

        #region Constructors

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<object?> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion  

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);


        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }


        #endregion // ICommand Members

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}