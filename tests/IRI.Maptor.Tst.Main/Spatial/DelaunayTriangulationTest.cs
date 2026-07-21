using System;
using System.Collections.Generic;
using System.Linq;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;

namespace IRI.Maptor.Tst.Spatial
{
    public class DelaunayTriangulationTest
    {
        #region Basic shapes

        [Fact]
        public void Square_ProducesTwoTriangles()
        {
            var triangulation = DelaunayTriangulation.Create(new List<Point>
            {
                new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10),
            });

            Assert.Equal(2, triangulation.Triangles.Count);

            AssertStructureIsValid(triangulation);

            Assert.Equal(100, TotalArea(triangulation), 9);
        }

        [Fact]
        public void UnitGrid_ProducesExpectedTriangleCount()
        {
            int columns = 5, rows = 4;

            var points = new List<Point>();

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    points.Add(new Point(i, j));
                }
            }

            var triangulation = DelaunayTriangulation.Create(points);

            Assert.Equal(2 * (columns - 1) * (rows - 1), triangulation.Triangles.Count);

            AssertStructureIsValid(triangulation);

            AssertDelaunayProperty(triangulation);

            Assert.Equal((columns - 1) * (rows - 1), TotalArea(triangulation), 9);
        }

        [Fact]
        public void RandomPoints_SatisfyDelaunayProperty()
        {
            var random = new Random(42);

            var points = new List<Point>();

            for (int i = 0; i < 200; i++)
            {
                points.Add(new Point(random.NextDouble() * 100, random.NextDouble() * 100));
            }

            var triangulation = DelaunayTriangulation.Create(points);

            Assert.True(triangulation.Triangles.Count > 0);

            AssertStructureIsValid(triangulation);

            AssertDelaunayProperty(triangulation);

            // the triangulation must tile the convex hull of the input
            Assert.Equal(ConvexHullArea(points), TotalArea(triangulation), 6);
        }

        #endregion

        #region Edge cases

        [Fact]
        public void PointExactlyOnEdge_IsHandled()
        {
            var triangulation = DelaunayTriangulation.Create(new List<Point>
            {
                new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10),
                new Point(5, 0), // exactly on the bottom edge
            });

            AssertStructureIsValid(triangulation);

            AssertDelaunayProperty(triangulation);

            // the on-edge vertex must be part of the triangulation
            Assert.Contains(triangulation.Triangles, t => t.HasVertex(4));

            Assert.Equal(100, TotalArea(triangulation), 9);
        }

        [Fact]
        public void DuplicatePoints_AreSkipped()
        {
            var triangulation = DelaunayTriangulation.Create(new List<Point>
            {
                new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10),
                new Point(0, 0), new Point(10, 10),
            });

            Assert.Equal(6, triangulation.Points.Count); // originals stay, indices preserved

            Assert.Equal(2, triangulation.Triangles.Count);

            // duplicates (indices 4, 5) are never referenced
            Assert.DoesNotContain(triangulation.Triangles, t => t.HasVertex(4) || t.HasVertex(5));

            AssertStructureIsValid(triangulation);
        }

        [Fact]
        public void CollinearPoints_YieldEmptyTriangulation()
        {
            var triangulation = DelaunayTriangulation.Create(new List<Point>
            {
                new Point(0, 0), new Point(1, 1), new Point(2, 2), new Point(3, 3), new Point(4, 4),
            });

            Assert.Empty(triangulation.Triangles);
        }

        [Fact]
        public void LessThanThreePoints_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                DelaunayTriangulation.Create(new List<Point> { new Point(0, 0), new Point(1, 1) }));

            Assert.Throws<ArgumentNullException>(() => DelaunayTriangulation.Create(null));
        }

        #endregion

        #region Point location

        [Fact]
        public void FindContainingTriangle_InsideAndOutside()
        {
            var random = new Random(7);

            var points = new List<Point>();

            for (int i = 0; i < 50; i++)
            {
                points.Add(new Point(random.NextDouble() * 10, random.NextDouble() * 10));
            }

            // corners so the hull is the full square
            points.Add(new Point(0, 0)); points.Add(new Point(10, 0));
            points.Add(new Point(10, 10)); points.Add(new Point(0, 10));

            var triangulation = DelaunayTriangulation.Create(points);

            var query = new Point(4.321, 6.789);

            int index = triangulation.FindContainingTriangle(query);

            Assert.InRange(index, 0, triangulation.Triangles.Count - 1);

            var t = triangulation.Triangles[index];

            Assert.True(Contains(triangulation.Points[t.A], triangulation.Points[t.B], triangulation.Points[t.C], query));

            // the walk must work from any start triangle
            Assert.Equal(index, triangulation.FindContainingTriangle(query, triangulation.Triangles.Count - 1));

            Assert.Equal(-1, triangulation.FindContainingTriangle(new Point(50, 50)));

            Assert.Equal(-1, triangulation.FindContainingTriangle(new Point(-1, -1)));
        }

        #endregion

        #region IrregularDtm

        [Fact]
        public void IrregularDtm_InterpolatesPlaneExactly()
        {
            // sample the plane z = 2x + 3y + 1
            static double Plane(double x, double y) => 2 * x + 3 * y + 1;

            var random = new Random(123);

            var east = new List<double> { 0, 10, 10, 0 };
            var north = new List<double> { 0, 0, 10, 10 };

            for (int i = 0; i < 50; i++)
            {
                east.Add(random.NextDouble() * 10);
                north.Add(random.NextDouble() * 10);
            }

            var value = east.Zip(north, Plane).ToArray();

            var dtm = new IrregularDtm(east.ToArray(), north.ToArray(), value);

            foreach (var (x, y) in new[] { (1.234, 2.345), (5.0, 5.0), (9.87, 0.12), (0.5, 9.5) })
            {
                Assert.Equal(Plane(x, y), dtm.Interpolate(new Point(x, y)), 9);
            }

            Assert.True(double.IsNaN(dtm.Interpolate(new Point(11, 5))));

            Assert.True(double.IsNaN(dtm.Interpolate(new Point(-0.5, -0.5))));
        }

        #endregion

        #region Voronoi

        [Fact]
        public void Voronoi_HasOneVertexPerTriangle_AndEquidistantEdges()
        {
            var random = new Random(99);

            var points = new List<Point>();

            for (int i = 0; i < 30; i++)
            {
                points.Add(new Point(random.NextDouble() * 10, random.NextDouble() * 10));
            }

            var voronoi = VoronoiDiagram.Create(points);

            Assert.Equal(voronoi.Triangulation.Triangles.Count, voronoi.Vertices.Count);

            Assert.True(voronoi.Edges.Count > 0);

            foreach (var edge in voronoi.Edges)
            {
                // a Voronoi edge lies on the perpendicular bisector of its two sites:
                // every edge vertex is equidistant from both sites
                Point siteA = voronoi.Sites[edge.SiteA], siteB = voronoi.Sites[edge.SiteB];

                Point vertexA = voronoi.Vertices[edge.VertexA];

                Assert.Equal(vertexA.DistanceTo(siteA), vertexA.DistanceTo(siteB), 6);

                if (edge.IsRay)
                {
                    // the ray direction must be a unit vector perpendicular to the site pair
                    Assert.Equal(1, Math.Sqrt(edge.DirectionX * edge.DirectionX + edge.DirectionY * edge.DirectionY), 9);

                    Assert.Equal(0, (siteB.X - siteA.X) * edge.DirectionX + (siteB.Y - siteA.Y) * edge.DirectionY, 9);
                }
                else
                {
                    Point vertexB = voronoi.Vertices[edge.VertexB];

                    Assert.Equal(vertexB.DistanceTo(siteA), vertexB.DistanceTo(siteB), 6);
                }
            }
        }

        [Fact]
        public void Voronoi_CellsContainTheirSites()
        {
            var random = new Random(21);

            var points = new List<Point>();

            for (int i = 0; i < 40; i++)
            {
                points.Add(new Point(random.NextDouble() * 10, random.NextDouble() * 10));
            }

            var voronoi = VoronoiDiagram.Create(points);

            Assert.Equal(points.Count, voronoi.Cells.Count);

            int closedCells = 0;

            for (int site = 0; site < voronoi.Cells.Count; site++)
            {
                var cell = voronoi.Cells[site];

                Assert.Equal(site, cell.SiteIndex);

                Assert.NotEmpty(cell.VertexIndices);

                if (!cell.IsClosed)
                    continue; // hull sites have unbounded cells

                closedCells++;

                // every point of a Voronoi cell is closer to its own site than to any other:
                // for closed (convex, CCW) cells, the site must lie inside the polygon
                var polygon = cell.VertexIndices.Select(v => voronoi.Vertices[v]).ToList();

                for (int i = 0; i < polygon.Count; i++)
                {
                    Assert.True(Orient(polygon[i], polygon[(i + 1) % polygon.Count], points[site]) > 0,
                        $"site {site} lies outside its own Voronoi cell");
                }
            }

            Assert.True(closedCells > 0);
        }

        #endregion

        #region Helpers

        private static double Orient(Point a, Point b, Point p)
            => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

        private static bool Contains(Point a, Point b, Point c, Point p)
        {
            double tol = 1e-9;

            return Orient(a, b, p) >= -tol && Orient(b, c, p) >= -tol && Orient(c, a, p) >= -tol;
        }

        private static double TotalArea(DelaunayTriangulation triangulation)
        {
            double area = 0;

            foreach (var t in triangulation.Triangles)
            {
                area += Orient(triangulation.Points[t.A], triangulation.Points[t.B], triangulation.Points[t.C]) / 2;
            }

            return area;
        }

        private static double ConvexHullArea(List<Point> points)
        {
            // Andrew's monotone chain
            var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

            List<Point> BuildChain(IEnumerable<Point> input)
            {
                var chain = new List<Point>();

                foreach (var p in input)
                {
                    while (chain.Count >= 2 && Orient(chain[chain.Count - 2], chain[chain.Count - 1], p) <= 0)
                        chain.RemoveAt(chain.Count - 1);

                    chain.Add(p);
                }

                return chain;
            }

            var lower = BuildChain(sorted);
            var upper = BuildChain(Enumerable.Reverse(sorted));

            var hull = lower.Take(lower.Count - 1).Concat(upper.Take(upper.Count - 1)).ToList();

            double area = 0;

            for (int i = 0; i < hull.Count; i++)
            {
                var p = hull[i];
                var q = hull[(i + 1) % hull.Count];

                area += p.X * q.Y - q.X * p.Y;
            }

            return area / 2;
        }

        /// <summary>All triangles CCW; neighbour links symmetric and sharing exactly two vertices.</summary>
        private static void AssertStructureIsValid(DelaunayTriangulation triangulation)
        {
            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                var t = triangulation.Triangles[i];

                Assert.True(Orient(triangulation.Points[t.A], triangulation.Points[t.B], triangulation.Points[t.C]) > 0,
                    $"triangle {i} is not counter-clockwise");

                foreach (int neighbour in new[] { t.NeighbourAB, t.NeighbourBC, t.NeighbourCA })
                {
                    if (neighbour == -1)
                        continue;

                    Assert.InRange(neighbour, 0, triangulation.Triangles.Count - 1);

                    var nb = triangulation.Triangles[neighbour];

                    Assert.Contains(i, new[] { nb.NeighbourAB, nb.NeighbourBC, nb.NeighbourCA });

                    int shared = new[] { t.A, t.B, t.C }.Intersect(new[] { nb.A, nb.B, nb.C }).Count();

                    Assert.Equal(2, shared);
                }
            }
        }

        /// <summary>No vertex may lie strictly inside any triangle's circumcircle.</summary>
        private static void AssertDelaunayProperty(DelaunayTriangulation triangulation)
        {
            var usedIndices = triangulation.Triangles
                .SelectMany(t => new[] { t.A, t.B, t.C })
                .Distinct()
                .ToList();

            foreach (var t in triangulation.Triangles)
            {
                Point a = triangulation.Points[t.A], b = triangulation.Points[t.B], c = triangulation.Points[t.C];

                foreach (int index in usedIndices)
                {
                    if (t.HasVertex(index))
                        continue;

                    Point p = triangulation.Points[index];

                    double adx = a.X - p.X, ady = a.Y - p.Y;
                    double bdx = b.X - p.X, bdy = b.Y - p.Y;
                    double cdx = c.X - p.X, cdy = c.Y - p.Y;

                    double t1 = (adx * adx + ady * ady) * (bdx * cdy - cdx * bdy);
                    double t2 = (bdx * bdx + bdy * bdy) * (adx * cdy - cdx * ady);
                    double t3 = (cdx * cdx + cdy * cdy) * (adx * bdy - bdx * ady);

                    double det = t1 - t2 + t3;

                    double scale = Math.Abs(t1) + Math.Abs(t2) + Math.Abs(t3);

                    Assert.True(det <= 1e-9 * Math.Max(scale, 1),
                        $"point {index} lies strictly inside the circumcircle of triangle ({t.A} {t.B} {t.C})");
                }
            }
        }

        #endregion
    }
}
