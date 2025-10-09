using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Extensions;


namespace IRI.Maptor.Sta.Common.Primitives;

public struct BoundingBox
{
    private double xMin, yMin, xMax, yMax;

    #region Properties

    public double XMin
    {
        get { return xMin; }
    }

    public double YMin
    {
        get { return yMin; }
    }

    public double XMax
    {
        get { return xMax; }
    }

    public double YMax
    {
        get { return yMax; }
    }


    [JsonIgnore]
    public double Width
    {
        get { return this.XMax - this.XMin; }
    }

    [JsonIgnore]
    public double Height
    {
        get { return this.YMax - this.YMin; }
    }

    [JsonIgnore]
    public Point TopRight { get { return new Point(xMax, YMax); } }

    [JsonIgnore]
    //The system is asumed to be right handed
    public Point TopLeft { get { return new Point(XMin, YMax); } }

    [JsonIgnore]
    //The system is asumed to be right handed
    public Point BottomRight { get { return new Point(XMax, YMin); } }

    [JsonIgnore]
    public Point BottomLeft { get { return new Point(XMin, YMin); } }

    [JsonIgnore]
    public Point MiddleRight { get { return new Point(xMax, Center.Y); } }

    [JsonIgnore]
    public Point MiddleLeft { get { return new Point(XMin, Center.Y); } }

    [JsonIgnore]
    public Point MiddleTop { get { return new Point(Center.X, YMax); } }

    [JsonIgnore]
    public Point MiddleBottom { get { return new Point(Center.X, YMin); } }

    [JsonIgnore]
    public Point Center { get { return new Point((XMax + XMin) / 2.0, (YMax + YMin) / 2.0); } }

    #endregion

    public BoundingBox(double xMin, double yMin, double xMax, double yMax)
    {
        this.xMin = xMin;

        this.xMax = xMax;

        this.yMin = yMin;

        this.yMax = yMax;
    }

    public BoundingBox(Point center, double offset) : this(center, offset * 2, offset * 2)
    {
        //this.xMin = center.X - offset;

        //this.xMax = center.X + offset;

        //this.yMin = center.Y - offset;

        //this.yMax = center.Y + offset;
    }

    public BoundingBox(Point center, double width, double height)
    {
        this.xMin = center.X - width / 2.0;

        this.xMax = center.X + width / 2.0;

        this.yMin = center.Y - height / 2.0;

        this.yMax = center.Y + height / 2.0;
    }

    public static BoundingBox NaN { get { return new BoundingBox(double.NaN, double.NaN, double.NaN, double.NaN); } }

    //public static bool IsNaN(BoundingBox value)
    //{
    //    return double.IsNaN(value.Width) || double.IsNaN(value.Height);
    //}

    public bool IsNaN()
    {
        //return BoundingBox.IsNaN(this);
        return double.IsNaN(this.Width) || double.IsNaN(this.Height);
    }

    public bool IsValid()
    {
        return !double.IsNaN(this.Width + this.Height) &&
                !double.IsInfinity(this.Width + this.Height);
    }

    public bool IsValidPlus()
    {
        return IsValid() && this.Width * this.Height > 0;
    }

    public double GetDiagonalLength()
    {
        double width = this.Width;

        double height = this.Height;

        return Math.Sqrt(width * width + height * height);
    }

