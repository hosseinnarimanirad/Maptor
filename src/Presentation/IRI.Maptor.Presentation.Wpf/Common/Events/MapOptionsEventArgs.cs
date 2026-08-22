using System;
using System.Windows;
using IRI.Maptor.Core.Common.Abstractions;

namespace IRI.Maptor.Presentation.Wpf.Events;

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
