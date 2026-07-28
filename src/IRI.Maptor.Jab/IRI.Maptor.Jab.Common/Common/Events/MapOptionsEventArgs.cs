using System;
using System.Windows;
using IRI.Maptor.Sta.Common.Abstractions;

namespace IRI.Maptor.Jab.Common.Events;

public class MapOptionsEventArgs<T> : EventArgs where T : FrameworkElement, new()
{
    public T View { get; set; }

    public ILocatable DataContext { get; set; }

    public MapOptionsEventArgs(T view, ILocatable dataContext)
    {
        View = view;

        DataContext = dataContext;
    }
}
