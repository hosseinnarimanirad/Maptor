using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.MachineLearning; 

namespace IRI.Maptor.Res.LRSimplification.Common;

public static class SyntheticDataHelper
{ 
    private static void Print(double baseLength, Predicate<(double dx1, double dx2, double dy)> isRetainedFunc, string fileName, int xOffset = 4, int yOffset = 4)
    {
        double pointY = 100;
        double startX = 100;
        double endX = startX + baseLength;

        double minX = startX - xOffset;
        double minY = pointY - yOffset;

        double maxX = endX + xOffset;
        double maxY = pointY + yOffset;

        var startPoint = new Point(startX, pointY);
        var endPoint = new Point(endX, pointY);

        var scale = 100;

        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)((maxX - minX) * scale), (int)((maxY - minY) * scale));

        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.FillRectangle(System.Drawing.Brushes.White, 0, 0, bitmap.Width, bitmap.Height);

            for (double y = minY; y <= maxY; y += 0.5)
            {
                //if (y == 100)
                //    continue;

                for (double x = minX; x <= maxX; x += 0.5)
                {
                    var dx1 = x - startX;
                    var dx2 = x - endX;
                    var dy = pointY - y;

                    bool retained = isRetainedFunc((dx1, dx2, dy));

                    var brush = retained ? System.Drawing.Brushes.White : System.Drawing.Brushes.Red;
                    var pen = System.Drawing.Pens.Gray;

                    var rectangle = new System.Drawing.Rectangle((int)((x - minX) * scale), (int)((y - minY) * scale), (int)(0.5 * scale), (int)(0.5 * scale));
                    if (rectangle.Width > 100 || rectangle.Height > 100)
                    {

                    }
                    graphics.FillRectangle(brush, rectangle);
                    graphics.DrawRectangle(pen, rectangle);
                }
            }
        }

        bitmap.Save(fileName);
    }

    public static void PrintAll(string fileName)
    {
        var directory = System.IO.Path.GetDirectoryName(fileName);
        var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

        Print(0.5, SyntheticDataFactory.IsRetainedFunc0_5, $"{directory}\\{fileNameWithoutExtension}_0.5.jpg");
        Print(01.0, SyntheticDataFactory.IsRetainedFunc1_0, $"{directory}\\{fileNameWithoutExtension}_1.0.jpg");
        Print(02.0, SyntheticDataFactory.IsRetainedFunc2_0, $"{directory}\\{fileNameWithoutExtension}_2.0.jpg");
        Print(04.0, SyntheticDataFactory.IsRetainedFunc4_0, $"{directory}\\{fileNameWithoutExtension}_4.0.jpg");
        Print(08.0, SyntheticDataFactory.IsRetainedFunc8_0, $"{directory}\\{fileNameWithoutExtension}_8.0.jpg");
        Print(16.0, SyntheticDataFactory.IsRetainedFunc16_0, $"{directory}\\{fileNameWithoutExtension}_16.0.jpg");
        Print(32.0, SyntheticDataFactory.IsRetainedFunc16_0, $"{directory}\\{fileNameWithoutExtension}_32.0.jpg");
    }
}
