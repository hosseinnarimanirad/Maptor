using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Res.TrajectoryCompression;

public class SingleSimplifiedGeometryLog
{
    // e.g. AdminArea_WM
    public string FileName { get; set; }

    public int FeatureIndex { get; set; }

    // e.g. AdminArea
    public string Type { get; set; }

    public int Level { get; set; }

    public double AreaThreshold { get; set; }

    public double DistanceThreshold { get; set; }

    public double Coef { get; set; }

    public string MethodName { get; set; }

    public long ElapsedMilliseconds { get; set; }

    // Max(With, Height)
    public double MaxExtent { get; set; }

    public int NumberOfPoints_Original { get; set; }
    public int NumberOfPoints_Simplified { get; set; }
    // درصد تغییرات در تعداد نقاط
    public double PCInCoordinates { get; set; }


    public double SegmentLengthSD_Original { get; set; }
    public double SegmentLengthSD_Simplified { get; set; }
    // تغییرات انحراف معیار طول قطعه‌ها
    public double PCInSegmentLengthSD { get; set; }


    public double PointDensity_Original { get; set; }
    public double PointDensity_Simplified { get; set; }
    // تغییرات تراکم
    public double PCInPointDensity { get; set; }


    public double Length_Original { get; set; }
    public double Length_Simplified { get; set; }
    // درصد تغییرات طول خط
    public double PCInLineLength { get; set; }


    public double Angularity_Original { get; set; }
    public double Angularity_Simplified { get; set; }
    // درصد تغییرات در تغییرات زاویه
    public double PCInAngularity { get; set; }


    public double Curvilinearity_Original { get; set; }
    public double Curvilinearity_Simplified { get; set; }
    public double PCInCurvilinearity { get; set; }


    public double TotalVectorDisplacement { get; set; }
    public double TotalVectorDisplacementAtWindowScale { get; set; }
    public double TotalVectorDisplacementPerLength { get; set; }

    public SingleSimplifiedGeometryLog(
        string fileName,
        BoundingBox originalBoundingBox,
        int featureIndex,
        Geometry<Point> original,
        Geometry<Point> simplified,
        int level,
        string methodName,
        long elapsedMilliseconds,
        SimplificationParameters parameters,
        double coef)
    {
        this.FileName = fileName;

        this.Type = fileName.Split('_').First();

        this.FeatureIndex = featureIndex;

        this.Level = level;

        this.AreaThreshold = parameters.AreaThreshold ?? 0;

        this.DistanceThreshold = parameters.DistanceThreshold ?? 0;

        this.Coef = coef;

        this.MethodName = methodName;

        this.ElapsedMilliseconds = elapsedMilliseconds;

        this.MaxExtent = Math.Max(originalBoundingBox.Width, originalBoundingBox.Height);

        this.NumberOfPoints_Original = original.TotalNumberOfPoints;
        this.NumberOfPoints_Simplified = simplified.TotalNumberOfPoints;
        this.PCInCoordinates = original.PercentageChangeInCoordinates(simplified);


        //this.SegmentLengthSD_Original = original.CalculateSegmentLengthVariations();
        //this.SegmentLengthSD_Simplified = simplified.CalculateSegmentLengthVariations();
        //this.PCInSegmentLengthSD = original.PercentageChangeInSegmentLengthVariations(simplified);

        this.PointDensity_Original = original.CalculatePointDensity();
        this.PointDensity_Simplified = simplified.CalculatePointDensity();
        this.PCInPointDensity = original.PercentageChangeInPointDensity(simplified);

        this.Length_Original = original.GetEuclideanLength();
        this.Length_Simplified = simplified.GetEuclideanLength();
        this.PCInLineLength = original.PercentageChangeInLineLength(simplified);

        this.Angularity_Original = original.CalculateMeanAngularChange();
        this.Angularity_Simplified = simplified.CalculateMeanAngularChange();
        this.PCInAngularity = original.PercentageChangeInAngularity(simplified);

        this.Curvilinearity_Original = original.GetNumerOfCurvilinearityChange();
        this.Curvilinearity_Simplified = simplified.GetNumerOfCurvilinearityChange();
        this.PCInCurvilinearity = original.PercentageChangeInCurvilinearSegments(simplified);

        this.TotalVectorDisplacement = (methodName == "APSC") ? 0 : original.CalculateTotalVectorDisplacement(simplified);

        //this.TotalVectorDisplacementAtWindowScale = WebMercatorUtility.ToScreenLength(level, TotalVectorDisplacement);

        if (Length_Original != 0)
            this.TotalVectorDisplacementPerLength = this.TotalVectorDisplacement / Length_Original;
    }

    public static string GetHeader()
    {
        var headers = new List<string>()
        {
            nameof(FileName),
            nameof(Type),
            nameof(FeatureIndex),
            nameof(Level),
            nameof(Coef),
            nameof(AreaThreshold),
            nameof(DistanceThreshold),
            nameof(MethodName),
            nameof(ElapsedMilliseconds),

            nameof(MaxExtent),

            nameof(NumberOfPoints_Original),
            nameof(NumberOfPoints_Simplified),
            nameof(PCInCoordinates),

            //nameof(SegmentLengthSD_Original),
            //nameof(SegmentLengthSD_Simplified),
            //nameof(PCInSegmentLengthSD),

            nameof(PointDensity_Original),
            nameof(PointDensity_Simplified),
            nameof(PCInPointDensity),

            nameof(Length_Original),
            nameof(Length_Simplified),
            nameof(PCInLineLength),

            nameof(Angularity_Original),
            nameof(Angularity_Simplified),
            nameof(PCInAngularity),

            nameof(Curvilinearity_Original),
            nameof(Curvilinearity_Simplified),
            nameof(PCInCurvilinearity),

            nameof(TotalVectorDisplacement),
            nameof(TotalVectorDisplacementPerLength),
            nameof(TotalVectorDisplacementAtWindowScale)

        };

        return string.Join("\t", headers);
    }

    public string ToTsv()
    {
        var headers = new List<string>()
        {
            FileName,
            Type,
            FeatureIndex.ToString(),
            Level.ToString(),
            Coef.ToString("N2"),
            AreaThreshold.ToString(),
            DistanceThreshold.ToString(),

            MethodName,
            ElapsedMilliseconds.ToString(),

            MaxExtent.ToString("N1"),

            NumberOfPoints_Original.ToString(),
            NumberOfPoints_Simplified.ToString(),
            PCInCoordinates.ToString("N3"),

            //SegmentLengthSD_Original.ToString("N2"),
            //SegmentLengthSD_Simplified.ToString("N2"),
            //PCInSegmentLengthSD.ToString("N3"),

            PointDensity_Original.ToString("N2"),
            PointDensity_Simplified.ToString("N2"),
            PCInPointDensity.ToString("N3"),

            Length_Original.ToString("N1"),
            Length_Simplified.ToString("N1"),
            PCInLineLength.ToString("N3"),

            (Angularity_Original * 180.0/Math.PI).ToString("N3"),
            (Angularity_Simplified* 180.0/Math.PI).ToString("N3"),
            PCInAngularity.ToString("N3"),

            Curvilinearity_Original.ToString("N1"),
            Curvilinearity_Simplified.ToString("N1"),
            PCInCurvilinearity.ToString("N3"),

            TotalVectorDisplacement.ToString("N2"),
            TotalVectorDisplacementPerLength.ToString("N3"),
            TotalVectorDisplacementAtWindowScale.ToString("N3")
        };

        return string.Join("\t", headers);
    }
}
