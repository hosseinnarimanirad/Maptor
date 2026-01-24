using System;
using System.IO;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;

using Microsoft.SqlServer.Types;
using Microsoft.Win32;

using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Sta.Common.Contracts.Google;
using IRI.Maptor.Sta.Spatial.Helpers;
using IRI.Maptor.Sta.Spatial.Model;
using IRI.Maptor.Ket.GdiPlus.Helpers;
using IRI.Maptor.Ket.GdiPlus.WorldfileFormat;
using IRI.Maptor.Sta.Spatial.IO;
using IRI.Maptor.Sta.Spatial.DigitalTerrainModeling;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Spatial.IO.TopoJson;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Windows;

namespace IRI.Maptor.MasterProjectWPF;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void loaded_l(object sender, RoutedEventArgs e)
    {
        //SqlServerTypes.Utilities.LoadNativeAssembliesv14(AppDomain.CurrentDomain.BaseDirectory);

        var polygon = SqlGeometry.Parse(new SqlString("POLYGON( (0 0 9, 30 0 9, 30 30 9, 0 30 9, 0 0 9) )"));
        var temp = polygon.AsGml();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        //TestSld(@"C:\Users\Hossein\Downloads\point_pointasgraphic.sld");
        //TestSld(@"C:\Users\Hossein\Downloads\point_attribute.sld");
        //TestSld(@"E:\Work\Barg\Sample SLD\barg\tower.sld");

        //var result2 = TiffReader.ReadGeoTiff32bitDEM(@"E:\Data\DEM\Iran_DEM_5s\Iran_DEM_5s_WebMercator.tif");



        ////var result = ImageHelper.Read32BitGrayscaleTiff(@"E:\Data\DEM\Iran_DEM_5s\Iran_DEM_5s.tif");

        //var worldfile = Worldfile.Read(@"E:\Data\DEM\Iran_DEM_5s\Iran_DEM_5s_WebMercator.tfw");

        //WorldfileMatrix16bit matrix = new WorldfileMatrix16bit(result2.Data, worldfile.XPixelSize, worldfile.YPixelSize, worldfile.CenterOfUpperLeftPixel);

        //matrix.WriteToBinarySimple(@"E:\Data\DEM\Iran_DEM_5s\Iran_DEM_5s_WebMercator.bdem");

        var wkt = "POLYGON((-20 -20, -20 20, 20 20, 20 -20, -20 -20), (10 0, 0 10, 0 -10, 10 0), (-10 0, -10 10, -15 0, -10 0))";

        TestDxf(@"E:\test.dxf", wkt);
    }

    private void TestShp()
    {
        var shpFile = @"E:\Data\Internet\World\World_Countries.shp";

        byte[] existingData = File.ReadAllBytes(shpFile);
        Console.WriteLine($"File loaded: {existingData.Length} bytes");

        // Data to append
        byte[] newData = System.IO.File.ReadAllBytes(@"E:\Work\1.OurProducts\Makan Negar\Assets\MakanNegarLogo.png");

        // Append new data to the file
        using (FileStream fs = new FileStream(shpFile, FileMode.Append, FileAccess.Write))
        {
            fs.Write(newData, 0, newData.Length);
        }
    }

    private void TestSld(string fileName)
    {
        var sld = XmlHelper.DeserializeFromFile<StyledLayerDescriptor>(fileName);

        string modifiedPath = Path.Combine(
                            Path.GetDirectoryName(fileName),
                            Path.GetFileNameWithoutExtension(fileName) + "_m" + Path.GetExtension(fileName));

        XmlHelper.Serialize(modifiedPath, sld, true);
    }


    private void TestDxf(string fileName, string wkt)
    {
        Geometry<IRI.Maptor.Sta.Common.Primitives.Point>.FromWkt(wkt, 0).SaveAsDxf(fileName);

        var geometries = DxfReader.ReadFromFile(fileName, defaultSrid: 0);

        if (geometries.First().AsWkt() != wkt)
            return;

    }

    /// <summary>
    /// Sample code to read TopoJSON file
    /// </summary>
    private void Button_ReadTopoJson_Click(object sender, RoutedEventArgs e)
    {
        // Open file dialog to select TopoJSON file
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select TopoJSON File",
            Filter = "TopoJSON Files (*.topojson;*.json)|*.topojson;*.json|All Files (*.*)|*.*",
            FilterIndex = 1,
            RestoreDirectory = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var fileName = openFileDialog.FileName;
                
                // Method 1: Read TopoJSON file
                var topology = TopoJson.ReadFromFile(fileName);
                
                // Display topology information
                var info = new StringBuilder();
                info.AppendLine($"✅ TopoJSON file loaded successfully!");
                info.AppendLine($"📁 File: {Path.GetFileName(fileName)}");
                info.AppendLine($"📊 Number of objects: {topology.Objects.Count}");
                info.AppendLine($"🔗 Number of arcs: {topology.Arcs.Count}");
                
                if (topology.BBox != null)
                {
                    info.AppendLine($"📦 BBox: [{string.Join(", ", topology.BBox.Select(v => v.ToString("F6")))}]");
                }
                
                if (topology.Transform != null)
                {
                    info.AppendLine($"🔄 Transform:");
                    info.AppendLine($"   Scale: [{topology.Transform.Scale[0]:E6}, {topology.Transform.Scale[1]:E6}]");
                    info.AppendLine($"   Translate: [{topology.Transform.Translate[0]:F6}, {topology.Transform.Translate[1]:F6}]");
                }
                
                info.AppendLine();
                info.AppendLine("📐 Objects:");
                
                // Convert TopoJSON to Geometry objects
                var geometries = TopoJson.ToGeometry(topology, srid: 4326);
                
                foreach (var kvp in geometries)
                {
                    var geometry = kvp.Value;
                    info.AppendLine($"   • {kvp.Key}: {geometry.Type}");
                    info.AppendLine($"      Points: {geometry.TotalNumberOfPoints}");
                    
                    var bbox = geometry.GetBoundingBox();
                    info.AppendLine($"      BBox: [{bbox.XMin:F6}, {bbox.YMin:F6}, {bbox.XMax:F6}, {bbox.YMax:F6}]");
                    
                    // Calculate length/area based on geometry type
                    if (geometry.IsLineStringOrMultiLineString())
                    {
                        var length = geometry.GetEuclideanLength();
                        info.AppendLine($"      Length: {length:F6}");
                    }
                    else if (geometry.IsPolygonOrMultiPolygon())
                    {
                        var area = geometry.EuclideanArea;
                        info.AppendLine($"      Area: {area:F6}");
                    }
                    
                    // Get first few points as sample
                    var points = geometry.GetAllPoints().Take(3).ToList();
                    if (points.Count > 0)
                    {
                        info.AppendLine($"      Sample points:");
                        foreach (var pt in points)
                        {
                            info.AppendLine($"         ({pt.X:F6}, {pt.Y:F6})");
                        }
                        if (geometry.TotalNumberOfPoints > 3)
                        {
                            info.AppendLine($"         ... and {geometry.TotalNumberOfPoints - 3} more");
                        }
                    }
                    
                    info.AppendLine();
                }
                
                // Show the information in a message box
                MessageBox.Show(info.ToString(), "TopoJSON File Information", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Optional: Save first geometry as WKT for inspection
                if (geometries.Count > 0)
                {
                    var firstGeometry = geometries.First().Value;
                    var wkt = firstGeometry.AsWkt();
                    
                    // Save WKT to a text file next to the TopoJSON file
                    var wktFileName = Path.ChangeExtension(fileName, ".wkt");
                    File.WriteAllText(wktFileName, wkt);
                    
                    MessageBox.Show($"✅ First geometry saved as WKT:\n{wktFileName}", 
                        "WKT Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error reading TopoJSON file:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}