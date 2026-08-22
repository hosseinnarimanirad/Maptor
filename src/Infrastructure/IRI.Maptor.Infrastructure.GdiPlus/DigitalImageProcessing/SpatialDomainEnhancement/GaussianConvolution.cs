// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj
 
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public class GaussianConvolution : IConvolution
{
    private double m_Sigma;

    public double Sigma
    {
        get { return m_Sigma; }
    }

    public GaussianConvolution()
        : this(1) { }

    public GaussianConvolution(double sigma)
    {
        this.m_Sigma = sigma;
    }

    public Matrix Convolve(Matrix original)
    {
        double width = original.NumberOfColumns;

        double height = original.NumberOfRows;

        //int tempWidth = (int)Math.Ceiling(width / 2);

        //int tempHeight = (int)Math.Ceiling(height / 2);

        int tempWidth = 1 + 2 * ((int)(3.0 * Sigma));

        int tempHeight = tempWidth;

        Matrix result = new Matrix((int)height, (int)width);

        double tempCoef = 1 / (2 * Math.PI * Sigma * Sigma);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                double temp = 0;

                int startX = (x - tempWidth < 0 ? 0 : x - tempWidth + 1);

                int endX = (x + tempWidth > width ? (int)width - 1 : x + tempWidth - 1);

                int startY = (y - tempHeight < 0 ? 0 : y - tempHeight + 1);

                int endY = (y + tempHeight > height ? (int)height - 1 : y + tempHeight - 1);

                for (int m = startX; m <= endX; m++)
                {
                    for (int n = startY; n <= endY; n++)
                    {
                        double tempX = m - x;

                        double tempY = n - y;

                        temp += original[n, m] * (tempCoef * Math.Exp(-(tempX * tempX + tempY * tempY) / (2 * Sigma * Sigma)));
                    }
                }

                result[y, x] = temp;
            }
        }

        return result;
    }

}
