using System;
using System.Windows;
using IRI.Maptor.Sta.Common.Abstrations;

namespace IRI.Maptor.Jab.Common.Events;

public class MapOptionsEventArgs<T> : EventArgs where T : FrameworkElement, new()
{
    public T View { get; set; }

    public ILocateable DataContext { get; set; }

    public MapOptionsEventArgs(T view, ILocateable dataContext)
    {
        View = view;

        DataContext = dataContext;
    }
}
