using IRI.Maptor.Sta.Common.IO.Gpx;
using Microsoft.SqlServer.Types;
using IRI.Maptor.Sta.SpatialReferenceSystem;
 
namespace IRI.Maptor.Extensions;

public static class GpxExtensions
{
    public static SqlGeography AsGeography(this GpxWaypoint point)
    {
        SqlGeographyBuilder builder = new SqlGeographyBuilder();

        builder.SetSrid(SridHelper.GeodeticWGS84);

        builder.BeginGeography(OpenGisGeographyType.Point);

        builder.BeginFigure(point.Latitude, point.Longitude);

        builder.EndFigure();

        builder.EndGeography();

        return builder.ConstructedGeography;
    }

    public static SqlGeography AsGeography(this GpxTrackPoint point)
    {
        SqlGeographyBuilder builder = new SqlGeographyBuilder();

        builder.SetSrid(SridHelper.GeodeticWGS84);

        builder.BeginGeography(OpenGisGeographyType.Point);

        builder.BeginFigure(point.Latitude, point.Longitude);

        builder.EndFigure();

        builder.EndGeography();

        return builder.ConstructedGeography;
    }

    public static SqlGeography? AsGeography(this GpxTrackSegment trackSegment)
    {
        SqlGeographyBuilder builder = new SqlGeographyBuilder();

        builder.SetSrid(SridHelper.GeodeticWGS84);

        //builder.BeginGeography(OpenGisGeographyType.LineString);
        if (trackSegment.TrackPoints.Count < 2)
            return null;

        AddTrackSegment(builder, trackSegment);

        //builder.EndGeography();

        return builder.ConstructedGeography.STIsValid().Value ? builder.ConstructedGeography : builder.ConstructedGeography.MakeValid();
    }

    public static SqlGeography AsGeography(this GpxTrack track)
    {
        SqlGeographyBuilder builder = new SqlGeographyBuilder();

        builder.SetSrid(SridHelper.GeodeticWGS84);

        builder.BeginGeography(OpenGisGeographyType.MultiLineString);

        foreach (var item in track.Segments)
        {
            if (item.TrackPoints.Count < 2)
                continue;

            AddTrackSegment(builder, item); 
        }

        builder.EndGeography();

        return builder.ConstructedGeography.STIsValid().Value ? builder.ConstructedGeography : builder.ConstructedGeography.MakeValid();
    }

    private static void AddTrackSegment(SqlGeographyBuilder builder, GpxTrackSegment segment)
    {
        builder.BeginGeography(OpenGisGeographyType.LineString);

        builder.BeginFigure(segment.TrackPoints[0].Latitude, segment.TrackPoints[0].Longitude);

        for (int i = 1; i < segment.TrackPoints.Count; i++)
        {
            builder.AddLine(segment.TrackPoints[i].Latitude, segment.TrackPoints[i].Longitude);
        }

        builder.EndFigure();

        builder.EndGeography();
    }
}
