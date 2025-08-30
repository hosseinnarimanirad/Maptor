using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.MachineLearning;

/// <summary>
/// Logistic Regression Simplification Training Data
/// </summary>
/// <typeparam name="T"></typeparam>
public class LRSimplificationTrainingData<T> where T : IPoint, new()
{
    public List<LRSimplificationParameters<T>> Records { get; set; } = new List<LRSimplificationParameters<T>>();

    public List<LRSimplificationFeatures> Features { get; set; } = new List<LRSimplificationFeatures>();

    public LRSimplificationTrainingData() : this(new List<LRSimplificationParameters<T>>(), new List<LRSimplificationFeatures>())
    {

    }

    public LRSimplificationTrainingData(List<LRSimplificationParameters<T>> records) : this(records, records.First().Features)
    {
    }

    private LRSimplificationTrainingData(List<LRSimplificationParameters<T>> records, List<LRSimplificationFeatures> features)
    {
        this.Records = records;

        this.Features = features;
    }


    public bool IsNullOrEmpty()
    {
        return this.Records.IsNullOrEmpty() || this.Features.IsNullOrEmpty();
    }
    public int GetCountOfFeatures()
    {
        return Features?.Count ?? 0;
    }

    public int GetCountOfRecords()
    {
        return Records?.Count ?? 0;
    }

    public void SaveAsJson(string fileName)
    { 
        var jsonString = JsonHelper.SerializeWithIgnoreNullOption(this);

        System.IO.File.WriteAllText(fileName, jsonString);
    }

    public void SaveAsCsv(string fileName, string decisionFieldName = "IsRetained")
    {
        // add header
        var lines = new List<string>
        {
            string.Join(",", this.Features.Select(f => f.ToString()).Concat([decisionFieldName]))
        };

        foreach (var record in this.Records)
        {
            lines.Add(string.Join(",", record.FeatureValues.Concat([record.IsRetained ? 1 : 0])));
        }

        System.IO.File.WriteAllLines(fileName, lines);
    }


    // ورودی این متد بایستی نقاط تمیز شده باشد
    public static LRSimplificationTrainingData<T> Create(
        List<T> originalPoints,
        List<T> simplifiedPoints,
        bool isRingMode,
        List<LRSimplificationFeatures> features,
        Func<T, T> toScreenMap = null)
    {
        if (originalPoints.IsNullOrEmpty() || simplifiedPoints.IsNullOrEmpty())
            return new LRSimplificationTrainingData<T>();

        List<LRSimplificationParameters<T>> parameters = new List<LRSimplificationParameters<T>>();

        // تعیین ارتباط بین اندکس نقطه در لیست 
        // اصلی و اندکس نقطه در لیست ساده شده
        Dictionary<int, int> indexMap = new Dictionary<int, int>();

        for (int simplifiedIndex = 0; simplifiedIndex < simplifiedPoints.Count; simplifiedIndex++)
        {
            var currentPoint = simplifiedPoints[simplifiedIndex];

            for (int originalIndex = simplifiedIndex; originalIndex < originalPoints.Count; originalIndex++)
            {
                if (IRI.Maptor.Sta.Spatial.Analysis.SpatialUtility.GetEuclideanDistance(currentPoint, originalPoints[originalIndex]) < IRI.Maptor.Sta.Spatial.Analysis.SpatialUtility.EpsilonDistance)
                {
                    indexMap.Add(simplifiedIndex, originalIndex);

                    break;
                }
            }
        }

        if (indexMap.Count != simplifiedPoints.Count)
            throw new NotImplementedException("خطا در شناسایی معادل نقطه عارضه ساده شده در عارضه اصلی");

        var startIndex = indexMap.First().Value;
        var middleIndex = originalPoints.Count <= startIndex + 1 ? 0 : startIndex + 1;
        var lastIndex = originalPoints.Count <= middleIndex + 1 ? 0 : middleIndex + 1;

        do
        {
            if (!isRingMode && lastIndex <= startIndex)
                break;

            // نقطه حفظ شده
            if (indexMap.ContainsValue(middleIndex))
            {
                parameters.Add(new LRSimplificationParameters<T>(
                                       originalPoints[startIndex],
                                       originalPoints[middleIndex],
                                       originalPoints[lastIndex],
                                       features,
                                       toScreenMap)
                {
                    IsRetained = true,
                });

                startIndex = middleIndex;
            }
            // نقطه حذف نشده
            else
            {
                parameters.Add(new LRSimplificationParameters<T>(
                                      originalPoints[startIndex],
                                      originalPoints[middleIndex],
                                      originalPoints[lastIndex],
                                      features,
                                      toScreenMap)
                {
                    IsRetained = false,
                });
            }

            middleIndex = lastIndex;
            lastIndex = originalPoints.Count <= middleIndex + 1 ? 0 : middleIndex + 1;

        } while (middleIndex != indexMap.First().Value);

        return new LRSimplificationTrainingData<T>(parameters, features);
    }

    public static LRSimplificationTrainingData<Point> LoadFromJson(string fileName)
    {
        try
        {
            var jsonString = System.IO.File.ReadAllText(fileName);

            return JsonHelper.Deserialize<LRSimplificationTrainingData<Point>>(jsonString);
        }
        catch (Exception)
        {
            return null;
        }
    }

}
