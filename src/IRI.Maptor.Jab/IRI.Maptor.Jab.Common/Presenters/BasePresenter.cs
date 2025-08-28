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

    private void Instance_FlowDirectionChanged()
    {
        RaisePropertyChanged(nameof(CurrentFlowDirection));
    }

    private void OnLanguageChanged()
    {
        RaisePropertyChanged(nameof(AddShapefileText));
        RaisePropertyChanged(nameof(BaseMapsText));
        RaisePropertyChanged(nameof(ClearText));
        RaisePropertyChanged(nameof(DrawingsText));
        RaisePropertyChanged(nameof(DrawPointText));
        RaisePropertyChanged(nameof(DrawPolylineText));
        RaisePropertyChanged(nameof(DrawPolygonText));
        RaisePropertyChanged(nameof(AddTextToMap));
        RaisePropertyChanged(nameof(FullExtentText));
        RaisePropertyChanged(nameof(GoToText));
        RaisePropertyChanged(nameof(LayersText));
        RaisePropertyChanged(nameof(MeasureAreaText));
        RaisePropertyChanged(nameof(MeasureLengthText));
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



    public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;

    public string AddShapefileText => LocalizationManager.Instance[ui_addShapefile.ToString()];
    public string BaseMapsText => LocalizationManager.Instance[ui_baseMaps.ToString()];
    public string ClearText => LocalizationManager.Instance[ui_clear.ToString()];
    public string DrawingsText => LocalizationManager.Instance[ui_drawings.ToString()];
    public string DrawPointText => LocalizationManager.Instance[ui_drawPoint.ToString()];
    public string DrawPolylineText => LocalizationManager.Instance[ui_drawPolyline.ToString()];
    public string DrawPolygonText => LocalizationManager.Instance[ui_drawPolygon.ToString()];
    public string AddTextToMap => LocalizationManager.Instance[ui_addTextToMap.ToString()];
    public string FullExtentText => LocalizationManager.Instance[ui_fullExtent.ToString()];
    public string GoToText => LocalizationManager.Instance[ui_goTo.ToString()];
    public string LayersText => LocalizationManager.Instance[ui_layers.ToString()];
    public string MeasureAreaText => LocalizationManager.Instance[ui_measureArea.ToString()];
    public string MeasureLengthText => LocalizationManager.Instance[ui_measureLength.ToString()];
}