// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.DataStructures.CustomStructures;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing.ImageMatching;

public class SiftImageMatching
{
    List<ImageDescriptors> database;

    public SiftImageMatching(List<ImageDescriptors> database)
    {
        this.database = database;
    }

    public int FindMatch(ImageDescriptors image, double threshold)
    {
        List<IndexValue<double>> values = CalculateSimilarity(image, threshold);

        // best (highest-similarity) match
        var best = values[0];

        for (int i = 1; i < values.Count; i++)
        {
            if (values[i].Value > best.Value)
            {
                best = values[i];
            }
        }

        return best.Index;
    }

    //Lowe use threshold = 0.8
    public List<IndexValue<double>> CalculateSimilarity(ImageDescriptors image, double threshold)
    {
        List<IndexValue<double>> values = new List<IndexValue<double>>();

        for (int i = 0; i < database.Count; i++)
        {
            Dictionary<int, int> temp = Compare(this.database[i], image, threshold);

            values.Add(new IndexValue<double>(i, temp.Count));
        }

        return values;
    }


    public Dictionary<int, int> Compare(ImageDescriptors referenceImage, ImageDescriptors targetImage, double threshold)
    {
        Dictionary<int, int> result = new Dictionary<int, int>();

        for (int i = 0; i < targetImage.Count; i++)
        {
            // Lowe's ratio test only needs the two smallest angles; a single
            // scan avoids sorting the whole array per target descriptor
            var best = new IndexValue<double>(-1, double.PositiveInfinity);
            var secondBest = new IndexValue<double>(-1, double.PositiveInfinity);

            for (int j = 0; j < referenceImage.Count; j++)
            {
                double angle = Descriptor.CalculateAngle(referenceImage[j], targetImage[i]);

                if (double.IsNaN(angle))
                {
                    throw new NotImplementedException();
                }

                if (angle < best.Value)
                {
                    secondBest = best;

                    best = new IndexValue<double>(j, angle);
                }
                else if (angle < secondBest.Value)
                {
                    secondBest = new IndexValue<double>(j, angle);
                }
            }

            if (best.Value / secondBest.Value < threshold)
            {
                result.Add(i, best.Index);
            }
        }

        return result;
    }
}
