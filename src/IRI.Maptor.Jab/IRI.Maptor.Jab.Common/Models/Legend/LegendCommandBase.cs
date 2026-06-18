using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public abstract class LegendCommandBase : Notifier, ILegendCommand
{
    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;
            RaisePropertyChanged();
        }
    }

    protected string ToolTipResourceKey { get; set; }
    public string ToolTip => LocalizationManager.Instance[ToolTipResourceKey];


    private bool _isCommandVisible = true;
    public bool IsCommandVisible
    {
        get { return _isCommandVisible; }
        set
        {
            _isCommandVisible = value;
            RaisePropertyChanged();
        }
    }


    private string _pathMarkup;
    public string PathMarkup
    {
        get { return _pathMarkup; }
        set
        {
            _pathMarkup = value;
            RaisePropertyChanged();
        }
    }


    private RelayCommand _command;
    public RelayCommand Command
    {
        get { return _command; }
        set
        {
            _command = value;
            RaisePropertyChanged();
        }
    }

    public ILayer Layer { get; set; }


    protected LegendCommandBase()
    {
        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    public LegendCommandBase(string tooltipResourceKey) : this()
    {
        ToolTipResourceKey = tooltipResourceKey;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(ToolTip));
    }


    public override string ToString() => ToolTip;

}
