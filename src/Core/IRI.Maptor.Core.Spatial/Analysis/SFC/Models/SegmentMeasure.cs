using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis.SFC;

public static class SegmentMeasure
{
    public static RegionMoves Measure(SpaceFillingCurve sFC, int level, firstToOtherSubregionTransforms firstToOtherSubregions)
    {
        RegionMoves totalRegionMoves = new RegionMoves();

        RegionMoves regionMoves = new RegionMoves();

        if (level == 0)
        {
            regionMoves.Add(sFC);

            return regionMoves;
        }
        else
        {
            regionMoves = Measure(sFC.CalculateSubregionBMFs(0), level - 1, firstToOtherSubregions);

            totalRegionMoves = RegionMoves.Combine(regionMoves, totalRegionMoves);

            List<Transform> transforms = firstToOtherSubregions(sFC.CalculateSubregionBMFs(0).GetMoves());

            int subRegionSize = (int)Math.Pow(sFC.BaseSize, level);

            for (int i = 0; i < sFC.NumberOfSteps - 1; i++)
            {
                RegionMoves otherRegionMoves = regionMoves.Transform(transforms[i]);

                totalRegionMoves = RegionMoves.Combine(otherRegionMoves, totalRegionMoves);

                Point tempPoint01 = sFC.CalculateSubregionBMFs(i).TraverseTheBasePath(new Point(0, 0), subRegionSize - 1).Last();

                Point tempPoint02 = sFC.MoveToNextRegion(i, new Point(0, 0), subRegionSize);

                totalRegionMoves.Add(new Move((p, s) => new Point(p.X + tempPoint02.X - tempPoint01.X, p.Y + tempPoint02.Y - tempPoint01.Y)));
            }

            return totalRegionMoves;
        }
    }

}
