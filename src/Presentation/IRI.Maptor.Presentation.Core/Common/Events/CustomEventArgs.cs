namespace IRI.Maptor.Presentation.Core;

public class CustomEventArgs<T> : EventArgs
{
    public T Arg { get; set; }

    public CustomEventArgs(T arg)
    {
        Arg = arg;
    }
}
