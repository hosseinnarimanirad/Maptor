using System;
using System.Windows;
using System.Security.Principal;

using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Localization;

using static IRI.Maptor.Jab.Common.Localization.LocalizationResourceKeys;

namespace IRI.Maptor.Jab.Common.Presenters;

//TO DO: consider replacing Action methods with "IDialogService" 
public class BasePresenter : Notifier
{
    public IDialogService DialogService { get; set; }

    private string _userName;

    public string UserName
    {
        get { return _userName; }
        set
        {
            _userName = value;
            RaisePropertyChanged();
        }
    }

    public GenericPrincipal CurrentGenericPrincipal
    {
        get { return System.Threading.Thread.CurrentPrincipal as GenericPrincipal; }
        set
        {
            System.Threading.Thread.CurrentPrincipal = value;

            UserName = value.Identity.Name;

            RaisePropertyChanged(nameof(UserName));

            RaisePropertyChanged();

            UserChanged?.Invoke(this, UserName);
        }
    }

    public event EventHandler<string> UserChanged;


    public Action RequestClose;

    public Action RequestActivateWindow;

    //public Func<string, string> RequestOpenFile;

    //public Func<string, string> RequestSaveFile;

    //public string[] OpenFiles(string filter, object owner = null)
    //{
    //    return DialogService.ShowOpenFilesDialog(filter, owner);
    //    //return this.RequestOpenFile?.Invoke(filter);
    //}

    //public string[] OpenFiles<T>(string filter)
    //{
    //    return DialogService.ShowOpenFilesDialog<T>(filter);
    //    //return this.RequestOpenFile?.Invoke(filter);
    //}

    //public string OpenFile(string filter, object owner = null)
    //{
    //    return DialogService.ShowOpenFileDialog(filter, owner);
    //    //return this.RequestOpenFile?.Invoke(filter);
    //}

    //public string OpenFile<T>(string filter)
    //{
    //    return DialogService.ShowOpenFileDialog<T>(filter);
    //    //return this.RequestOpenFile?.Invoke(filter);
    //}




    //public string SaveFile(string filter, object owner = null)
    //{
    //    return DialogService.ShowSaveFileDialog(filter, owner);
    //    //return this.RequestSaveFile?.Invoke(filter);
    //}

    //public string SaveFile<T>(string filter)
    //{
    //    return DialogService.ShowSaveFileDialog<T>(filter);
    //    //return this.RequestSaveFile?.Invoke(filter);
    //}

    public BasePresenter()
    {
        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
        LocalizationManager.Instance.FlowDirectionChanged += Instance_FlowDirectionChanged;
    }

    public void RedirectRequestesTo(BasePresenter presenter)
    {
        if (presenter == this)
        {
            return;
        }

        DialogService = presenter.DialogService;

        //this.RequestOpenFile = arg => presenter.RequestOpenFile(arg);
        //this.RequestSaveFile = arg => presenter.RequestSaveFile(arg);
        //this.RequestShowMessage = message => presenter.ShowMessage(message);
    }

    #region Localization

    private void Instance_FlowDirectionChanged()
    {
        RaisePropertyChanged(nameof(CurrentFlowDirection));
    }

    private void OnLanguageChanged()
    {
        RaisePropertyChanged(nameof(Ltxt_cmd_general_AddShapefile));
        RaisePropertyChanged(nameof(Ltxt_header_BaseMaps));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_clearAll));
        RaisePropertyChanged(nameof(Ltxt_header_DrawingLegend));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_drawPoint));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_drawPolyline));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_drawPolygon));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_addTextToMap));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_fullExtent));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_goTo));
        RaisePropertyChanged(nameof(Ltxt_header_LayerLegend));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_measureArea));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_measureLength));
    }


    public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

    /// <summary>
    /// Header of BaseMaps panel
    /// </summary>
    public string Ltxt_header_BaseMaps => LocalizationManager.Instance[ui_header_baseMaps.ToString()];

    /// <summary>
    /// Header of Drawing's Legend 
    /// </summary>
    public string Ltxt_header_DrawingLegend => LocalizationManager.Instance[ui_header_drawingLegend.ToString()];

    /// <summary>
    /// Header of Layer's Legend
    /// </summary>
    public string Ltxt_header_LayerLegend => LocalizationManager.Instance[ui_header_layerLegend.ToString()];

    /// <summary>
    /// Content of a button for Add shapefile command
    /// </summary>
    public string Ltxt_cmd_general_AddShapefile => LocalizationManager.Instance[cmd_general_addShapefile.ToString()];

    /// <summary>
    /// Content of a button for Add text to map command
    /// </summary>
    public string Ltxt_cmd_general_addTextToMap => LocalizationManager.Instance[cmd_general_addTextToMap.ToString()];
    
    /// <summary>
    /// Content of a button for Draw point command
    /// </summary>
    public string Ltxt_cmd_general_drawPoint => LocalizationManager.Instance[cmd_general_drawPoint.ToString()];

    /// <summary>
    /// Content of a button for Draw polyline command
    /// </summary>
    public string Ltxt_cmd_general_drawPolyline => LocalizationManager.Instance[cmd_general_drawPolyline.ToString()];

    /// <summary>
    /// Content of a button for Draw polygon command
    /// </summary>
    public string Ltxt_cmd_general_drawPolygon => LocalizationManager.Instance[cmd_general_drawPolygon.ToString()];

    /// <summary>
    /// Content of a button for clear all command
    /// </summary>
    public string Ltxt_cmd_general_clearAll => LocalizationManager.Instance[cmd_general_clearAll.ToString()];

    /// <summary>
    /// Content of a button for full extent command
    /// </summary>
    public string Ltxt_cmd_general_fullExtent => LocalizationManager.Instance[cmd_general_fullExtent.ToString()];
    
    /// <summary>
    /// Content of a button for Measure area command
    /// </summary>
    public string Ltxt_cmd_general_measureArea => LocalizationManager.Instance[cmd_general_measureArea.ToString()];
    
    /// <summary>
    /// Content of a button for Measure length command
    /// </summary>
    public string Ltxt_cmd_general_measureLength => LocalizationManager.Instance[cmd_general_measureLength.ToString()];

    /// <summary>
    /// Content of a button for showing the Goto dialog command
    /// </summary>
    public string Ltxt_cmd_general_goTo => LocalizationManager.Instance[cmd_general_goTo.ToString()];

    #endregion
}