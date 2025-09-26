using Microsoft.SqlServer.Types;
using System;
using System.Data.SqlTypes;
using System.IO;
using System.Windows;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Ogc.SLD;
using IRI.Maptor.Sta.Common.Contracts.Google;
using IRI.Maptor.Sta.Spatial.Helpers;
using System.Data.Common;
using IRI.Maptor.Sta.Spatial.Model;

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

        TestShp();

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

}