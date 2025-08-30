namespace IRI.Maptor.Jab.Common.Events;

public class CustomEventArgs<T> : System.EventArgs
{
    public T Arg { get; set; }

    public CustomEventArgs(T arg)
    {
        Arg = arg;
    }
}
