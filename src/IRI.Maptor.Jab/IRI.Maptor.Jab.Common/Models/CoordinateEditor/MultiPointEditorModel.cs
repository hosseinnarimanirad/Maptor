using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

namespace IRI.Maptor.Jab.Common.Models.CoordinateEditor;

public class MultiPointEditorModel : CoordinateEditor
{
    private ObservableCollection<PointEditorModel> _points;

    public ObservableCollection<PointEditorModel> Points
    {
        get { return _points; }
        set
        {
            _points = value;
            RaisePropertyChanged();
        }
    }

    public MultiPointEditorModel()
    {
        this.Points = new ObservableCollection<PointEditorModel>();
    }

    public MultiPointEditorModel(Geometry multiPoint)
    {
        if (multiPoint.Type != GeometryType.MultiPoint)
        {
            throw new NotImplementedException();
        }

        Points = new ObservableCollection<PointEditorModel>(multiPoint.Geometries.Select(g => new PointEditorModel(g)));

        this.Srid = multiPoint.Srid;
    }


    public override Geometry GetGeometry()
    {
        return new Geometry(Points.Select(i => i.GetGeometry()).ToList(), GeometryType.MultiPoint, Srid);
    }
}
