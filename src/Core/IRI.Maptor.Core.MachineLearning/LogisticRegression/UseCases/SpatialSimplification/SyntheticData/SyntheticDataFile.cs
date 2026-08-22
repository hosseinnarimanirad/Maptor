using IRI.Maptor.Core.Common.Helpers;
using System.Collections.Generic;

namespace IRI.Maptor.Core.MachineLearning;

public class SyntheticDataFile
{
    public List<SyntheticDataItem> LineSamples { get; set; }

    public List<LRSimplificationFeatures> Features { get; set; }

    public void Save(string fileName)
    {
        System.IO.File.WriteAllText(fileName, JsonHelper.Serialize(this));
    }

    public static SyntheticDataFile Load(string fileName)
    {
        if (!System.IO.File.Exists(fileName))
            return null;

        var jsonString = System.IO.File.ReadAllText(fileName);

        if (string.IsNullOrWhiteSpace(jsonString))
            return null;

        return JsonHelper.Deserialize<SyntheticDataFile>(jsonString);
    }
}
