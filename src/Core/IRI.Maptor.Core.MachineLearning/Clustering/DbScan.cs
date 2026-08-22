using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Core.MachineLearning
{
    public static class Dbscan
    {
        public static void Cluster<T>(List<T> input, int minNumberOfNeighbour, double distanceThreshold, Func<T, T, double> distanceFunc)
        {
            var values = new HashSet<DbscanItem<T>>(input.Select(i => new DbscanItem<T>() { Value = i }));

            var clusters = new List<DbscanCluster<T>>();

            int clusterNumber = 0;

            for (int i = values.Count - 1; i >= 0; i--)
            {
                var neighbours = RetrieveAllDensityReachablePoints(values, i, minNumberOfNeighbour, distanceThreshold, distanceFunc);

                var getCurrentCluster = neighbours.Where(n => n.ClusterNumber.HasValue).Select(n => n.ClusterNumber.Value).Distinct();

                int currentCluster = 0;

                DbscanCluster<T> cluster = null;

                if (getCurrentCluster == null)
                {
                    currentCluster = clusterNumber;

                    cluster = new DbscanCluster<T>() { ClusterNumber = currentCluster };

                    foreach (var item in neighbours)
                    {
                        item.ClusterNumber = currentCluster;

                        cluster.Items.Add(item);
                    }

                    clusterNumber++;
                }
                else
                {
                    if (getCurrentCluster.Count() > 1)
                    {
                        //mergeClusters
                        MergeClusters(clusters, getCurrentCluster);
                    }

                    currentCluster = getCurrentCluster.First();
                }

                values.ElementAt(i).Flag = neighbours.Count >= minNumberOfNeighbour ? DbscanFlag.CorePoint : neighbours.Count > 0 ? DbscanFlag.BorderPoint : DbscanFlag.OutlierPoint;

                values.ElementAt(i).ClusterNumber = currentCluster;

                cluster.Items.Add(values.ElementAt(i));
            }
        }

        private static DbscanCluster<T> MergeClusters<T>(List<DbscanCluster<T>> clusters, IEnumerable<int> getCurrentCluster)
        {
            var toBeMergedClusters = clusters.Where(c => getCurrentCluster.Contains(c.ClusterNumber)).ToList();

            var firstCluster = toBeMergedClusters.First();

            for (int i = 1; i < toBeMergedClusters.Count; i++)
            {
                firstCluster.AddRange(toBeMergedClusters[i].Items);

                clusters.Remove(toBeMergedClusters[i]);
            }

            return firstCluster;
        }

        private static List<DbscanItem<T>> RetrieveAllDensityReachablePoints<T>(HashSet<DbscanItem<T>> input, int minNumberOfNeighbour, int currentPointIndex, double distanceThreshold, Func<T, T, double> distanceFunc)
        {
            var result = new List<DbscanItem<T>>();

            //int numberOfNeighbours = 0;

            for (int i = 0; i < input.Count; i++)
            {
                if (i == currentPointIndex)
                {
                    continue;
                }

                if (distanceFunc(input.ElementAt(i).Value, input.ElementAt(currentPointIndex).Value) <= distanceThreshold)
                {
                    //numberOfNeighbours++;
                    result.Add(input.ElementAt(i));
                    //do not process any more
                    //if (numberOfNeighbours > minNumberOfNeighbour)
                    //{
                    //    return DbscanFlag.CorePoint;
                    //}
                }
            }

            //return numberOfNeighbours == 0 ? DbscanFlag.OutlierPoint : DbscanFlag.BorderPoint;
            return result;
        }
    }

    public class DbscanCluster<T>
    {
        public HashSet<DbscanItem<T>> Items { get; set; }

        public int ClusterNumber { get; set; }

        public void AddRange(HashSet<DbscanItem<T>> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items.ElementAt(i);

                item.ClusterNumber = this.ClusterNumber;

                this.Items.Add(item);
            }
        }
    }

    public class DbscanItem<T>
    {
        public T Value { get; set; }

        public int NumberOfNeighbours { get; set; }

        public DbscanFlag Flag { get; set; } = DbscanFlag.None;

        public int? ClusterNumber { get; set; }
    }

    public enum DbscanFlag
    {
        None = 1,
        CorePoint = 2,
        BorderPoint = 3,
        OutlierPoint = 4
    }
}
