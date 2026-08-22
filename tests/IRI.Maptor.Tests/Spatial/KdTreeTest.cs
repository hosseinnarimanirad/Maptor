using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Spatial.AdvancedStructures;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Analysis;

namespace IRI.Maptor.Tests.Spatial
{

    public class KdTreeTest
    {
        private static List<Func<Point, Point, int>> XyComparers()
        {
            return new List<Func<Point, Point, int>>
            {
                (p1, p2) => p1.X.CompareTo(p2.X),
                (p1, p2) => p1.Y.CompareTo(p2.Y),
            };
        }

        private static List<Point> SamplePoints()
        {
            return new List<Point>
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
                new Point(11, 6),
            };
        }

        /// <summary>
        /// Distinct points on an integer grid, so a value can never be inserted twice and
        /// set comparisons against brute force stay unambiguous.
        /// </summary>
        private static List<Point> DistinctRandomPoints(int seed, int count, int extent)
        {
            var random = new Random(seed);

            var seen = new HashSet<Point>();

            var result = new List<Point>();

            while (result.Count < count)
            {
                var point = new Point(random.Next(0, extent), random.Next(0, extent));

                if (seen.Add(point))
                {
                    result.Add(point);
                }
            }

            return result;
        }

        [Fact]
        public void TestNearestNeighbour()
        {
            List<Point> points = SamplePoints();

            var kdtree = new BalancedKdTree<Point>(points.ToArray(), XyComparers(), Point.NaN, i => i);

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

        /// <summary>
        /// The radius query used to skip the point held at every node it descended through —
        /// only whole subtrees falling inside the radius contributed, so the answer was a
        /// subset of the truth and the root's own point could never come back at all.
        /// </summary>
        [Fact]
        public void TestFindNeighboursMatchesBruteForce()
        {
            var points = DistinctRandomPoints(seed: 20260728, count: 400, extent: 60);

            var kdtree = new BalancedKdTree<Point>(points, XyComparers(), Point.NaN, i => i);

            // non-integer radii over an integer grid: no point sits exactly on the boundary
            var radii = new[] { 0.5, 3.3, 7.3, 11.7, 25.4, 100.0 };

            var random = new Random(97);

            foreach (var radius in radii)
            {
                for (int i = 0; i < 25; i++)
                {
                    var center = new Point(random.Next(-10, 70), random.Next(-10, 70));

                    var expected = points
                        .Where(p => SpatialUtility.GetEuclideanLength(p, center) <= radius)
                        .ToHashSet();

                    var actual = kdtree.FindNeighbours(center, radius);

                    Assert.Equal(expected.Count, actual.Count);
                    Assert.Equal(expected, actual.ToHashSet());
                }
            }
        }

        /// <summary>
        /// Querying a point that is itself in the tree must return that point.
        /// </summary>
        [Fact]
        public void TestFindNeighboursReturnsTheQueriedPointItself()
        {
            var points = SamplePoints();

            var kdtree = new BalancedKdTree<Point>(points, XyComparers(), Point.NaN, i => i);

            foreach (var point in points)
            {
                Assert.Contains(point, kdtree.FindNeighbours(point, 0.0));
            }

            // and a radius covering everything returns everything, root included
            Assert.Equal(points.Count, kdtree.FindNeighbours(new Point(6, 7), 1000).Count);
        }

        /// <summary>
        /// The point accessor and the nil node used to be static, so building a second tree
        /// over the same element type rebound the first tree's accessor underneath it.
        /// </summary>
        [Fact]
        public void TestTreesOverTheSameTypeAreIndependent()
        {
            var points = SamplePoints();

            var first = new BalancedKdTree<Point>(points, XyComparers(), Point.NaN, p => p);

            var query = new Point(7, 7);

            var before = first.FindNearestNeighbour(query);

            // a second tree whose accessor reports coordinates 1000 units away
            var shifted = new BalancedKdTree<Point>(
                points,
                XyComparers(),
                new Point(-1, -1),
                p => new Point(p.X + 1000, p.Y + 1000));

            Assert.Equal(before, first.FindNearestNeighbour(query));
            Assert.Equal(new Point(6, 7), first.FindNearestNeighbour(query));

            Assert.Equal(points.Count, first.GetAllValues().Count);
            Assert.Equal(points.Count, shifted.GetAllValues().Count);

            // the two accessors stayed distinct
            Assert.Equal(7.0, first.PointFunc(new Point(7, 7)).X);
            Assert.Equal(1007.0, shifted.PointFunc(new Point(7, 7)).X);
        }

        [Fact]
        public void TestEmptyAndInvalidInput()
        {
            var comparers = XyComparers();

            // an empty tree answers instead of throwing, and stays usable
            var empty = new BalancedKdTree<Point>(new List<Point>(), comparers, Point.NaN, i => i);

            Assert.Empty(empty.GetAllValues());
            Assert.Empty(empty.FindNeighbours(new Point(0, 0), 10));
            Assert.Throws<InvalidOperationException>(() => empty.FindNearestNeighbour(new Point(0, 0)));

            empty.Insert(new Point(3, 4));

            Assert.Single(empty.GetAllValues());
            Assert.Equal(new Point(3, 4), empty.FindNearestNeighbour(new Point(0, 0)));

            // a null sequence is treated as no values, not as a crash
            var fromNull = new BalancedKdTree<Point>(null, comparers, Point.NaN, i => i);

            Assert.Empty(fromNull.GetAllValues());

            // comparers are not optional
            Assert.Throws<ArgumentException>(
                () => new BalancedKdTree<Point>(SamplePoints(), new List<Func<Point, Point, int>>(), Point.NaN, i => i));

            Assert.Throws<ArgumentException>(
                () => new BalancedKdTree<Point>(SamplePoints(), null, Point.NaN, i => i));

            // the plain tree used to throw NullReferenceException here, from its own null guard
            Assert.Throws<ArgumentNullException>(() => new KdTree<Point>(null, comparers));

            Assert.Throws<ArgumentException>(() => new KdTree<Point>(SamplePoints().ToArray(), null));

            var emptyPlain = new KdTree<Point>(Array.Empty<Point>(), comparers);

            Assert.Null(emptyPlain.Root);
        }

        /// <summary>
        /// A lazy sequence must be walked once. The constructor used to enumerate it four times.
        /// </summary>
        [Fact]
        public void TestConstructorEnumeratesValuesOnce()
        {
            int enumerations = 0;

            IEnumerable<Point> Source()
            {
                enumerations++;

                foreach (var point in SamplePoints())
                {
                    yield return point;
                }
            }

            var tree = new BalancedKdTree<Point>(Source(), XyComparers(), Point.NaN, i => i);

            Assert.Equal(1, enumerations);
            Assert.Equal(SamplePoints().Count, tree.GetAllValues().Count);
        }

        [Fact]
        public void TestBoundingBox()
        {
            List<Point> points = SamplePoints();

            var kdtree = new BalancedKdTree<Point>(points.ToArray(), XyComparers(), Point.NaN, i => i);

            Assert.True(CheckBoundingBox(kdtree.Root));

            // and after further insertions, which drive the rotations
            foreach (var point in DistinctRandomPoints(seed: 5, count: 150, extent: 40))
            {
                kdtree.Insert(point);
            }

            Assert.True(CheckBoundingBox(kdtree.Root));
        }

        [Fact]
        public void TestClusterCenters()
        {
            // two tight clusters, far apart
            var points = new List<Point>
            {
                new Point(0, 0), new Point(0.1, 0), new Point(0, 0.1), new Point(0.1, 0.1),
                new Point(20, 20), new Point(20.1, 20), new Point(20, 20.1), new Point(20.1, 20.1),
            };

            var centers = KdTreePointClusters<Point>.GetClusterCenters(points, Point.NaN, radius: 1);

            Assert.Equal(2, centers.Count);
            Assert.Contains(new Point(0, 0), centers);
            Assert.Contains(new Point(20, 20), centers);

            Assert.Empty(KdTreePointClusters<Point>.GetClusterCenters(new List<Point>(), Point.NaN, radius: 1));
        }

        private Point FindNearestBruteForce(List<Point> dataSet, Point targetPoint)
        {
            var minDistance = SpatialUtility.GetEuclideanLength(dataSet[0], targetPoint);

            var result = dataSet.First();

            for (int i = 0; i < dataSet.Count; i++)
            {
                var distance = SpatialUtility.GetEuclideanLength(dataSet[i], targetPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;

                    result = dataSet[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Compares each node's cached box against the extent recomputed from the subtree's
        /// points. <see cref="BalancedKdTreeNode{T}.CalculateBoundingBox"/> deliberately does
        /// not read <c>MinimumBoundingBox</c>, so a cached box that is too large fails here.
        /// </summary>
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
