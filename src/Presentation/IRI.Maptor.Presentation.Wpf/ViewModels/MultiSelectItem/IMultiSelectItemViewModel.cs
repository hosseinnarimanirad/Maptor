using IRI.Maptor.Presentation.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Presentation.Wpf.ViewModels;

public abstract class IMultiSelectItemViewModel : Notifier
{
    private bool _isMultiSelectEnabled = true;

    public bool IsMultiSelectEnabled
    {
        get { return _isMultiSelectEnabled; }
        set
        {
            _isMultiSelectEnabled = value;
            RaisePropertyChanged();
        }
    }

    private bool _isAndMode;

    public bool IsAndMode
    {
        get { return _isAndMode; }
        set
        {
            _isAndMode = value;
            RaisePropertyChanged();
        }
    }

    internal abstract void AddItem();
}
