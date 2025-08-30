 
//using Microsoft.SqlServer.Types;
//using System;
//using System.Collections.Generic;
//using drawing = System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using OpenTK;
//using OpenTK.Graphics.OpenGL;
//using System.Diagnostics;
//using System.Windows;
//using IRI.Maptor.Extensions;
//using IRI.Maptor.Ket.SpatialExtensions;

//namespace IRI.Maptor.Jab.Common.Convertor
//{
//    public class SqlSpatialToOpenTKBitmap
//    {
//        OpenTK.GLControl gl;

//        const double pointSize = 4;

//        //static int numPoints = 0;

//        public drawing.Bitmap ParseSqlGeometry(List<SqlGeometry> geometries, double width, double height, Func<Point, Point> transform, drawing.Pen pen, drawing.Brush brush)//, ImageSource pointSymbol = null, Geometry symbol = null)
//        {
//            //if (width + height > 5000)
//            //{
//            //    throw new NotImplementedException();
//            //}

//            gl = new GLControl(new OpenTK.Graphics.GraphicsMode(32, 24, 8, 4));
//            gl.MakeCurrent();

//            //Debug.Print("ParseSqlGeometry started");
//            //numPoints = 0;

//            gl.Width = (int)width; gl.Height = (int)height;

//            GL.Enable(EnableCap.PointSmooth);
//            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
//            //GL.Enable(EnableCap.LineSmooth);
//            //GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);

//            //GL.Disable(EnableCap.Texture2D);

//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

//            GL.MatrixMode(MatrixMode.Projection);
//            GL.LoadIdentity();
//            GL.Ortho(0, width, 0, height, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
//            GL.Viewport(0, 0, (int)width, (int)height); // Use all of the glControl painting area

//            GL.ClearColor(drawing.Color.Transparent);

//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

//            GL.MatrixMode(MatrixMode.Modelview);
//            GL.LoadIdentity();
//            GL.Color3(pen.Color);
//            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
//            GL.PointSize(5f);
//            GL.LineWidth(2f);
//            //GL.Color4(System.Drawing.Color.LightBlue);
//            //GL.Enable(EnableCap.VertexArray);

//            //OpenTKHelper.pen = pen;

//            //OpenTKHelper.brush = brush;

//            int p = 0;
//            if (geometries != null)
//            {
//                foreach (SqlGeometry item in geometries)
//                {
//                    p += AddGeometry(item, transform);
//                }
//            }
//            //***************
//            //var image2 = GrabScreenshot((int)width, (int)height);

//            //image2.Save(DateTime.Now.ToString("yyyy,MM,dd,hh,mm,ss") + ".png", drawing.Imaging.ImageFormat.Png);

//            //image2.Dispose();
//            //**********
//            GL.Finish();

//            var result = GrabScreenshot();

//            gl.Dispose();

//            return result;

//        }

//        public drawing.Bitmap GrabScreenshot()
//        {

//            if (OpenTK.Graphics.GraphicsContext.CurrentContext == null)
//                throw new OpenTK.Graphics.GraphicsContextMissingException();

//            //gl.SwapBuffers();
//            //if (width + height > 5500)
//            //{
//            //    return null;
//            //}

//            var width = gl.Width; var height = gl.Height;

//            drawing.Bitmap result = new drawing.Bitmap(width, height);

//            drawing.Imaging.BitmapData data = result.LockBits(new drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

//            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

//            result.UnlockBits(data);

//            //result.RotateFlip(drawing.RotateFlipType.RotateNoneFlipY);

//            //Debug.Print($"ParseSqlGeometry end; NumPoints: {numPoints}");

//            return result;
//        }

//        private int AddGeometry(SqlGeometry geometry, Func<Point, Point> transform)//, ImageSource pointSymbol, Geometry symbol)
//        {
//            if (geometry.STIsValid().Value != true)
//            {
//                return 1;
//            }
//            //CheckGeometry(geometry);
//            var type = geometry.GetOpenGisType();

//            switch (type)
//            {
//                case OpenGisGeometryType.Point:
//                    AddPoint(geometry, transform);//, pointSymbol, symbol);
//                    break;

//                case OpenGisGeometryType.LineString:
//                    AddLineString(geometry, transform);
//                    break;

//                case OpenGisGeometryType.Polygon:
//                    AddPolygon(geometry, transform);
//                    break;

//                case OpenGisGeometryType.MultiPoint:
//                    AddMultiPoint(geometry, transform);//, pointSymbol, symbol);
//                    break;

