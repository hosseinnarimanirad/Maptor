using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IRI.Maptor.Presentation.Maui.Mvvm;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base class so consumers can write
/// MVVM view models without taking a dependency on CommunityToolkit.Mvvm.
/// </summary>
public abstract class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
