using IRI.Maptor.Jab.Common.Events;
using System;

namespace IRI.Maptor.Jab.Common.Presenters;

public abstract class DialogViewModelBase : BasePresenter
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
