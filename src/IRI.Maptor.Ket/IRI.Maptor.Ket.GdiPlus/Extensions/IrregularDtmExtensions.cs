
using IRI.Maptor.Sta.Common;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Extensions;

public static class IrregularDtmExtensions
{
    public static System.Drawing.Bitmap DrawSlopeMap(this IrregularDtm dtm, int scale)
    {
        if (dtm.triangulation.Triangles.Count < 1)
        {
            throw new InvalidOperationException("The triangulation is empty.");
        }

        double minX = dtm.LowerLeft.X; double minY = dtm.LowerLeft.Y;

        int width = (int)(dtm.UpperRight.X - minX);

        int height = (int)(dtm.UpperRight.Y - minY);

        System.Drawing.Bitmap result = new System.Drawing.Bitmap(width / scale, height / scale);

        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(result);

        foreach (var item in dtm.triangulation.Triangles)
        {
            Point first = dtm.triangulation.Points[item.A];

            Point second = dtm.triangulation.Points[item.B];

            Point third = dtm.triangulation.Points[item.C];

            Triangle temp = new Triangle(first, second, third);

            double tempSlope = dtm.CalculateSlope(temp);

            //int color = (int)((tempSlope + Math.PI / 2) * 255 / (Math.PI));

            int color = (int)((Math.Abs(tempSlope) * 250 / (Math.PI / 2)));

            System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(color, color, color));

            g.FillPolygon(brush,
                new System.Drawing.PointF[] {
                    new System.Drawing.PointF((float)(first.X - minX), (float)(result.Height - (first.Y - minY))),
                    new System.Drawing.PointF((float)(second.X - minX), (float)(result.Height - (second.Y - minY))),
                    new System.Drawing.PointF((float)(third.X - minX), (float)(result.Height - (third.Y - minY)))});
        }

        return result;
    }

    public static System.Drawing.Bitmap DrawAspectMap(this IrregularDtm dtm, int scale)
    {
        if (dtm.triangulation.Triangles.Count < 1)
        {
            throw new InvalidOperationException("The triangulation is empty.");
        }

        double minX = dtm.LowerLeft.X; double minY = dtm.LowerLeft.Y;

        int width = (int)(dtm.UpperRight.X - minX);

        int height = (int)(dtm.UpperRight.Y - minY);

        System.Drawing.Bitmap result = new System.Drawing.Bitmap(width / scale, height / scale);

        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(result);

        foreach (var item in dtm.triangulation.Triangles)
        {
            Point first = dtm.triangulation.Points[item.A];

            Point second = dtm.triangulation.Points[item.B];

            Point third = dtm.triangulation.Points[item.C];

            Triangle temp = new Triangle(first, second, third);

            double tempSlope = dtm.CalculateAspect(temp);

            //int color = (int)((tempSlope + Math.PI / 2) * 255 / (Math.PI));

            int color = (int)(tempSlope * 255 / (2 * Math.PI));

            System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(color, color, color));

            g.FillPolygon(brush,
                new System.Drawing.PointF[] {
                    new System.Drawing.PointF((float)(first.X - minX), (float)(result.Height - (first.Y - minY))),
                    new System.Drawing.PointF((float)(second.X - minX), (float)(result.Height - (second.Y - minY))),
                    new System.Drawing.PointF((float)(third.X - minX), (float)(result.Height - (third.Y - minY)))});
        }

        return result;
    }
}
