using IRI.Maptor.Jab.Common.Models.CoordinatePanel;
using System.Collections.ObjectModel;
using System.Linq;
using IRI.Maptor.Jab.Common.Models;
using System.Windows;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.Presenters;

public class CoordinatePanelPresenter : Notifier
{
    private readonly LocalizationManager _localization;
    public FlowDirection CurrentFlowDirection => _localization.CurrentFlowDirection;



    private ObservableCollection<SpatialReferenceItem> _spatialReferences = new ObservableCollection<SpatialReferenceItem>();

    public ObservableCollection<SpatialReferenceItem> SpatialReferences
    {
        get { return _spatialReferences; }
        private set
        {
            _spatialReferences = value;
            RaisePropertyChanged();
        }
    }

    private SpatialReferenceItem _selectedItem;

    public SpatialReferenceItem SelectedItem
    {
        get { return _selectedItem; }
        set
        {
            _selectedItem = value;
            RaisePropertyChanged();
        }
    }


    public CoordinatePanelPresenter()
    {
        SpatialReferences = new ObservableCollection<SpatialReferenceItem>();

        SpatialReferences.CollectionChanged += (sender, e) =>
        {
            UpdateSelectedItem();
        };

        SpatialReferences.Add(SpatialReferenceItems.GeodeticWgs84);
        SpatialReferences.Add(SpatialReferenceItems.GeodeticDmsWgs84);
        SpatialReferences.Add(SpatialReferenceItems.UtmWgs84);

        SpatialReferences.First().IsSelected = true;

        //SetLanguage(LanguageMode.Persian);
        _localization = LocalizationManager.Instance;
        _localization.FlowDirectionChanged += () =>
        {
            RaisePropertyChanged(nameof(CurrentFlowDirection));
            RaisePropertyChanged(nameof(SelectedItem));
        };
    }

    private void UpdateSelectedItem()
    {
        foreach (var item in SpatialReferences)
        {
            item.FireIsSelectedChanged = e => { SelectedItem = e; };
        }
    }

    //public void SetLanguage(LanguageMode value)
    //{
    //    foreach (var item in SpatialReferences)
    //    {
    //        item.UILanguage = value;
    //    }

    //    this.UIFlow = value == LanguageMode.Persian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    //}

    //private FlowDirection _uiFlow;

    //public FlowDirection UIFlow
    //{
    //    get { return _uiFlow; }
    //    set
    //    {
    //        _uiFlow = value;
    //        RaisePropertyChanged();
    //        System.Diagnostics.Debug.WriteLine("UIFlow: " + value.ToString());
    //    }
    //}

    public string GetCurrentPosstionString(Sta.Common.Primitives.Point geodeticPoint)
    {
        return SelectedItem?.GetPositionString(geodeticPoint);
    }
}
