using System;
using System.Collections.ObjectModel;
using System.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;

namespace IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

public class PolygonEditorViewModel : Notifier
{
    public string LExteriorRing => LocalizationManager.Instance["GeometryDetailsView_ExteriorRing"] ?? "Exterior Ring";
    public string LInteriorRings => LocalizationManager.Instance["GeometryDetailsView_InteriorRings"] ?? "Interior Rings";
    public string LAddInteriorRing => LocalizationManager.Instance["GeometryDetailsView_AddInteriorRing"] ?? "Add Interior Ring";
    private ObservableCollection<RingInfo> _rings = new ObservableCollection<RingInfo>();
    public ObservableCollection<RingInfo> Rings
    {
        get => _rings;
        set
        {
            _rings = value ?? new ObservableCollection<RingInfo>();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ExteriorRing));
            RaisePropertyChanged(nameof(InteriorRings));
        }
    }

    public RingInfo? ExteriorRing => Rings.FirstOrDefault();
    public ObservableCollection<RingInfo> InteriorRings => 
        new ObservableCollection<RingInfo>(Rings.Skip(1));

    public LineStringEditorViewModel? ExteriorRingEditor
    {
        get
        {
            if (ExteriorRing != null)
            {
                //return new LineStringEditorPresenter(ExteriorRing.Points);
            }
            return null;
        }
    }

    public event Action<ObservableCollection<RingInfo>>? GeometryChanged;

    private RelayCommand _addInteriorRingCommand;
    public RelayCommand AddInteriorRingCommand =>
        _addInteriorRingCommand ??= new RelayCommand(param =>
        {
            var newRing = new RingInfo { IsExterior = false, Points = new ObservableCollection<NotifiablePoint>() };
            Rings.Add(newRing);
            RaisePropertyChanged(nameof(InteriorRings));
            GeometryChanged?.Invoke(Rings);
        });

    private RelayCommand _deleteRingCommand;
    public RelayCommand DeleteRingCommand =>
        _deleteRingCommand ??= new RelayCommand(param =>
        {
            if (param is RingInfo ring && !ring.IsExterior)
            {
                Rings.Remove(ring);
                RaisePropertyChanged(nameof(InteriorRings));
                GeometryChanged?.Invoke(Rings);
            }
        }, param => param is RingInfo ring && !ring.IsExterior);

    public PolygonEditorViewModel(ObservableCollection<RingInfo>? rings = null)
    {
        Rings = rings ?? new ObservableCollection<RingInfo>();
        if (Rings.Count == 0)
        {
            Rings.Add(new RingInfo { IsExterior = true, Points = new ObservableCollection<NotifiablePoint>() });
        }
        else
        {
            Rings[0].IsExterior = true;
            for (int i = 1; i < Rings.Count; i++)
            {
                Rings[i].IsExterior = false;
            }
        }

        Rings.CollectionChanged += (s, e) =>
        {
            RaisePropertyChanged(nameof(ExteriorRing));
            RaisePropertyChanged(nameof(ExteriorRingEditor));
            RaisePropertyChanged(nameof(InteriorRings));
            GeometryChanged?.Invoke(Rings);
        };
    }
}

