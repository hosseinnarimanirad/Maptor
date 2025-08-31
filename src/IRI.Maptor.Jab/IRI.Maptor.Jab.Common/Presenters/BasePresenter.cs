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
        RaisePropertyChanged(nameof(Ltxt_cmd_general_addTextToMap));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_clearAll));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_drawPoint));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_drawPolygon));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_drawPolyline));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_fullExtent));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_goTo));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_measureArea));
        RaisePropertyChanged(nameof(Ltxt_cmd_general_measureLength));

        RaisePropertyChanged(nameof(cmd_general_pan));
        RaisePropertyChanged(nameof(cmd_general_zoomIn));
        RaisePropertyChanged(nameof(cmd_general_zoomOut));
        RaisePropertyChanged(nameof(cmd_general_zoomPrevious));
        RaisePropertyChanged(nameof(cmd_general_zoomNext));


        RaisePropertyChanged(nameof(Ltxt_header_BaseMaps));
        RaisePropertyChanged(nameof(Ltxt_header_DrawingLegend));
        RaisePropertyChanged(nameof(Ltxt_header_LayerLegend));
    }


    public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

    /// <summary>
    /// Content of a button for Add shapefile command
    /// </summary>
    public string Ltxt_cmd_general_AddShapefile => LocalizationManager.Instance[cmd_general_addShapefile.ToString()];

    /// <summary>
    /// Content of a button for Add text to map command
    /// </summary>
    public string Ltxt_cmd_general_addTextToMap => LocalizationManager.Instance[cmd_general_addTextToMap.ToString()];

    /// <summary>
    /// Content of a button for clear all command
    /// </summary>
    public string Ltxt_cmd_general_clearAll => LocalizationManager.Instance[cmd_general_clearAll.ToString()];

    /// <summary>
    /// Content of a button for Draw point command
    /// </summary>
    public string Ltxt_cmd_general_drawPoint => LocalizationManager.Instance[cmd_general_drawPoint.ToString()];

    /// <summary>
    /// Content of a button for Draw polygon command
    /// </summary>
    public string Ltxt_cmd_general_drawPolygon => LocalizationManager.Instance[cmd_general_drawPolygon.ToString()];

    /// <summary>
    /// Content of a button for Draw polyline command
    /// </summary>
    public string Ltxt_cmd_general_drawPolyline => LocalizationManager.Instance[cmd_general_drawPolyline.ToString()];

    /// <summary>
    /// Content of a button for full extent command
    /// </summary>
    public string Ltxt_cmd_general_fullExtent => LocalizationManager.Instance[cmd_general_fullExtent.ToString()];

    /// <summary>
    /// Content of a button for showing the Goto dialog command
    /// </summary>
    public string Ltxt_cmd_general_goTo => LocalizationManager.Instance[cmd_general_goTo.ToString()];

    /// <summary>
    /// Content of a button for Measure area command
    /// </summary>
    public string Ltxt_cmd_general_measureArea => LocalizationManager.Instance[cmd_general_measureArea.ToString()];

    /// <summary>
    /// Content of a button for Measure length command
    /// </summary>
    public string Ltxt_cmd_general_measureLength => LocalizationManager.Instance[cmd_general_measureLength.ToString()];

    /// <summary>
    /// Content of a button for Pan command
    /// </summary>
    public string Ltxt_cmd_general_pan => LocalizationManager.Instance[cmd_general_pan.ToString()];

    /// <summary>
    /// Content of a button for Zoom In command
    /// </summary>
    public string Ltxt_cmd_general_zoomIn => LocalizationManager.Instance[cmd_general_zoomIn.ToString()];

    /// <summary>
    /// Content of a button for Zoom Out command
    /// </summary>
    public string Ltxt_cmd_general_zoomOut => LocalizationManager.Instance[cmd_general_zoomOut.ToString()];

    /// <summary>
    /// Content of a button for Zoom Previous command
    /// </summary>
    public string Ltxt_cmd_general_zoomPrevious => LocalizationManager.Instance[cmd_general_zoomPrevious.ToString()];

    /// <summary>
    /// Content of a button for Zoom Next command
    /// </summary>
    public string Ltxt_cmd_general_zoomNext => LocalizationManager.Instance[cmd_general_zoomNext.ToString()];

     
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

    #endregion
}
 

