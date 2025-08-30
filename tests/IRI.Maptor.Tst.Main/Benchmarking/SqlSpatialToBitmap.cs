using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace IRI.Maptor.Tst.Main.Benchmarking;

static class SqlSpatialToBitmap
{
    public static void Go()
    {
        DateTime t0 = DateTime.Now;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();


        //*********************************************************
        watch.Start();                                          //*
        DateTime t2 = DateTime.Now;                             //*
        //*********************************************************
        //
        string connectionString = @"server = YA-MORTAZA\MSSQLSERVER2012; integrated security = true;user id=sa; password=sa123456; database = TestSpatialDatabase";
        //
        SqlConnection connection = new SqlConnection(connectionString);
        //
        SqlCommand command = new SqlCommand("SELECT Geo FROM TehranBlocksMercator", connection);
        //
        connection.Open();
        //
        List<Microsoft.SqlServer.Types.SqlGeometry> geometries = new List<Microsoft.SqlServer.Types.SqlGeometry>();
        //
        SqlDataReader reader = command.ExecuteReader();
        //
        while (reader.Read())
        {
            Microsoft.SqlServer.Types.SqlGeometry sqlGeometry = new Microsoft.SqlServer.Types.SqlGeometry();
            System.Data.SqlTypes.SqlBytes bytes = reader.GetSqlBytes(0);
            using (MemoryStream stream = new MemoryStream(bytes.Buffer))
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    sqlGeometry.Read(binaryReader);
                }
            }
            geometries.Add(sqlGeometry.MakeValid().Reduce(10));
        }
        //
        //*********************************************************
        watch.Stop();                                           //*
        TimeSpan dt2 = DateTime.Now - t2;                       //*
        var shapesToSqlSpatial = watch.ElapsedMilliseconds;     //*
        watch.Reset();                                          //*
        //*********************************************************
        //*********************************************************
        //*********************************************************



        //*********************************************************
        //*********************************************************
        watch.Start();                                          //*
        DateTime t3 = DateTime.Now;                             //*
        //*********************************************************
        //
        //ScaleTransform transform = new ScaleTransform(+1, -1);
        //StreamGeometry geo =
        //    IRI.Maptor.Jab.Common.Convertor.StreamGeometryRenderer.ParseSqlGeometry(geometries, p => transform.Transform(p));
        //
        //*********************************************************
        watch.Stop();                                           //*
        TimeSpan dt3 = DateTime.Now - t3;                       //*
        var sqlSpatialToStreamGeometry = watch.ElapsedMilliseconds;//*
        watch.Reset();                                          //*
        //*********************************************************
        //*********************************************************
        //*********************************************************



        //*********************************************************
        watch.Start();                                          //*
        DateTime t4 = DateTime.Now;                             //*
        //*********************************************************
        //
        DrawingGroup drawing = new DrawingGroup();
        //
        //using (DrawingContext dc = drawing.Open())
        //{
        //    dc.DrawGeometry(new SolidColorBrush(Colors.Orange), new Pen(new SolidColorBrush(Colors.Black), 5), geo);
        //}
        //
        //*********************************************************
        watch.Stop();                                           //*
        TimeSpan dt4 = DateTime.Now - t4;                       //*
        var streamGeometryToDrawing = watch.ElapsedMilliseconds;//*
        watch.Reset();                                          //*
        //*********************************************************
        //*********************************************************
        //*********************************************************



        //*********************************************************
        watch.Start();                                          //*
        DateTime t5 = DateTime.Now;                             //*
        //*********************************************************
        //
        var drawingVisual = new DrawingVisual();
        //
        double scale = 1 / 25.0;
        //
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.PushTransform(new ScaleTransform(scale, scale));

            drawingContext.PushTransform(new TranslateTransform(-drawing.Bounds.X, -drawing.Bounds.Y));

            drawingContext.DrawDrawing(drawing);

        }
        //
        //*********************************************************
        watch.Stop();                                           //*
        TimeSpan dt5 = DateTime.Now - t5;                       //*
        var drawingToDrawingVisual = watch.ElapsedMilliseconds; //*
        watch.Reset();                                          //*
        //*********************************************************
        //*********************************************************
        //*********************************************************



        //*********************************************************
        watch.Start();                                          //*
        DateTime t6 = DateTime.Now;                             //*
        //*********************************************************
        //
        var width = drawing.Bounds.Width * scale;
        //
        var height = drawing.Bounds.Height * scale;
        //
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        //          
        bitmap.Render(drawingVisual);
        //
        var encoder = new PngBitmapEncoder();
        //
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        //
        //*********************************************************
        watch.Stop();                                           //*
        TimeSpan dt6 = DateTime.Now - t6;                       //*
        var VisualToBitmap = watch.ElapsedMilliseconds; //*
        watch.Reset();                                          //*
        //*********************************************************
        //*********************************************************
        //*********************************************************


        //*********************************************************
        watch.Start();                                          //*
        DateTime t7 = DateTime.Now;                             //*
        //*********************************************************
        //
        using (var stream = new FileStream("D:\\result.Reduce10Border.bmp", FileMode.Create))
        {
            encoder.Save(stream);
        }
        //
        //*********************************************************
        watch.Stop();                                           //*
        TimeSpan dt7 = DateTime.Now - t7;                       //*
        var saveImage = watch.ElapsedMilliseconds; //*
        watch.Reset();                                          //*
        //*********************************************************
        //*********************************************************
        //*********************************************************

        TimeSpan dt = DateTime.Now - t0;


    }
}
