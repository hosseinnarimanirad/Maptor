using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;
using IRI.Maptor.Core.Common.Model;

namespace IRI.Maptor.Presentation.Core;

public abstract class ValueObjectNotifier : ValueObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        VerifyPropertyName(propertyName);

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
}
