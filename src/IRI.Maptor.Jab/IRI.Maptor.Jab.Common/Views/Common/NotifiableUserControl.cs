using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.CompilerServices;

namespace IRI.Maptor.Jab.Common;

public class NotifiableUserControl : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        this.VerifyPropertyName(propertyName);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public void VerifyPropertyName(string propertyName)
    {
        // If you raise PropertyChanged and do not specify a property name,
        // all properties on the object are considered to be changed by the binding system.
        if (String.IsNullOrEmpty(propertyName))
            return;

        // Verify that the property name matches a real,  
        // public, instance property on this object.
        if (TypeDescriptor.GetProperties(this)[propertyName] == null)
        {
            string msg = "Invalid property name: " + propertyName;

            ////if (this.ThrowOnInvalidPropertyName)
            ////    throw new ArgumentException(msg);
            ////else
            Debug.Fail(msg);
        }
    }

    public void NotifyAllProperties()
    {
        var properties = this.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var prop in properties)
        {
            RaisePropertyChanged(prop.Name);
        }

    }



}
