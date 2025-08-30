using System;

namespace IRI.Maptor.Jab.Common.Events;

public class ChangeEventArgs<T> : EventArgs
{
    public T OldValue { get; private set; }

    public T NewValue { get; private set; }

    public ChangeEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;

        NewValue = newValue;
    }
}
