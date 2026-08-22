// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Core.Common.Mathematics; 

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public static class PrincipalComponentTransformation
{

    public static Matrix GetTransformationMatrix(Matrix[] bands)
    {
        Matrix varCovarMatrix = Statistics.CalculateVarianceCovariance(bands);

        Matrix transformMatrix = varCovarMatrix.CalculateEigenvector().Transpose();

        return transformMatrix;
    }

    public static Matrix[] Transform(Matrix[] bands)
    {
        Matrix transformationMatrix = GetTransformationMatrix(bands);

        Matrix[] result = new Matrix[bands.Length];

        for (int k = 0; k < bands.Length; k++)
        {
            Vector eigenValue = new Vector(transformationMatrix.GetColumn(k));

            for (int row = 0; row < bands[0].NumberOfRows; row++)
            {
                for (int column = 0; column < bands[0].NumberOfColumns; column++)
                {
                    Vector firstSpace = new Vector(bands.Length);

                    for (int i = 0; i < bands.Length; i++)
                    {
                        firstSpace[i] = bands[i][row, column];
                    }

                    result[k][row, column] = firstSpace.DotMultiply(eigenValue);
                }
            }
        }

        return result;
    }

}
