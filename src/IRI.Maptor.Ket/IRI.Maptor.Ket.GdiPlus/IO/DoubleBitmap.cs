// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System.Drawing;
using System.Collections;


namespace IRI.Maptor.Ket.GdiPlus;

public static class DoubleBitmap
{
    public static Bitmap DoubleToBitmapLinear(IRI.Maptor.Sta.Mathematics.Matrix data)
    {

        Bitmap result = new Bitmap(data.NumberOfColumns, data.NumberOfRows);

        double Max = IRI.Maptor.Sta.Mathematics.Statistics.GetMax(data);

        double Min = IRI.Maptor.Sta.Mathematics.Statistics.GetMin(data);

        for (int i = 0; i < data.NumberOfRows; i++)
        {
            for (int j = 0; j < data.NumberOfColumns; j++)
            {
                if (data[i, j].Equals(double.NaN))
                {
                    result.SetPixel(j, i, Color.Red);

                    continue;
                }
                int tempValue = (int)((data[i, j] - Min) * 255 / (Math.Abs(Max - Min)));

                result.SetPixel(j, i, Color.FromArgb(tempValue, tempValue, tempValue));
            }
        }

        return result;
    }

    public static Bitmap DoubleToBitmapSimple(IRI.Maptor.Sta.Mathematics.Matrix data)
    {
        Bitmap result = new Bitmap(data.NumberOfColumns, data.NumberOfRows);

        for (int i = 0; i < data.NumberOfRows; i++)
        {
            for (int j = 0; j < data.NumberOfColumns; j++)
            {
                if (data[i, j] > 256)
                    result.SetPixel(j, i, Color.FromArgb(255, 255, 255));

                else if (data[i, j] < 0)
                    result.SetPixel(j, i, Color.FromArgb(0, 0, 0));

                else
                    result.SetPixel(j, i, Color.FromArgb((int)data[i, j], (int)data[i, j], (int)data[i, j]));

            }
        }

        return result;
    }

    public static void DrawBreaksAbsolutely(ref Bitmap image, ArrayList x, ArrayList y)
    {

        Graphics graphic = Graphics.FromImage(image);

        for (int k = 0; k < x.Count; k++)
        {
            graphic.DrawEllipse(Pens.Red, new Rectangle(new Point((int)x[k] - 10, (int)y[k] - 10), new Size(20, 20)));
        }
    }

    public static void DrawBreaksRelatively(ref Bitmap image, double[,] data, ArrayList x, ArrayList y)
    {
        Graphics graphic = Graphics.FromImage(image);


        for (int k = 0; k < x.Count; k++)
        {
            int i = (int)y[k];

            int j = (int)x[k];

            int temp = (int)Math.Abs(data[i, j]);

            try
            {
                graphic.DrawEllipse(Pens.Red, new Rectangle(new Point(j - temp / 2, i - temp / 2), new Size(temp, temp)));
            }
            catch (Exception)
            {
                graphic.DrawEllipse(Pens.Red, new Rectangle(new Point(j, i), new Size(1, 1)));
            }
        }

    }
}
