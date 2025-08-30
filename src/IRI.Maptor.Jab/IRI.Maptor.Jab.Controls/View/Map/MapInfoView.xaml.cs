using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Localization; 

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for MapInfoView.xaml
/// </summary>
public partial class MapInfoView : LocalizedUserControl
{
    public MapInfoView() : base()
    {
        InitializeComponent();

        //LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    protected override void Instance_LanguageChanged()
    {
        this.NotifyAllProperties();
    }

    //public FlowDirection CurrentFlowDirection => LocalizationManager.Instance.CurrentFlowDirection;


    public string Ltxt_mapPanel_header_currentPoint => LocalizationManager.Instance[LocalizationResourceKeys.mapPanel_header_currentPoint.ToString()];

    public string Ltxt_mapPanel_header_multiPart => LocalizationManager.Instance[LocalizationResourceKeys.mapPanel_header_multiPart.ToString()];

    public string Ltxt_mapPanel_header_srs => LocalizationManager.Instance[LocalizationResourceKeys.mapPanel_header_srs.ToString()];

    public string Ltxt_srs_utmZone => LocalizationManager.Instance[LocalizationResourceKeys.srs_utmZone.ToString()];

    public string Ltxt_srs_utmTitle => LocalizationManager.Instance[LocalizationResourceKeys.srs_utmTitle.ToString()];

    public string Ltxt_srs_geodeticWgs84Title => LocalizationManager.Instance[LocalizationResourceKeys.srs_geodeticTitle.ToString()];

    public string Ltxt_map_draw_newDrawing => LocalizationManager.Instance[LocalizationResourceKeys.map_draw_newDrawing.ToString()];

    public string Ltxt_map_draw_addPoint => LocalizationManager.Instance[LocalizationResourceKeys.map_draw_addPoint.ToString()];

    public string Ltxt_map_draw_cancelDrawing => LocalizationManager.Instance[LocalizationResourceKeys.map_draw_cancelDrawing.ToString()];

    public string Ltxt_map_draw_finishDrawing => LocalizationManager.Instance[LocalizationResourceKeys.map_draw_finishDrawing.ToString()];

    public string Ltxt_map_draw_finishDrawingPart => LocalizationManager.Instance[LocalizationResourceKeys.map_draw_finishDrawingPart.ToString()];


    //#region IDispose
    //private bool _disposed = false;
    //protected virtual void Dispose(bool disposing)
    //{
    //    if (!_disposed)
    //    {
    //        if (disposing)
    //        {
    //            // Dispose managed resources
    //            LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
    //        }

    //        // Dispose unmanaged resources here if any
    //        _disposed = true;
    //    }
    //}
    //public void Dispose()
    //{
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}
    //#endregion
}
