//using System;
//using System.Collections.ObjectModel;
//using System.Linq;
//using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;
//using IRI.Maptor.Sta.Common.Primitives;

//namespace IRI.Maptor.Jab.Common.Models.CoordinateEditor;

//public class LineStringEditorModel : CoordinateEditor
//{
//    private bool _isClosed;

//    public bool IsClosed
//    {
//        get { return _isClosed; }
//        set
//        {
//            _isClosed = value;
//            RaisePropertyChanged();
//        }
//    }

//    private ObservableCollection<Point> _points;

//    public ObservableCollection<Point> Points
//    {
//        get { return _points; }
//        set
//        {
//            _points = value;
//            RaisePropertyChanged();
//        }
//    }



//    public LineStringEditorModel()
//    {
//        this.Points = new ObservableCollection<Point>();
//    }

//    public LineStringEditorModel(Geometry lineString)
//    {
//        if (lineString.Type != GeometryType.LineString)
//        {
//            throw new NotImplementedException();
//        }

//        this.Points = new ObservableCollection<Point>(lineString.Points);

//        this.Srid = lineString.Srid;
//    }

//    public override Geometry GetGeometry()
//    {
//        return Geometry.Create(_points.ToList(), GeometryType.LineString, Srid);
//    }
//}