//                case OpenGisGeometryType.MultiLineString:
//                    AddMultiLineString(geometry, transform);
//                    break;

//                case OpenGisGeometryType.MultiPolygon:
//                    AddMultiPolygon(geometry, transform);
//                    break;

//                case OpenGisGeometryType.GeometryCollection:
//                case OpenGisGeometryType.CircularString:
//                case OpenGisGeometryType.CompoundCurve:
//                case OpenGisGeometryType.CurvePolygon:
//                default:
//                    throw new NotImplementedException();
//            }

//            return 0;
//        }

//        private void AddPoint(SqlGeometry point, Func<Point, Point> transform)//, ImageSource pointSymbol, Geometry symbol)
//        {
//            //numPoints++;

//            GL.Begin(PrimitiveType.Points);

//            var parsedPoint = transform(new Point(point.STX.Value, point.STY.Value));

//            GL.Vertex2(parsedPoint.X, parsedPoint.Y);


//            GL.End();

//            //if (symbol != null)
//            //{
//            //    var temp = transform(new Point(point.STX.Value, point.STY.Value));

//            //    var geometry = SqlSpatialToStreamGeometry.Transform(
//            //                        symbol,
//            //                        new Point(temp.X - pointSymbolMinX - pointSymbolWidth / 2.0, temp.Y - pointSymbolMinY + pointSymbolHeight / 2.0));

//            //    //var geometry = SqlSpatialToStreamGeometry.Transform(symbol, transform(new Point(point.STX.Value, point.STY.Value)));

//            //    context.DrawGeometry(brush, pen, geometry);
//            //}
//            //else if (pointSymbol != null)
//            //{
//            //    Point location = transform(point.ParseToWindowsPoint());

//            //    context.DrawImage(pointSymbol, new Rect(location.X, location.Y, 16, 16));
//            //}
//            //else
//            //{
//            //    context.DrawEllipse(brush, pen, transform(point.ParseToWindowsPoint()), pointSize, pointSize);
//            //}
//        }


//        private void AddLineString(SqlGeometry lineString, Func<Point, Point> transform)
//        {
//            GL.Begin(PrimitiveType.LineStrip);

//            int numberOfPoints = lineString.STNumPoints().Value;
//            //numPoints += numberOfPoints;
//            //STPointN(index): index is between 1 and number of points
//            for (int i = 0; i < numberOfPoints; i++)
//            {
//                var start = transform(lineString.STPointN(i + 1).AsWpfPoint());

//                //var end = SqlSpatialExtensions.ParseToPoint(lineString.STPointN(i + 1));

//                //context.DrawLine(pen, transform(start), transform(end));
//                GL.Vertex2(start.X, start.Y);
//            }

//            GL.End();
//        }

//        //private static void AddLineString(SqlGeometry lineString, Func<Point, Point> transform)
//        //{


//        //    int numberOfPoints = lineString.STNumPoints().Value;

//        //    Vector2[] vertices = new Vector2[numberOfPoints];

//        //    //STPointN(index): index is between 1 and number of points
//        //    for (int i = 0; i < numberOfPoints; i++)
//        //    {
//        //        var temp = transform(SqlSpatialExtensions.ParseToPoint(lineString.STPointN(i + 1)));

//        //        vertices[i] = new Vector2((float)temp.X, (float)temp.Y);
//        //    }

//        //    int vbo;

//        //    GL.GenBuffers(1, out vbo);
//        //    //GL.DrawArrays(BeginMode.Polygon, 0, numberOfPoints);



//        //    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
//        //    //GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector2.SizeInBytes), vertices, BufferUsageHint.StaticDraw);
//        //    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector2.SizeInBytes), vertices, BufferUsageHint.StaticDraw);

//        //    //GL.EnableClientState(ArrayCap.VertexArray);

//        //    //GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
//        //    //GL.VertexAttribPointer(0, numberOfPoints, VertexAttribPointerType.Float, false, 0, new IntPtr(0));
//        //    GL.DrawArrays(PrimitiveType.LineStrip, 0, numberOfPoints);

//        //    //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
//        //    GL.Finish();

//        //}

//        //private static void AddLineString(SqlGeometry lineString, Func<Point, Point> transform)
//        //{
//        //    int numberOfPoints = lineString.STNumPoints().Value;

//        //    float[] vertices = new float[numberOfPoints * 3];

