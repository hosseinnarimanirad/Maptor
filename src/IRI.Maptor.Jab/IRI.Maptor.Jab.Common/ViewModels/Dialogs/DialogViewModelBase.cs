using System;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Jab.Common.ViewModels.Dialogs;

public abstract class DialogViewModelBase : ViewModelBase
{
    private bool? _dialogResult = false;

    public bool? DialogResult
    {
        get
        {
            return _dialogResult;
        }
        protected set
        {
            _dialogResult = value;

            OnSetResult?.Invoke(this, new CustomEventArgs<bool?>(value));
        }
    }

    public event EventHandler<CustomEventArgs<bool?>> OnSetResult;
}
