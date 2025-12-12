using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Common.Assets.Converters;

public class DistanceAzimuthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 3)
            return "-";

        // values[0] = Current point (Locateable)
        // values[1] = Points collection (ObservableCollection<Locateable>)
        // values[2] = DataContext (GeometryEditorViewModel)
        // parameter = "DistanceToNext", "AzimuthToNext", "AzimuthToPrevious", or "VertexAngle"

        if (values[0] is not Locateable currentPoint)
            return "-";

        if (values[1] is not ObservableCollection<Locateable> points)
            return "-";

        if (values[2] is not GeometryEditorViewModel viewModel)
            return "-";

        // Find the index of the current point in the Points collection
        // Try IndexOf first, then fall back to coordinate comparison if needed
        int currentIndex = points.IndexOf(currentPoint);
        
        // If IndexOf fails, try to find by coordinate comparison
        if (currentIndex < 0)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && 
                    Math.Abs(points[i].X - currentPoint.X) < 0.0001 && 
                    Math.Abs(points[i].Y - currentPoint.Y) < 0.0001)
                {
                    currentIndex = i;
                    break;
                }
            }
        }
        
        if (currentIndex < 0)
            return "-";

        string param = parameter?.ToString()?.ToUpper() ?? "";

        // Check if this is a ring (closed geometry)
        bool isRing = viewModel.IsRingBase && points.Count >= 3;

        try
        {
            // Convert Web Mercator to Geodetic (WGS84) for current point
            var currentGeodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(currentPoint.X, currentPoint.Y));

            // Handle DistanceToNext
            if (param == "DISTANCETONEXT")
            {
                int nextIndex;
                if (isRing)
                {
                    // For rings, last point connects to first point
                    nextIndex = (currentIndex + 1) % points.Count;
                }
                else
                {
                    // For non-rings, last point has no next
                    if (currentIndex >= points.Count - 1)
                        return "-";
                    nextIndex = currentIndex + 1;
                }

                Locateable nextPoint = points[nextIndex];
                if (nextPoint == null)
                    return "-";

                var nextGeodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(nextPoint.X, nextPoint.Y));
                double distance = SpatialUtility.GetEllipsoidalLength(currentGeodetic, nextGeodetic);
                return UnitHelper.GetLengthLabel(distance);
            }

            // Handle AzimuthToNext
            if (param == "AZIMUTHTONEXT")
            {
                int nextIndex;
                if (isRing)
                {
                    // For rings, last point connects to first point
                    nextIndex = (currentIndex + 1) % points.Count;
                }
                else
                {
                    // For non-rings, last point has no next
                    if (currentIndex >= points.Count - 1)
                        return "-";
                    nextIndex = currentIndex + 1;
                }

                Locateable nextPoint = points[nextIndex];
                if (nextPoint == null)
                    return "-";

                var nextGeodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(nextPoint.X, nextPoint.Y));
                double bearing = CalculateInitialBearing(currentGeodetic, nextGeodetic);
                return FormatAngle(bearing, viewModel.UseDecimalDegreesForAngles);
            }

            // Handle AzimuthToPrevious
            if (param == "AZIMUTHTOPREVIOUS")
            {
                int previousIndex;
                if (isRing)
                {
                    // For rings, first point connects to last point
                    previousIndex = (currentIndex - 1 + points.Count) % points.Count;
                }
                else
                {
                    // For non-rings, first point has no previous
                    if (currentIndex <= 0)
                        return "-";
                    previousIndex = currentIndex - 1;
                }

                Locateable previousPoint = points[previousIndex];
                if (previousPoint == null)
                    return "-";

                var previousGeodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(previousPoint.X, previousPoint.Y));
                // Azimuth to previous is the bearing from current to previous
                double bearing = CalculateInitialBearing(currentGeodetic, previousGeodetic);
                return FormatAngle(bearing, viewModel.UseDecimalDegreesForAngles);
            }

            // Handle VertexAngle
            if (param == "VERTEXANGLE")
            {
                int previousIndex, nextIndex;
                
                if (isRing)
                {
                    // For rings, wrap around: first point uses last point, last point uses first point
                    previousIndex = (currentIndex - 1 + points.Count) % points.Count;
                    nextIndex = (currentIndex + 1) % points.Count;
                }
                else
                {
                    // For non-rings, need both previous and next points
                    if (currentIndex <= 0 || currentIndex >= points.Count - 1)
                        return "-";
                    previousIndex = currentIndex - 1;
                    nextIndex = currentIndex + 1;
                }

                Locateable previousPoint = points[previousIndex];
                Locateable nextPoint = points[nextIndex];
                
                if (previousPoint == null || nextPoint == null)
                    return "-";

                var previousGeodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(previousPoint.X, previousPoint.Y));
                var nextGeodetic = MapProjects.WebMercatorToGeodeticWgs84(new Point(nextPoint.X, nextPoint.Y));

                double vertexAngle = CalculateVertexAngle(previousGeodetic, currentGeodetic, nextGeodetic, viewModel.ShowInnerAngle);
                return FormatAngle(vertexAngle, viewModel.UseDecimalDegreesForAngles);
            }
        }
        catch
        {
            return "-";
        }

        return "-";
    }

    /// <summary>
    /// Calculates the initial bearing (azimuth) from point1 to point2 using spherical bearing formula.
    /// Returns bearing in degrees (0-360).
    /// </summary>
    private double CalculateInitialBearing(Point point1, Point point2)
    {
        // Convert degrees to radians
        double lat1 = point1.Y * Math.PI / 180.0;
        double lat2 = point2.Y * Math.PI / 180.0;
        double deltaLon = (point2.X - point1.X) * Math.PI / 180.0;

        // Spherical bearing formula
        // bearing = atan2(sin(Δλ) * cos(φ2), cos(φ1) * sin(φ2) - sin(φ1) * cos(φ2) * cos(Δλ))
        double y = Math.Sin(deltaLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

        // Calculate bearing in radians
        double bearing = Math.Atan2(y, x);

        // Convert to degrees and normalize to 0-360
        double bearingDegrees = bearing * 180.0 / Math.PI;
        
        // Normalize to 0-360
        bearingDegrees = (bearingDegrees + 360) % 360;

        return bearingDegrees;
    }

    /// <summary>
    /// Calculates the vertex angle (interior or exterior angle) at the current point between the previous edge and next edge.
    /// Returns angle in degrees (0-180 for inner, 180-360 for outer).
    /// </summary>
    private double CalculateVertexAngle(Point previousPoint, Point currentPoint, Point nextPoint, bool showInnerAngle)
    {
        // Calculate bearings from current point:
        // - Bearing from current to previous (reverse direction of incoming edge)
        // - Bearing from current to next (outgoing edge direction)
        double bearingFromPrevious = CalculateInitialBearing(previousPoint, currentPoint);
        double bearingToPrevious = (bearingFromPrevious + 180) % 360; // Reverse to get direction from current to previous
        double bearingToNext = CalculateInitialBearing(currentPoint, nextPoint);

        // Calculate the angle between the two directions
        // This is the smaller angle between the two bearings (0-180)
        double angleDifference = Math.Abs(bearingToNext - bearingToPrevious);
        
        // Take the smaller angle (if > 180, use 360 - angle)
        if (angleDifference > 180)
        {
            angleDifference = 360 - angleDifference;
        }
        
        // The inner vertex angle is the interior angle, which equals the angle between the two directions
        double innerAngle = Math.Max(0, Math.Min(180, angleDifference));
        
        // Return inner or outer angle based on setting
        if (showInnerAngle)
        {
            return innerAngle;
        }
        else
        {
            // Outer angle = 360° - inner angle
            return 360 - innerAngle;
        }
    }

    /// <summary>
    /// Formats an angle as either decimal degrees or DMS based on the setting.
    /// </summary>
    private string FormatAngle(double angleDegrees, bool useDecimalDegrees)
    {
        if (useDecimalDegrees)
        {
            // Format as decimal degrees with appropriate precision
            return $"{angleDegrees:F6}°";
        }
        else
        {
            // Format as DMS
            return DegreeHelper.ToDms(angleDegrees, true);
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

