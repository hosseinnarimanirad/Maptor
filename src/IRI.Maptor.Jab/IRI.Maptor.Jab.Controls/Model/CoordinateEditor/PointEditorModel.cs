using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

namespace IRI.Maptor.Jab.Controls.Models;

public class PointEditorModel : CoordinateEditor
{

    private Point _point;

    public Point Point
    {
        get { return _point; }
        set
        {
            _point = value;
            RaisePropertyChanged();
        }
    }



    public PointEditorModel()
    {

    }

    public PointEditorModel(Geometry point)
    {
        if (point.Type != GeometryType.Point)
        {
            throw new NotImplementedException();
        }

        this.Point = new Point(point.Points[0].X, point.Points[0].Y);

        this.Srid = point.Srid;
    }

    public override Geometry GetGeometry()
    {
        return Geometry.CreatePointOrLineString(new System.Collections.Generic.List<Point> { Point }, Srid);
    }
}
