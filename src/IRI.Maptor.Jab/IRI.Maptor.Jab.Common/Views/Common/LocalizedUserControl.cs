//using System;
//using System.Windows;

//using IRI.Maptor.Jab.Common.Localization;

//namespace IRI.Maptor.Jab.Common;

//public class LocalizedUserControl : NotifiableUserControl, IDisposable
//{
//    public LocalizedUserControl()
//    {
//        LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
//        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
//    }

//    //public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

//    protected virtual void Instance_LanguageChanged()
//    {
//        NotifyAllProperties();
//    }
     
//    #region IDispose

//    private bool _disposed = false;

//    protected virtual void Dispose(bool disposing)
//    {
//        if (!_disposed)
//        {
//            if (disposing)
//            {
//                // Dispose managed resources
//                LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
//            }

//            // Dispose unmanaged resources here if any
//            _disposed = true;
//        }
//    }

//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }

//    #endregion

//}