using System;

namespace IRI.Maptor.Jab.Common.Events;

public class CallbackEventArgs<T> : EventArgs
{
    public Action Callback { get; private set; }

    public T Arg { get; set; }

    public CallbackEventArgs(T arg, Action callback)
    {
        Arg = arg;

        Callback = callback;
    }
}