//        //    //STPointN(index): index is between 1 and number of points
//        //    for (int i = 0; i < numberOfPoints; i++)
//        //    {
//        //        var temp = transform(SqlSpatialExtensions.ParseToPoint(lineString.STPointN(i + 1)));
//        //        vertices[3 * i] = (float)temp.X;
//        //        vertices[3 * i + 1] = (float)temp.Y;
//        //        vertices[3 * i + 2] = 0;

//        //        //vertices[i] = new Vector2((float)temp.X, (float)temp.Y);
//        //    }

//        //    int vbo;

//        //    GL.GenBuffers(1, out vbo);
//        //    //GL.DrawArrays(BeginMode.Polygon, 0, numberOfPoints);

//        //    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
//        //    //GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector2.SizeInBytes), vertices, BufferUsageHint.StaticDraw);
//        //    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * 3 * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

//        //    //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
//        //    GL.Finish();

//        //}

//        private void AddPolygonRing(SqlGeometry ring, Func<Point, Point> transform)
//        {
//            GL.Begin(PrimitiveType.LineStrip);

//            int numberOfPoints = ring.STNumPoints().Value;

//            //numPoints += numberOfPoints;

//            //STPointN(index): index is between 1 and number of points
//            for (int i = 0; i < numberOfPoints; i++)
//            {
//                var start = transform(ring.STPointN(i + 1).AsWpfPoint());

//                GL.Vertex2(start.X, start.Y);
//            }

//            GL.End();
//        }

//        //private static void AddPolygonRing(SqlGeometry ring, Func<Point, Point> transform)
//        //{

//        //    int numberOfPoints = ring.STNumPoints().Value;

//        //    Vector2[] vertices = new Vector2[numberOfPoints];

//        //    //STPointN(index): index is between 1 and number of points
//        //    for (int i = 0; i < numberOfPoints; i++)
//        //    {
//        //        var temp = transform(SqlSpatialExtensions.ParseToPoint(ring.STPointN(i + 1)));
//        //        vertices[i] = new Vector2((float)temp.X, (float)temp.Y);
//        //    }

//        //    int vbo;

//        //    GL.GenBuffers(1, out vbo);
//        //    //GL.DrawArrays(BeginMode.Polygon, 0, numberOfPoints);

//        //    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
//        //    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector2.SizeInBytes), vertices, BufferUsageHint.StaticDraw);

//        //    //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
//        //    GL.Finish();

//        //}

//        private void AddMultiLineString(SqlGeometry multiLineString, Func<Point, Point> transform)
//        {
//            int numberOfLineStrings = multiLineString.STNumGeometries().Value;

//            for (int i = 1; i <= numberOfLineStrings; i++)
//            {
//                SqlGeometry lineString = multiLineString.STGeometryN(i);

//                AddLineString(lineString, transform);
//            }
//        }

//        private void AddPolygon(SqlGeometry polygon, Func<Point, Point> transform)
//        {
//            //GL.Begin(BeginMode.Polygon);

//            var exteriorRing = polygon.STExteriorRing();

//            AddPolygonRing(exteriorRing, transform);

//            int numberOfInteriorRings = polygon.STNumInteriorRing().Value;

//            for (int i = 0; i < numberOfInteriorRings; i++)
//            {
//                var ring = polygon.STInteriorRingN(i + 1);

//                AddPolygonRing(ring, transform);
//            }

//            //GL.End();
//        }

//        //private static void AddPolygon(DrawingContext context, SqlGeometry polygon, Func<Point, Point> transform)
//        //{
//        //    //There is no DrawPolygon method for DrawingContext so we should get the Geometry and use the DrawGeometry method
//        //    var geometry = SqlSpatialToStreamGeometry.ParseSqlGeometry(new List<SqlGeometry>() { polygon }, transform);

//        //    context.DrawGeometry(brush, pen, geometry);
//        //}

//        private void AddMultiPolygon(SqlGeometry multiPolygon, Func<Point, Point> transform)
//        {
//            int numberOfPolygons = multiPolygon.STNumGeometries().Value;

//            for (int i = 0; i < numberOfPolygons; i++)
//            {
//                var polygon = multiPolygon.STGeometryN(i + 1);

//                AddPolygon(polygon, transform);
//            }
//        }

//        private void AddMultiPoint(SqlGeometry multiPoint, Func<Point, Point> transform)//, ImageSource pointSymbol, Geometry symbol)
//        {
//            int numberOfPoints = multiPoint.STNumGeometries().Value;

//            for (int i = 0; i < numberOfPoints; i++)
//            {
//                var point = multiPoint.STGeometryN(i + 1);

//                AddPoint(point, transform);
//            }
//        }


//    }
//}
