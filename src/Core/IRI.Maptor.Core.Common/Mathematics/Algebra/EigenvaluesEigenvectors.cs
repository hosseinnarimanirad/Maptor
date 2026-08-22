// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Mathematics;

public struct EigenvaluesEigenvectors
{
    private double[] m_Eigenvalues;

    private Matrix m_Eigenvectors;

    //int[] sortedEigenvalueIndexes;

    //each column is an eigenvector
    public EigenvaluesEigenvectors(double[] eigenvalues, Matrix eigenvectors)
    {
        int dimension = eigenvalues.Length;

        if (dimension != eigenvectors.NumberOfColumns)
        {
            throw new NotImplementedException();
        }

        this.m_Eigenvalues = new double[dimension];

        this.m_Eigenvectors = new Matrix(eigenvectors.NumberOfRows, eigenvectors.NumberOfColumns);

        List<double> tempCopy = new List<double>(eigenvalues);

        List<double> temp = new List<double>(eigenvalues);

        temp.Sort();
        
        temp.Reverse();

        for (int i = 0; i <temp.Count; i++)
        {
            int tempIndex = tempCopy.IndexOf(temp[i]);

            this.m_Eigenvalues[i] = eigenvalues[tempIndex];

            this.m_Eigenvectors.SetColumn(i, eigenvectors.GetColumn(tempIndex));
        }
    }

    public double GetEigenvalue(int index)
    {
        return this.m_Eigenvalues[index];
    }

    public double[] GetEigenvector(int index)
    {
        return this.m_Eigenvectors.GetColumn(index);
    }

    public Matrix EigenvectorMatrix
    {
        get { return this.m_Eigenvectors; }
    }

    public double[] Eigenvalues
    {
        get { return this.m_Eigenvalues; }
    }

}
