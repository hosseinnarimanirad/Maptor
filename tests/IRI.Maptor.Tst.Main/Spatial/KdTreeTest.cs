using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.AdvancedStructures;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;

namespace IRI.Maptor.Tst.NetFrameworkTest.Spatial
{
    
    public class KdTreeTest
    {
        [Fact]
        public void TestNearestNeighbour()
        {
            List<Point> points = new List<Point>
            {
                new Point(1, 9),
                new Point(2, 2),
                new Point(2, 5),
                new Point(2, 10),
                new Point(2, 12),
                new Point(3, 7),
                new Point(4, 11),
                new Point(5, 8),
                new Point(6, 7),
                new Point(7, 3),
                new Point(7, 4),
                new Point(7, 5),
                new Point(7, 11),
                new Point(8, 3),
                new Point(8, 4),
                new Point(8, 5),
                new Point(9, 3),
                new Point(9, 4),
                new Point(9, 5),
                new Point(9, 10),
                new Point(9, 11),
                new Point(10, 3),
                new Point(10, 10),
                new Point(10, 11),
                new Point(11, 6)
            };

            Func<Point, Point, int> xWise = (p1, p2) => p1.X.CompareTo(p2.X);
            Func<Point, Point, int> yWise = (p1, p2) => p1.Y.CompareTo(p2.Y);
            Func<Point, Point, int>[] funcs = { xWise, yWise };
            var kdtree = new BalancedKdTree<Point>(points.ToArray(), funcs.ToList(), Point.NaN, i => i);

            Assert.Equal(new Point(6, 7), kdtree.FindNearestNeighbour(new Point(7, 7)));
            Assert.Equal(new Point(7, 4), kdtree.FindNearestNeighbour(new Point(7, 4)));
            Assert.Equal(new Point(11, 6), kdtree.FindNearestNeighbour(new Point(10, 6)));
            Assert.Equal(new Point(4, 11), kdtree.FindNearestNeighbour(new Point(3, 11)));

            for (int i = 0; i < 100; i++)
            {
                var point = new Point(Math.Sin(RandomHelper.Get(0, 100)) * 20,
                                        Math.Cos(RandomHelper.Get(0, 100)) * 20);

                System.Diagnostics.Debug.WriteLine(point.ToString());

                Assert.Equal(kdtree.FindNearestNeighbour(point), FindNearestBruteForce(points, point));
            }
        }

        [Fact]
        public void TestBoundingBox()
        {
            List<Point> points =
            [
                new Point(1, 9),
                new Point(2, 2),
                new Point(2, 5),
                new Point(2, 10),
                new Point(2, 12),
                new Point(3, 7),
                new Point(4, 11),
                new Point(5, 8),
                new Point(6, 7),
                new Point(7, 3),
                new Point(7, 4),
                new Point(7, 5),
                new Point(7, 11),
                new Point(8, 3),
                new Point(8, 4),
                new Point(8, 5),
                new Point(9, 3),
                new Point(9, 4),
                new Point(9, 5),
                new Point(9, 10),
                new Point(9, 11),
                new Point(10, 3),
                new Point(10, 10),
                new Point(10, 11),
                new Point(11, 6),
            ];

            Func<Point, Point, int> xWise = (p1, p2) => p1.X.CompareTo(p2.X);
            Func<Point, Point, int> yWise = (p1, p2) => p1.Y.CompareTo(p2.Y);
            Func<Point, Point, int>[] funcs = { xWise, yWise };
            var kdtree = new BalancedKdTree<Point>(points.ToArray(), funcs.ToList(), Point.NaN, i => i);

            Assert.Equal(true, CheckBoundingBox(kdtree.Root));


        }



        private Point FindNearestBruteForce(List<Point> dataSet, Point targetPoint)
        {
            var minDistance = SpatialUtility.GetEuclideanDistance(dataSet[0], targetPoint);

            var result = dataSet.First();

            for (int i = 0; i < dataSet.Count; i++)
            {
                var distance = SpatialUtility.GetEuclideanDistance(dataSet[i], targetPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;

                    result = dataSet[i];
                }
            }

            return result;
        }

        private bool CheckBoundingBox(BalancedKdTreeNode<Point> node)
        {
            var result = node.MinimumBoundingBox == node.CalculateBoundingBox();

            if (node.LeftChild != null && !node.LeftChild.IsNilNode())
            {
                result = result && CheckBoundingBox(node.LeftChild);
            }

            if (node.RightChild != null && !node.RightChild.IsNilNode())
            {
                result = result && CheckBoundingBox(node.RightChild);
            }

            return result;
        }

    }
}
