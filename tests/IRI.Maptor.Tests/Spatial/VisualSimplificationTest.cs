using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Tests.Spatial
{

    public class VisualSimplificationTest
    {
        [Fact]
        public void TestSimplifyByDouglasPeucker()
        {
            List<Point> originalList = new List<Point>() {
                new Point(0.0,0.0),
                new Point(1.0,0.1),
                new Point(2.0,-0.1),
                new Point(3.0,5.0),
                new Point(4.0,6.0),
                new Point(5.0,7.0),
                new Point(6.0,8.1),
                new Point(7.0,9.0),
                new Point(8.0,9.0),
                new Point(9.0,9.0),
            };

            var result = Simplifications.SimplifyByRamerDouglasPeucker(originalList, new SimplificationParameters() { DistanceThreshold = 1.0 });

            Assert.Equal(result.Count, 5);

            Assert.Equal(result[0], new Point(0, 0));
            Assert.Equal(result[1], new Point(2, -0.1));
            Assert.Equal(result[2], new Point(3, 5));
            Assert.Equal(result[3], new Point(7, 9));
            Assert.Equal(result[4], new Point(9, 9));
        }
         

        [Fact]
        public void TestSimplifyByPerpendicular()
        {
            List<Point> originalList = new List<Point>() {
                new Point(-5.68944, 1.62113),//A
                new Point(-4.95, 3.4),//B
                new Point(-3.88016, 2.64869),//C
                new Point(-3.39, 2.96),//D
                new Point(-3.81082, 0.66291),//E
                new Point(-0.99289, 1.96785),//F
                new Point(-0.67, 1.74),//G                
                new Point(-0.97398, 1.21767), //L
                new Point(-0.56421, 0.99072) //M
            };

            var result = Simplifications.SimplifyByPerpendicularDistance(originalList, new SimplificationParameters() { DistanceThreshold = 1 });

            Assert.Equal(6, result.Count);
            Assert.Equal(true, result.Contains(originalList[1]));
            Assert.Equal(false, result.Contains(originalList[2]));
            Assert.Equal(false, result.Contains(originalList[5]));
            Assert.Equal(false, result.Contains(originalList[7]));

        }


        // todo: test has not be runned
        // https://github.com/geobarry/line-simplify/blob/master/example_usage.py
        [Fact]
        public void TestSimplifyByAPCS()
        {
            var originalList = new List<Point>
            {
                new Point(47.2, 86.4),
                new Point(63.9, 103.3),
                new Point(63.9, 120.0),
                new Point(63.9, 136.8),
                new Point(63.8, 153.6),
                new Point(79.6, 170.4),
                new Point(79.6, 187.2),
                new Point(96.3, 202.9),
                new Point(96.2, 219.7),
                new Point(112.9, 236.5),
                new Point(129.5, 253.3),
                new Point(145.3, 270.2),
                new Point(145.3, 286.9),
                new Point(162.0, 302.6),
                new Point(178.7, 319.4),
                new Point(194.5, 336.3),
                new Point(211.1, 353.1),
                new Point(227.8, 369.9),
                new Point(243.6, 386.7),
                new Point(243.5, 403.5)
            };

            // Create the simplification table
            var simplifiedList = Simplifications.SimplifyByAPSC(originalList, new SimplificationParameters() { AreaThreshold = 50 });

            //[(47.2, 86.4),
            Assert.Equal(new Point(47.2, 86.4), simplifiedList[0]);

            //(63.899999999999984, 103.29999999999998),
            Assert.Equal(new Point(63.899999999999984, 103.29999999999998), simplifiedList[1]);

            //(63.86645990808869, 153.67066623138544),
            Assert.Equal(new Point(63.86645990808869, 153.67066623138544), simplifiedList[2]);

            //(79.6, 170.4),
            Assert.Equal(new Point(79.6, 170.4), simplifiedList[3]);

            //(79.6, 187.2),
            Assert.Equal(new Point(79.6, 187.2), simplifiedList[4]);

            //(96.3, 202.9),
            Assert.Equal(new Point(96.3, 202.9), simplifiedList[5]);

            //(96.20029850746268, 219.6498507462693),
            Assert.Equal(new Point(96.20029850746268, 219.6498507462693), simplifiedList[6]);

            //(145.3, 269.56674570170946),
            Assert.Equal(new Point(145.3, 269.56674570170946), simplifiedList[7]);

            //(145.3, 285.9639203979555),
            Assert.Equal(new Point(145.3, 285.9639203979555), simplifiedList[8]);

            //(243.6035108575, 386.11017593999713),
            Assert.Equal(new Point(243.6035108575, 386.11017593999713), simplifiedList[9]);

            //(243.5, 403.5)]
            Assert.Equal(new Point(243.5, 403.5), simplifiedList[10]);
        }
    }
}
