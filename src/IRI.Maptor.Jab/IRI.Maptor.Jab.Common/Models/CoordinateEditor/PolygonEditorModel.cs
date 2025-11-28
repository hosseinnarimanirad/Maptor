using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

namespace IRI.Maptor.Jab.Common.Models.CoordinateEditor;

public class PolygonEditorModel : CoordinateEditor
{
    private LineStringEditorModel _outterRing;

    public LineStringEditorModel OutterRing
    {
        get { return _outterRing; }
        set
        {
            _outterRing = value;
            RaisePropertyChanged();
        }
    }

    private ObservableCollection<LineStringEditorModel> _innerRings;

    public ObservableCollection<LineStringEditorModel> InnerRings
    {
        get { return _innerRings; }
        set
        {
            _innerRings = value;
            RaisePropertyChanged();
        }
    }

    public PolygonEditorModel()
    {
        this.OutterRing = new LineStringEditorModel();

        this.InnerRings = new ObservableCollection<LineStringEditorModel>();
    }

    public PolygonEditorModel(Geometry polygon)
    {
        if (polygon.Type != GeometryType.Polygon)
        {
            throw new NotImplementedException();
        }

        OutterRing = new LineStringEditorModel(polygon.Geometries.First());

        InnerRings = new ObservableCollection<LineStringEditorModel>(polygon.Geometries.Skip(1).Select(g => new LineStringEditorModel(g)));

        this.Srid = polygon.Srid;
    }


    public override Geometry GetGeometry()
    {
        List<Geometry> rings = new List<Geometry>();

        rings.Add(OutterRing.GetGeometry());

        foreach (var ring in InnerRings)
        {
            rings.Add(ring.GetGeometry());
        }

        return new Geometry(rings, GeometryType.Polygon, Srid);
    }
}
