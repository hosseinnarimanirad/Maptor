using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace IRI.Maptor.Jab.Core;

public class Notifier : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public void VerifyPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return;

        if (TypeDescriptor.GetProperties(this)[propertyName] == null)
        {
            string msg = "Invalid property name: " + propertyName;
            Debug.Fail(msg);
        }
    }

    public void NotifyAllProperties()
    {
        var properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var prop in properties)
        {
            RaisePropertyChanged(prop.Name);
        }
    }
}
