using System;

namespace IRI.Maptor.Sta.Graph;

public class FloydWarshallProblem
{
    public double[,] shortestPaths;
     
    double[,] predecessors;

    public FloydWarshallProblem(double[,] adjacency)
    {
        //if (!adjacency.IsSquare())
        //    throw new NotImplementedException();

        Initialize(adjacency);

        int n = adjacency.GetLength(0);

        for (int k = 0; k < n; k++)
        {
            //this.shortestPaths.Add(new double[n, n]);
            double[,] temp = this.shortestPaths;

            //this.predecessors.Add(new double[n, n]);

            for (int row = 0; row < n; row++)
            {
                for (int column = 0; column < n; column++)
                {
                    //this.shortestPaths[k + 1][row, column] =
                    //    Math.Min(this.shortestPaths[k][row, column], this.shortestPaths[k][row, k] + this.shortestPaths[k][k, column]);
                    this.shortestPaths[row, column] =
                        Math.Min(temp[row, column], temp[row, k] + temp[k, column]);

                    if (temp[row, column] > temp[row, k] + temp[k, column])
                    {
                        //this.predecessors[k + 1][row, column] = this.predecessors[k][k, column];
                        this.predecessors[row, column] = this.predecessors[k, column];
                    }
                    //else
                    //{
                    //    this.predecessors[k + 1][row, column] = this.predecessors[k][row, column];
                    //}

                    if (row == column)
                    {
                        if (this.shortestPaths[row, column] < 0)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
        }

    }

    private void Initialize(double[,] adjacency)
    {
        int n = adjacency.GetLength(0);

        //this.shortestPaths = new List<double[,]>();

        //this.predecessors = new List<double[,]>();

        //this.shortestPaths.Add(new double[n, n]);
        this.shortestPaths = new double[n, n];

        //this.predecessors.Add(new double[n, n]);
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
