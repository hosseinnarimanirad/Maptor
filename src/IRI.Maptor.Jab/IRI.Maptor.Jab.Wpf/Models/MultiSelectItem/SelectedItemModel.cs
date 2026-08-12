using IRI.Maptor.Jab.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Wpf.Models.MultiSelectItem
{
    public class SelectedItemModel<T> : Notifier
    {
        private T _value;

        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Title));
            }
        }

        //private string _title;

        public string Title
        {
            get { return _getTitleFunc(Value); }
        }

        private Func<T, string> _getTitleFunc;

        public SelectedItemModel(T value, Func<T, string> getTitleFunc)
        {
            _getTitleFunc = getTitleFunc;

            Value = value;
        }

        private RelayCommand _removeCommand;

        public RelayCommand RemoveCommand
        {
            get
            {
                if (_removeCommand == null)
                {
                    _removeCommand = new RelayCommand(param => RequestRemove());
                }
                return _removeCommand;
            }
        }

        private void RequestRemove()
        {
            OnRequestRemove?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnRequestRemove;

        public override string ToString()
        {
            return $"Title: {Title}, Value: {Value}";
        }
    }
}