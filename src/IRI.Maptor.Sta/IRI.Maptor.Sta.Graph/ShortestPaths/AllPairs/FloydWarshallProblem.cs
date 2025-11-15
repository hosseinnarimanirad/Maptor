using System;

namespace IRI.Maptor.Sta.Graph;

public class FloydWarshallProblem
{
    public double[,] shortestPaths;
     
    double[,] predecessors;

    public FloydWarshallProblem(double[,] adjacency)
    {
        if (adjacency == null)
            throw new ArgumentNullException(nameof(adjacency));

        Initialize(adjacency);

        int n = adjacency.GetLength(0);

        for (int k = 0; k < n; k++)
        {
            double[,] temp = this.shortestPaths ?? throw new InvalidOperationException("Shortest paths matrix was not initialized.");

            for (int row = 0; row < n; row++)
            {
                for (int column = 0; column < n; column++)
                {
                    this.shortestPaths[row, column] =
                        Math.Min(temp[row, column], temp[row, k] + temp[k, column]);

                    if (temp[row, column] > temp[row, k] + temp[k, column])
                    {
                        this.predecessors[row, column] = this.predecessors[k, column];
                    }

                    if (row == column)
                    {
                        if (this.shortestPaths[row, column] < 0)
                        {
                            throw new InvalidOperationException($"Negative cycle detected at node {row}. The graph contains a negative-weight cycle.");
                        }
                    }
                }
            }
        }

    }

    private void Initialize(double[,] adjacency)
    {
        int n = adjacency.GetLength(0);

        this.shortestPaths = new double[n, n];

        this.predecessors = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                this.shortestPaths[i, j] = adjacency[i, j];

                if (double.IsInfinity(adjacency[i, j]) || i == j)
                {
                    this.predecessors[i, j] = double.NaN;
                }
                else
                {
                    this.predecessors[i, j] = i;
                }
            }
        }
    }
}
