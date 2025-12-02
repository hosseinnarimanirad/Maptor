using IRI.Maptor.Jab.Common.Models.CoordinatePanel;
using System.Collections.ObjectModel;
using System.Linq;
using IRI.Maptor.Jab.Common.Models;
using System.Windows;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public class CoordinatePanelViewModel : Notifier
{
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


    public CoordinatePanelViewModel()
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
    }

    private void UpdateSelectedItem()
    {
        foreach (var item in SpatialReferences)
        {
            item.FireIsSelectedChanged = e => { SelectedItem = e; };
        }
    }

    public string GetCurrentPosstionString(Sta.Common.Primitives.Point geodeticPoint)
    {
        return SelectedItem?.GetPositionString(geodeticPoint);
    }
}
