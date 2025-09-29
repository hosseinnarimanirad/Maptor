using System;
using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Extensions;


namespace IRI.Maptor.Res.TrajectoryCompression;

public class NineAlgoSixMeasureLog
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

    //public string MethodName { get; set; }
     
    // Max(With, Height)
    public double MaxExtent { get; set; }

    public int NumberOfPoints_Original { get; set; }

    public List<AlgoMeasure> Measures { get; set; } = new List<AlgoMeasure>();

    public double GetTlvdForRDP()
    {
        return Measures.FirstOrDefault(x => x.Algorithm == SimplificationType.RamerDouglasPeucker && x.Measure == CharacteristicsMeasure.TLVD)?.Value ?? 0;
    }

    public double GetTlvdForSF()
    {
        return Measures.FirstOrDefault(x => x.Algorithm == SimplificationType.SleeveFitting && x.Measure == CharacteristicsMeasure.TLVD)?.Value ?? 0;
    }

    public double GetSF_RDP_TlvdDiff()
    {
        return GetTlvdForSF() - GetTlvdForRDP();
    }
     
    public double PointDensity_Original { get; set; }
     
    public double Length_Original { get; set; }
     
    public double Angularity_Original { get; set; }
    
    public double Curvilinearity_Original { get; set; }
     
    public NineAlgoSixMeasureLog(
        string fileName,
        BoundingBox originalBoundingBox,
        int featureIndex,
        Geometry<Point> original,
        //Geometry<Point> simplified,
        int level,
        //string methodName,
        //long elapsedMilliseconds,
        SimplificationParamters parameters,
        double coef)
    {
        this.FileName = fileName;

        this.Type = fileName.Split('_').First();

        this.FeatureIndex = featureIndex;

        this.Level = level;

        this.AreaThreshold = parameters.AreaThreshold ?? 0;

        this.DistanceThreshold = parameters.DistanceThreshold ?? 0;

        this.Coef = coef;

        //this.MethodName = methodName;
         
        this.MaxExtent = Math.Max(originalBoundingBox.Width, originalBoundingBox.Height);

        this.NumberOfPoints_Original = original.TotalNumberOfPoints;
        //this.NumberOfPoints_Simplified = simplified.TotalNumberOfPoints;
        //this.PCInCoordinates = original.PercentageChangeInCoordinates(simplified);


        this.PointDensity_Original = original.CalculatePointDensity();
        //this.PointDensity_Simplified = simplified.CalculatePointDensity();
        //this.PCInPointDensity = original.PercentageChangeInPointDensity(simplified);

        this.Length_Original = original.GetEuclideanLength();
        //this.Length_Simplified = simplified.CalculateEuclideanLength();
        //this.PCInLineLength = original.PercentageChangeInLineLength(simplified);

        this.Angularity_Original = original.CalculateMeanAngularChange();
        //this.Angularity_Simplified = simplified.CalculateMeanAngularChange();
        //this.PCInAngularity = original.PercentageChangeInAngularity(simplified);

        this.Curvilinearity_Original = original.GetNumerOfCurvilinearityChange();
        //this.Curvilinearity_Simplified = simplified.GetNumerOfCurvilinearityChange();
        //this.PCInCurvilinearity = original.PercentageChangeInCurvilinearSegments(simplified);

        //this.TotalVectorDisplacement = original.CalculateTotalVectorDisplacement(simplified);

        //if (Length_Original != 0)
        //    this.TotalVectorDisplacementPerLength = this.TotalVectorDisplacement / Length_Original;
    }

    public void AddAlgoResult(SimplificationType algorithm, Geometry<Point> original, Geometry<Point> simplified)
    {
        var tlvdPerLength = (Length_Original != 0 && algorithm != SimplificationType.APSC) ? original.CalculateTotalVectorDisplacement(simplified) / Length_Original : 0;
         
        this.Measures.Add(new AlgoMeasure(algorithm, CharacteristicsMeasure.PCC, original.PercentageChangeInCoordinates(simplified)));
        this.Measures.Add(new AlgoMeasure(algorithm, CharacteristicsMeasure.PDD, original.PercentageChangeInPointDensity(simplified)));
        this.Measures.Add(new AlgoMeasure(algorithm, CharacteristicsMeasure.PCLL, original.PercentageChangeInLineLength(simplified)));
        this.Measures.Add(new AlgoMeasure(algorithm, CharacteristicsMeasure.PCANGLE, original.PercentageChangeInAngularity(simplified)));
        this.Measures.Add(new AlgoMeasure(algorithm, CharacteristicsMeasure.PCCS, original.PercentageChangeInCurvilinearSegments(simplified)));
        this.Measures.Add(new AlgoMeasure(algorithm, CharacteristicsMeasure.TLVD, tlvdPerLength));

    }
     
    public void UpdateAllRanks()
    {
        var groups = Measures.GroupBy(m => m.Measure).ToList();

        foreach (var group in groups)
        {
            UpdateRanksAndCheckForDuplicateRanks(group.ToList());
        }
    }

    private void UpdateRanksAndCheckForDuplicateRanks(List<AlgoMeasure> measures)
    {
        var ranks = measures.Select(m => m.Value)
                            .Distinct()
                            .OrderByDescending(v => v)
                            .Select((v, index) => new { Value = v, Rank = index + 1 })
                            .ToDictionary(x => x.Value, x => x.Rank);

        foreach (var measure in measures)
        {
            measure.Rank = ranks[measure.Value];
        }
    }
     
    public string GetHeader()
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

                //nameof(MethodName), 

                nameof(MaxExtent),

                nameof(NumberOfPoints_Original),
                nameof(PointDensity_Original),
                nameof(Length_Original),
                nameof(Angularity_Original),
                nameof(Curvilinearity_Original),
        };

        headers.AddRange(this.Measures.SelectMany(m => new string[]
        {
            $"{m.GetTitle()}",
            $"{m.GetTitle()}_Rank"
        }));
         
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

                //MethodName,
                //ElapsedMilliseconds.ToString(),

                MaxExtent.ToString("N1"),

                NumberOfPoints_Original.ToString(),
                PointDensity_Original.ToString("N2"),
                Length_Original.ToString("N1"),
                (Angularity_Original * 180.0/Math.PI).ToString("N3"),
                Curvilinearity_Original.ToString("N1"),
            };
         
        headers.AddRange(this.Measures.SelectMany(m => new string[] { m.Value.ToString("N3"), m.Rank.ToString() }));

        return string.Join("\t", headers);
    }
}


public class AlgoMeasure
{
    public SimplificationType Algorithm { get; set; }

    public CharacteristicsMeasure Measure { get; set; }

    public double Value { get; set; }

    public int Rank { get; set; }

    public AlgoMeasure(SimplificationType algorithm, CharacteristicsMeasure measure, double value)
    {
        Algorithm = algorithm;
        Measure = measure;
        Value = Math.Round(value, 3);
    }

    public string GetTitle()
    {
        return $"{Measure.GetDescription()}_{Algorithm.GetDescription()}";
    }

    public override string ToString()
    {
        return $"{Algorithm.GetDescription()}-{Measure.GetDescription()}: Value: {Value}, Rank: {Rank}";
    }
}