    public string AsWkt()
    {
        return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "POLYGON(({0} {1}, {0} {2}, {3} {2}, {3} {1}, {0} {1}))",
                this.XMin,
                this.YMin,
                this.YMax,
                this.XMax);
    }


    #region Manipulation

    public BoundingBox Add(BoundingBox secondBoundingBox)
    {
        return new BoundingBox(xMin: Math.Min(this.XMin, secondBoundingBox.XMin),
                                yMin: Math.Min(this.YMin, secondBoundingBox.YMin),
                                xMax: Math.Max(this.XMax, secondBoundingBox.XMax),
                                yMax: Math.Max(this.YMax, secondBoundingBox.YMax));
    }

    public BoundingBox Intersect(BoundingBox other)
    {
        if (!Intersects(other))
        {
            return NaN;
        }

        return new BoundingBox(xMin: Math.Max(this.XMin, other.XMin),
                                yMin: Math.Max(this.YMin, other.YMin),
                                xMax: Math.Min(this.XMax, other.XMax),
                                yMax: Math.Min(this.YMax, other.YMax));
    }

    //public void Extend(BoundingBox secondBoundingBox)
    //{
    //    this.xMin = Math.Min(this.XMin, secondBoundingBox.XMin);

    //    this.yMin = Math.Min(this.YMin, secondBoundingBox.YMin);

    //    this.xMax = Math.Max(this.XMax, secondBoundingBox.XMax);

    //    this.yMax = Math.Max(this.YMax, secondBoundingBox.YMax);
    //}

    public BoundingBox Expand(double factor)
    {
        var xShift = this.Width / 2.0 * factor;

        var yShift = this.Height / 2.0 * factor;

        var center = this.Center;

        return new BoundingBox(center.X - xShift, center.Y - yShift, center.X + xShift, center.Y + yShift);
    }

    public BoundingBox Extend(double value)
    {
        var xShift = this.Width / 2.0 + value;

        var yShift = this.Height / 2.0 + value;

        var center = this.Center;

        return new BoundingBox(center.X - xShift, center.Y - yShift, center.X + xShift, center.Y + yShift);
    }

    public BoundingBox Transform(Func<Point, Point> func)
    {
        var newTopLeft = func is null ? this.TopLeft : func(this.TopLeft);

        var newButtomRight = func is null ? this.BottomRight : func(this.BottomRight);

        return new BoundingBox(newTopLeft.X, newButtomRight.Y, newButtomRight.X, newTopLeft.Y);
    }

    #endregion

    public bool Intersects(BoundingBox other)
    {
        var xIntersects = IsInRange(this.XMin, other.XMin, other.XMax) ||
                            IsInRange(this.XMax, other.XMin, other.XMax) ||
                            IsInRange(other.XMin, this.XMin, this.XMax) ||
                            IsInRange(other.XMax, this.XMin, this.XMax);

        var yIntersects = IsInRange(this.YMin, other.YMin, other.YMax) ||
                            IsInRange(this.YMax, other.YMin, other.YMax) ||
                            IsInRange(other.YMin, this.YMin, this.YMax) ||
                            IsInRange(other.YMax, this.YMin, this.YMax);

        return xIntersects && yIntersects;
    }

    public bool Intersects<T>(T point) where T : IPoint, new()
    {
        return IsInRange(point.X, this.XMin, this.XMax) && IsInRange(point.Y, this.YMin, this.YMax);
    }


    #region Static Methods

    public static BoundingBox GetMergedBoundingBox(IEnumerable<BoundingBox> boundingBoxes, bool ignoreNanValues = false)
    {
        if (boundingBoxes == null || boundingBoxes.Count() == 0)
        {
            return BoundingBox.NaN;
        }

        if (ignoreNanValues)
        {
            return GetMergedBoundingBox(boundingBoxes.Where(i => !i.IsNaN()));
        }
        else
        {
            return new BoundingBox(
            xMin: boundingBoxes.Min(i => i.XMin),
            yMin: boundingBoxes.Min(i => i.YMin),
            xMax: boundingBoxes.Max(i => i.XMax),
            yMax: boundingBoxes.Max(i => i.YMax));
        }

    }

    public static BoundingBox CalculateBoundingBox<T>(IEnumerable<T> points) where T : IPoint, new()
    {
        if (points.IsNullOrEmpty()/* == null || points.Count() == 0*/)
            return BoundingBox.NaN;

        return new BoundingBox(xMin: points.Min(i => i.X),
                                yMin: points.Min(i => i.Y),
                                xMax: points.Max(i => i.X),
                                yMax: points.Max(i => i.Y));
    }

    public static BoundingBox Add(BoundingBox first, BoundingBox second)
    {
        return GetMergedBoundingBox([first, second]);
    }

    public static BoundingBox Parse(string bbox)
    {
        try
        {
            var items = bbox.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(i => double.Parse(i)).ToList();

            if (items.Count != 4)
            {
                throw new NotImplementedException("Invalid input in BoundingBox.Parse");
            }

            return new BoundingBox(xMin: items[0], yMin: items[1], xMax: items[2], yMax: items[3]);
        }
        catch (Exception)
        {
            throw new NotImplementedException("ERROR AT BoundingBox.Parse");
        }
    }

    public static BoundingBox Create<T>(params T[] points) where T : IPoint
    {
        if (points.IsNullOrEmpty())
            return BoundingBox.NaN;

        return new BoundingBox(
            xMin: points.Min(p => p.X),
            yMin: points.Min(p => p.Y),
            xMax: points.Max(p => p.X),
            yMax: points.Max(p => p.Y));
    }

    private static bool IsInRange(double value, double minRange, double maxRange)
        => value >= minRange && value <= maxRange;

    #endregion

    #region Operators

    public static bool operator ==(BoundingBox first, BoundingBox second)
    {
        return first.XMin == second.XMin &&
                first.XMax == second.XMax &&
                first.YMin == second.YMin &&
                first.YMax == second.YMax;
    }

    public static bool operator !=(BoundingBox first, BoundingBox second)
    {
        return !(first == second);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
        return string.Format("XMin: {0}, YMin: {1}, XMax: {2}, YMax: {3}", this.XMin, this.YMin, this.XMax, this.YMax);
    }

    public override bool Equals(object obj)
    {
        return obj.GetType() == typeof(BoundingBox) && (BoundingBox)obj == this;
    }

    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    #endregion

    public List<T> GetClockWiseOrderOfEsriPoints<T>() where T : IPoint, new()
    {
        return new List<T>
        {
            new T() { X = this.XMin, Y = this.YMin },
            new T() { X = this.XMin, Y = this.YMax },
            new T() { X = this.XMax, Y = this.YMax },
            new T() { X = this.XMax, Y = this.YMin }
        };
    }



    public bool Covers<T>(T point) where T : IPoint
    {
        if (point is null)
            return false;

        return (point.X >= this.XMin && point.X <= this.XMax) &&
               (point.Y >= this.YMin && point.Y <= this.YMax);
    }

    public bool Contains<T>(T point) where T : IPoint
    {
        if (point is null)
            return false;

        return (point.X > this.XMin && point.X < this.XMax) &&
               (point.Y > this.YMin && point.Y < this.YMax);
    }

    public bool CoversApproximately<T>(T point, double threshold = 0.01) where T : IPoint
    {
        if (point is null)
            return false;

        return this.Extend(threshold).Covers(point);
    }

}
