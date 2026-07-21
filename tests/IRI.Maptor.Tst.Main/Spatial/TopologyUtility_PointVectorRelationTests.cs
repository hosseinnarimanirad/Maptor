using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Topology;

namespace IRI.Maptor.Tst.Spatial;

public class TopologyUtility_PointVectorRelationTests
{
    [Fact]
    public void GetPointVectorRelation_DefaultTolerance_ClassifiesLeftRightAndOnLine()
    {
        var start = new Point(0, 0);
        var end = new Point(10, 0);

        Assert.Equal(PointVectorRelation.LiesLeft, TopologyUtility.GetPointVectorRelation(new Point(5, 1), start, end));
        Assert.Equal(PointVectorRelation.LiesRight, TopologyUtility.GetPointVectorRelation(new Point(5, -1), start, end));
        Assert.Equal(PointVectorRelation.LiesOnTheLine, TopologyUtility.GetPointVectorRelation(new Point(5, 0), start, end));
    }

    [Fact]
    public void GetPointVectorRelation_NearCollinearPoint_UsesTolerance()
    {
        var start = new Point(0, 0);
        var end = new Point(10, 0);

        // cross product = 1e-8: strictly left with the exact default, on the line with a tolerance
        var nearlyLeft = new Point(5, 1e-9);

        Assert.Equal(PointVectorRelation.LiesLeft, TopologyUtility.GetPointVectorRelation(nearlyLeft, start, end));
        Assert.Equal(PointVectorRelation.LiesOnTheLine, TopologyUtility.GetPointVectorRelation(nearlyLeft, start, end, tolerance: 1e-6));

        var nearlyRight = new Point(5, -1e-9);

        Assert.Equal(PointVectorRelation.LiesRight, TopologyUtility.GetPointVectorRelation(nearlyRight, start, end));
        Assert.Equal(PointVectorRelation.LiesOnTheLine, TopologyUtility.GetPointVectorRelation(nearlyRight, start, end, tolerance: 1e-6));
    }
}
