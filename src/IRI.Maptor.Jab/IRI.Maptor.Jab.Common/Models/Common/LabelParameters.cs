//using System;
//using System.Windows.Media;
//using IRI.Maptor.Sta.Spatial.Primitives;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Jab.Common.Events;

//namespace IRI.Maptor.Jab.Common.Models;

//public class LabelParameters : Notifier
//{
//    //public event EventHandler<CustomEventArgs<LabelParameters>> OnIsInScaleRangeChanged;

//    //public event EventHandler<CustomEventArgs<LabelParameters>> OnIsOnChanged;


//    //private ScaleInterval _visibleRange;
//    //public ScaleInterval VisibleRange
//    //{
//    //    get { return _visibleRange; }
//    //    set
//    //    {
//    //        _visibleRange = value;
//    //        RaisePropertyChanged();
//    //    }
//    //}

//    public LabelParameters(ScaleInterval? visibleRange, int fontSize, Brush foreground, FontFamily fontFamily, Func<Geometry<Point>, Point> positionFunc)
//    {
//        VisibleRange = visibleRange ?? ScaleInterval.All;

//        FontSize = fontSize;

//        Foreground = foreground ?? Brushes.Black;

//        FontFamily = fontFamily;

//        PositionFunc = positionFunc;
//    }

//    public LabelParameters(ScaleInterval visibleRange, int fontSize, Color foreground, FontFamily fontFamily, Func<Geometry<Point>, Point> positionFunc)
//        : this(visibleRange, fontSize, new SolidColorBrush(foreground), fontFamily, positionFunc)
//    {

//    }
//}
