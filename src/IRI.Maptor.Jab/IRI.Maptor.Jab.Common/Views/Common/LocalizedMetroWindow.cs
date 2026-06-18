using System;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using MahApps.Metro.Controls;

using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Controls;

public class LocalizedMetroWindow : MetroWindow, IDisposable, INotifyPropertyChanged
{
    public LocalizedMetroWindow()
    {
        LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    //public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

    protected virtual void Instance_LanguageChanged()
    {
        NotifyAllProperties();
    }


    #region INotifyPropertyChanged

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
        // If you raise PropertyChanged and do not specify a property name,
        // all properties on the object are considered to be changed by the binding system.
        if (string.IsNullOrEmpty(propertyName))
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

    // todo: consider using an attribute on top of the 
    // properties that are localized text
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

    #endregion


    #region IDispose

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
            }

            // Dispose unmanaged resources here if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
