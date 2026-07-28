using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IRI.Maptor.Sta.Common.Helpers;

public class RandomHelper
{
    //static RandomHelper()
    //{
    //    _random = new Random();

    //    syncLock = new object();
    //}

    //private static readonly Random _random;

    //private static readonly object syncLock;

    public static int Get(int minValue, int maxValue)
    {
        //lock (syncLock)
        //{ // synchronize
        //    return _random.Next(minValue, maxValue);
        //}
        return RandomNumberGenerator.GetInt32(minValue, maxValue);
    }

    public static double GetBetweenZeroAndOne()
    {
        //lock (syncLock)
        //{ // synchronize
        //    return _random.Next(0, 100) / 100.0;
        //}

        return RandomNumberGenerator.GetInt32(0, 2);
    }

    public static int Get(int maxValue)
    {
        return Get(0, maxValue);
    }
     
    public static T? RandomlyChooseOne<T>(List<T> list)
    {
        if (list.IsNullOrEmpty())
            throw new ArgumentException("List is null or empty");

        var index = RandomNumberGenerator.GetInt32(0, list.Count);

        return list.ElementAt(index);
    }

    public static List<T> RandomlyChooseMultiple<T>(List<T> list, int n)
    {
        if (list.IsNullOrEmpty())
            throw new ArgumentException("List is null or empty");

        if (n > list.Count)
            return list;

        HashSet<int> selectedIndices = new HashSet<int>();

        List<T> result = new List<T>();

        while (selectedIndices.Count < n)
        {
            var index = RandomNumberGenerator.GetInt32(0, list.Count);

            if (selectedIndices.Add(index))
            {
                result.Add(list[index]);
            }
        }

        return result;
    }

    public static List<TEnum> GetRandomEnumValues<TEnum>(int count) where TEnum : Enum
    {
        var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();

        if (count <= 0 || count > values.Count)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and the total number of enum values.");

        // Shuffle the list
        var random = new Random();
        values = values.OrderBy(_ => random.Next()).ToList();

        // Take the first 'count' items from the shuffled list
        return values.Take(count).ToList();
    }
     
    public static T? RandomlyChooseOne<T>(List<T> includedList, List<T>? excludedList = null)
    {
        if (excludedList.IsNullOrEmpty())
        {
            return RandomlyChooseOne(includedList);
        }
        else
        {
            return RandomlyChooseOne(includedList.Except(excludedList).ToList());
        }
    }

    /// <summary>
    /// Get a random double value between 0 and maximum (inclusive)
    /// </summary>
    /// <param name="maximum">Inclusive maximum</param>
    /// <returns></returns>
    public static double GetRandomValue(double maximum)
    {
        var random = RandomNumberGenerator.GetInt32(0, 100_001);

        var result = (random / 100_000.0d) * maximum;

        return result;
    }
     
    public static T RandomlyChooseOneBasedOnWeight<T>(List<RandomItem<T>> list)
    {
        var totalWeight = list.Sum(item => item.Weight);

        var min = 0d;

        for (int i = 0; i < list.Count; i++)
        {
            list[i].MinInclusive = min;

            min = min + list[i].Weight;
        }

        var randomNumber = GetRandomValue(totalWeight);

        RandomItem<T>? randomItem = null;

        if (randomNumber > totalWeight)
        {
            throw new NotImplementedException(
                "HoneylandRandom > RandomlyChooseOneBasedOnWeight > unexpected greater value than maximum!");
        }
        else if (randomNumber == totalWeight)
        {
            randomItem = list.LastOrDefault();
        }
        else
        {
            randomItem = list.FirstOrDefault(c => c.IsInRange(randomNumber));
        }

        if (randomItem is null)
            throw new NotImplementedException("HoneylandRandom > RandomlyChooseOneBasedOnWeight > null item");

        return randomItem.Item;
    }
}