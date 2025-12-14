using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.ShapefileFormat.Dbf;
using IRI.Maptor.Sta.ShapefileFormat.Model;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.ShapefileFormat.Reader;
using IRI.Maptor.Sta.ShapefileFormat.Writer;
using IRI.Maptor.Sta.ShapefileFormat.EsriType;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.ShapefileFormat.ShapeTypes.Abstractions;
using IRI.Maptor.Sta.Spatial.Extensions;
using IRI.Maptor.Sta.Spatial.IO.Prj;


namespace IRI.Maptor.Sta.ShapefileFormat;

public static class Shapefile
{
    //private static System.ComponentModel.License license = null;

    #region Read Shapefile

    public static Task<IEsriShapeCollection> ReadShapesAsync(string fileName, int? defaultSrid = null)
    {
        return Task.Run(() => { return ReadShapes(fileName, defaultSrid); });
    }

    public static IEsriShapeCollection ReadShapes(string shpFileName, int? defaultSrid = null)
    {
        //System.ComponentModel.LicenseManager.Validate(typeof(Shapefile));

        if (!System.IO.File.Exists(shpFileName))
        {
            throw new System.IO.FileNotFoundException();
        }

        MainFileHeader MainHeader;

        using (System.IO.FileStream shpStream = new System.IO.FileStream(shpFileName, System.IO.FileMode.Open))
        {
            using (System.IO.BinaryReader shpReader = new System.IO.BinaryReader(shpStream))
            {
                MainHeader = new MainFileHeader(shpReader.ReadBytes(ShapeConstants.MainHeaderLengthInBytes));
            }
        }

        int srid;

        if (!defaultSrid.HasValue)
        {
            srid = TryGetSrid(shpFileName);
        }
        else
        {
            srid = defaultSrid.Value;
        }

        switch (MainHeader.ShapeType)
        {
            case EsriShapeType.NullShape:
                throw new NotImplementedException();

            case EsriShapeType.EsriPoint:
                Reader.PointReader reader01 = new Reader.PointReader(shpFileName, srid);
                return reader01.elements;

            case EsriShapeType.EsriPolyLine:
                Reader.PolyLineReader reader02 = new Reader.PolyLineReader(shpFileName, srid);
                return reader02.elements;

            case EsriShapeType.EsriPolygon:
                Reader.PolygonReader reader03 = new Reader.PolygonReader(shpFileName, srid);
                return reader03.elements;

            case EsriShapeType.EsriMultiPoint:
                Reader.MultiPointReader reader04 = new Reader.MultiPointReader(shpFileName, srid);
                return reader04.elements;

            case EsriShapeType.EsriPointZM:
                Reader.PointZReader reader05 = new Reader.PointZReader(shpFileName, srid);
                return reader05.elements;

            case EsriShapeType.EsriPolyLineZM:
                Reader.PolyLineZReader reader06 = new Reader.PolyLineZReader(shpFileName, srid);
                return reader06.elements;

            case EsriShapeType.EsriPolygonZM:
                Reader.PolygonZReader reader07 = new Reader.PolygonZReader(shpFileName, srid);
                return reader07.elements;

            case EsriShapeType.EsriMultiPointZM:
                Reader.MultiPointZReader reader08 = new Reader.MultiPointZReader(shpFileName, srid);
                return reader08.elements;

            case EsriShapeType.EsriPointM:
                Reader.PointMReader reader09 = new Reader.PointMReader(shpFileName, srid);
                return reader09.elements;

            case EsriShapeType.EsriPolyLineM:
                Reader.PolyLineMReader reader10 = new Reader.PolyLineMReader(shpFileName, srid);
                return reader10.elements;

            case EsriShapeType.EsriPolygonM:
                Reader.PolygonMReader reader11 = new Reader.PolygonMReader(shpFileName, srid);
                return reader11.elements;

            case EsriShapeType.EsriMultiPointM:
                Reader.MultiPointMReader reader12 = new Reader.MultiPointMReader(shpFileName, srid);
                return reader12.elements;

            case EsriShapeType.EsriMultiPatch:
                throw new NotImplementedException();

            default:
                throw new NotImplementedException();
        }
    }


    public static Task<List<T>> ReadAsync<T>(string shpFileName, Func<Dictionary<string, object>, EsriShapeBase, T> mapFunc, bool correctFarsiCharacters = true, Encoding dataEncoding = null, int? defaultSrid = null, Encoding headerEncoding = null)
    {
        return Task.Run(() => { return Read(shpFileName, mapFunc, correctFarsiCharacters, dataEncoding, defaultSrid, headerEncoding); });
    }

    public static List<T> Read<T>(string shpFileName, Func<Dictionary<string, object>, EsriShapeBase, T> mapFunc, bool correctFarsiCharacters = true, Encoding dataEncoding = null, int? defaultSrid = null, Encoding headerEncoding = null)
    {
        var shapes = ReadShapes(shpFileName, defaultSrid);

        var attributeArray = Dbf.DbfFile.Read(GetDbfFileName(shpFileName), correctFarsiCharacters, dataEncoding, headerEncoding);

        //int srid;

        //if (defaultSrid != 0)
        //{
        //    srid = defaultSrid;
        //}
        //else
        //{
        //    srid = TryGetSrid(shpFileName);
        //}

        List<T> result = new List<T>(shapes.Count);

        for (int i = 0; i < attributeArray.Attributes.Count; i++)
        {
            //shapes[i].Srid = srid;

            var feature = mapFunc(attributeArray.Attributes[i], shapes[i]);

            result.Add(feature);
        }

        return result;
    }

    public static List<Feature<Point>> ReadAsFeature(string shpFileName, int? defaultSrid = null, bool correctFarsiCharacters = true, Encoding dataEncoding = null, Encoding headerEncoding = null)
    {
        //return Read<Feature>(shpFileName, d => new Feature() { Attributes = d }, (ies, srid, f) => f.TheGeometry = ies.AsGeometry(), dataEncoding, headerEncoding, correctFarsiCharacters, defaultSrid);

        return Read<Feature<Point>>(shpFileName,
            (d, ies) => new Feature<Point>()
            {
                Attributes = d,
                TheGeometry = ies.AsGeometry()
            },
            correctFarsiCharacters,
            dataEncoding,
            defaultSrid,
            headerEncoding);
         
    }

    public static MainFileHeader GetFileHeader(string shpFileName)
    {
        MainFileHeader MainHeader;

        using (System.IO.FileStream shpStream = new System.IO.FileStream(shpFileName, System.IO.FileMode.Open))
        {
            using (System.IO.BinaryReader shpReader = new System.IO.BinaryReader(shpStream))
            {
                MainHeader = new MainFileHeader(shpReader.ReadBytes(ShapeConstants.MainHeaderLengthInBytes));

                shpReader.Close();

                shpStream.Close();
            }
        }

        return MainHeader;
    }


    public static List<Feature<Point>> ReadAsFeature(string shpFileName, Encoding dataEncoding, SrsBase targetSrs = null, Encoding headerEncoding = null, bool correctFarsiCharacters = true, string label = null)
    {
        if (targetSrs != null)
        {
            var sourceSrs = TryGetSrs(shpFileName);

            Func<Point, Point> map = p => p.Project(sourceSrs, targetSrs);

            return Read(
                    shpFileName,
                    (d, ies) => new Feature<Point>() { Attributes = d, LabelAttribute = label, TheGeometry = ies.Transform(map as Func<IPoint, IPoint>, targetSrs.Srid).AsGeometry() },
                    //(d, srid, feature) => feature.TheSqlGeometry = d.Transform(map, targetSrs.Srid).AsSqlGeometry(),
                    true,
                    dataEncoding,
                    null,
                    headerEncoding
                    );
        }
        else
        {

            return Read(
                    shpFileName,
                    (d, ies) => new Feature<Point>() { Attributes = d, LabelAttribute = label, TheGeometry = ies.AsGeometry() },
                    //d => new SqlFeature() { Attributes = d, LabelAttribute = label },
                    //(d, srid, feature) => feature.TheSqlGeometry = d.AsSqlGeometry(),
                    true,
                    dataEncoding,
                    null,
                    headerEncoding);
        }
    }

    public static async Task<List<Feature<Point>>> ReadAsFeatureAsync(string shpFileName, Encoding dataEncoding, SrsBase targetSrs = null, Encoding headerEncoding = null, bool correctFarsiCharacters = true, string label = null)
    {
        if (targetSrs != null)
        {
            var sourceSrs = TryGetSrs(shpFileName);

            Func<Point, Point> map = p => p.Project(sourceSrs, targetSrs);

            return await ReadAsync(
                    shpFileName,
                    (d, ies) => new Feature<Point>() { Attributes = d, LabelAttribute = label, TheGeometry = ies.Transform(map as Func<IPoint, IPoint>, targetSrs.Srid).AsGeometry() },
                    //d => new SqlFeature() { Attributes = d, LabelAttribute = label },
                    //(d, srid, feature) => feature.TheSqlGeometry = d.Transform(map, targetSrs.Srid).AsSqlGeometry(),
                    true,
                    dataEncoding,
                    null,
                    headerEncoding);
        }
        else
        {
            return await ReadAsync(
                    shpFileName,
                    (d, ies) => new Feature<Point>() { Attributes = d, LabelAttribute = label, TheGeometry = ies.AsGeometry() },
                    //d => new SqlFeature() { Attributes = d, LabelAttribute = label },
                    //(d, srid, feature) => feature.TheSqlGeometry = d.AsSqlGeometry(),
                    true,
                    dataEncoding,
                    null,
                    headerEncoding);
        }
    }




    #endregion

    #region Prj & Srid

    public static EsriPrjFile? TryReadPrjFile(string shpFileName)
    {
        var prjFile = GetPrjFileName(shpFileName);

        try
        {
            if (System.IO.File.Exists(prjFile))
            {
                return new EsriPrjFile(prjFile);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    public static SrsBase? TryGetSrs(string shpFileName)
    {
        var esriPrjFile = TryReadPrjFile(shpFileName);

        return esriPrjFile?.AsMapProjection() ?? null;
    }

    internal static int TryGetSrid(string shpFileName)
    {
        var esriPrjFile = TryReadPrjFile(shpFileName);

        return esriPrjFile?.Srid ?? 0;
    }

    #endregion


    #region Write (Save) Shapefile 

    public static void Save(string shpFileName, IEnumerable<EsriShapeBase> shapes, bool createDbf = false, bool overwrite = false, SrsBase srs = null)
    {
        if (shapes.IsNullOrEmpty())
        {
            return;
        }

        var directory = System.IO.Path.GetDirectoryName(shpFileName);

        if (!System.IO.Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        IEsriShapeCollection collection = new EsriShapeCollection<EsriShapeBase>(shapes);

        EsriShapeType shapeType = shapes.First().EsriType;

        using (System.IO.MemoryStream featureWriter = new System.IO.MemoryStream())
        {
            int recordNumber = 0;

            foreach (EsriShapeBase item in shapes)
            {
                featureWriter.Write(ShpWriter.WriteHeaderToByte(++recordNumber, item), 0, 2 * ShapeConstants.IntegerSize);

                featureWriter.Write(item.WriteContentsToByte(), 0, 2 * item.ContentLength);
            }

            using (System.IO.MemoryStream shpWriter = new System.IO.MemoryStream())
            {
                int fileLength = (int)featureWriter.Length / 2 + 50;

                shpWriter.Write(ShpWriter.WriteMainHeader(collection, fileLength, shapeType), 0, 100);

                shpWriter.Write(featureWriter.ToArray(), 0, (int)featureWriter.Length);

                //var mode = overwrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew;
                var mode = Shapefile.GetMode(shpFileName, overwrite);

                System.IO.FileStream stream = new System.IO.FileStream(shpFileName, mode);

                shpWriter.WriteTo(stream);

                stream.Close();

                shpWriter.Close();

                featureWriter.Close();
            }
        }

        ShxWriter.Write(Shapefile.GetShxFileName(shpFileName), collection, shapeType, overwrite);

        if (createDbf)
        {
            Dbf.DbfFile.WriteDefault(Shapefile.GetDbfFileName(shpFileName), collection.Count, overwrite);
        }

        //try to automatically find srs
        if (srs == null)
        {
            var srid = shapes.First()?.Srid ?? 0;

            srs = SridHelper.AsSrsBase(srid);
        }

        if (srs != null)
        {
            SaveAsPrj(shpFileName, srs, overwrite);
        }

    }

    //public static void Save<T>(string shpFileName, IEnumerable<T> objects, Func<T, IEsriShape> map, bool createDbf = false, bool overwrite = false)
    //{
    //    if (objects.IsNullOrEmpty())
    //    {
    //        return;
    //    }

    //    IEsriShapeCollection collection = new EsriShapeCollection<IEsriShape>(objects.Select(o => map(o)));

    //    Save(shpFileName, collection, createDbf, overwrite);
    //}

    //public static void Save(string shpFileName, IEnumerable<IEsriShape> shapes, bool createDbf = false, bool overwrite = false)
    //{
    //    if (shapes.IsNullOrEmpty())
    //    {
    //        return;
    //    }

    //    IEsriShapeCollection collection = new EsriShapeCollection<IEsriShape>(shapes);

    //    Save(shpFileName, collection, createDbf, overwrite);
    //}

    public static void Save<T>(string shpFileName,
                                  IEnumerable<T> values,
                                  Func<T, EsriShapeBase> geometryMap,
                                  List<ObjectToDbfTypeMap<T>> attributeMappings,
                                  Encoding encoding,
                                  SrsBase srs,
                                  bool overwrite = false)
    {
        SaveAsShapefile(shpFileName, values.Select(v => geometryMap(v)), false, srs, overwrite);

        DbfFile.Write(GetDbfFileName(shpFileName), values, attributeMappings, encoding, overwrite);
    }

    // 1400.03.02- remove method
    //public static void Save<T>(string shpFileName,
    //                              IEnumerable<T> values,
    //                              Func<T, IEsriShape> geometryMap,
    //                              ObjectToDfbFields<T> attributeMappings,
    //                              Encoding encoding,
    //                              SrsBase srs,
    //                              bool overwrite = false)
    //{
    //    SaveAsShapefile(shpFileName, values.Select(v => geometryMap(v)), false, srs, overwrite);

    //    DbfFile.Write(GetDbfFileName(shpFileName), values, attributeMappings, encoding, overwrite);
    //}


    // old
    //public static void Save<T>(string shpFileName,
    //                             IEnumerable<T> values,
    //                             Func<T, IEsriShape> geometryMap,
    //                             List<Func<T, object>> attributeMapping,
    //                             List<DbfFieldDescriptor> fields,
    //                             Encoding encoding,
    //                             SrsBase srs,
    //                             bool overwrite = false)
    //{
    //    SaveAsShapefile(shpFileName, values.Select(v => geometryMap(v)), false, srs, overwrite);

    //    DbfFile.Write(GetDbfFileName(shpFileName), values, attributeMapping, fields, encoding, overwrite);

    //    //DbfFile.Write(GetDbfFileName(shpFileName), values, attributeMappings.Select(m => m.MapFunction).ToList(), attributeMappings.Select(m => m.FieldType).ToList(), encoding, overwrite);
    //}

    public static void SaveAsShapefile(string shpFileName, IEnumerable<EsriShapeBase> data, bool createEmptyDbf, SrsBase srs, bool overwrite = false)
    {
        Save(shpFileName, data, createEmptyDbf, overwrite, srs);

        //SaveAsPrj(shpFileName, crs, overwrite);
    }

    public static void SaveAsShapefile<T>(string shpFileName, IEnumerable<T> data, Func<T, EsriShapeBase> map, bool createEmptyDbf, SrsBase srs, bool overwrite = false)
    {
        Save(shpFileName, data.Select(t => map(t)), createEmptyDbf, overwrite, srs);

        //SaveAsPrj(shpFileName, crs, overwrite);
    }

    public static void SaveAsShapefile(string shpFileName, IEnumerable<GeoJsonFeature> features, bool isLongitudeFirst = true, SrsBase srs = null)
    {
        var shapes = features.Select(f => f.Geometry.AsEsriShape(isLongitudeFirst, srs.Srid));

        Save(shpFileName, shapes, false, true, srs);

        var dbfFile = GetDbfFileName(shpFileName);

        var attributes = features.Select(f => f.Properties).ToList();

        DbfFile.Write(dbfFile, attributes, Encoding.GetEncoding(1256), true);
    }

    public static void SaveAsShapefile<T>(string shpFileName, IEnumerable<Feature<T>> features, SrsBase srs = null) where T : IPoint, new()
    {
        var shapes = features.Select(f => f.TheGeometry.AsEsriShape(srs?.Srid ?? 0));

        Save(shpFileName, shapes, false, true, srs);

        var dbfFile = GetDbfFileName(shpFileName);

        var attributes = features.Select(f => f.Attributes).ToList();

        DbfFile.Write(dbfFile, attributes, Encoding.GetEncoding(1256), true);
    }

    public static void SaveAsPrj(string shpFileName, SrsBase srs, bool overwrite)
    {
        var prjFileName = GetPrjFileName(shpFileName);

        if (overwrite && System.IO.File.Exists(prjFileName))
        {
            System.IO.File.Delete(prjFileName);
        }

        srs.AsEsriPrj().Save(prjFileName);
    }

    #endregion


    #region Project

    //public static Func<Point, Point> GetProjectFunc(string shapefileName, SrsBase targetCrs)
    //{
    //    //var sourcePrj = IRI.Maptor.Sta.ShapefileFormat.Shapefile.GetPrjFileName(shapefileName);

    //    //if (!System.IO.File.Exists(sourcePrj))
    //    //{
    //    //    throw new System.IO.FileNotFoundException($"prj file not found. {sourcePrj}");
    //    //}

    //    //var sourceCrs = new ShapefileFormat.Prj.EsriPrjFile(sourcePrj).AsMapProjection();
    //    var sourceSrs = TryGetSrs(shapefileName);

    //    Func<Point, Point> result = null;

    //    if (sourceSrs.Ellipsoid.AreTheSame(targetCrs.Ellipsoid))
    //    {
    //        result = new Func<Point, Point>(p => (Point)targetCrs.FromGeodetic(sourceSrs.ToGeodetic(p)));
    //    }
    //    else
    //    {
    //        result = new Func<Point, Point>(p => (Point)targetCrs.FromGeodetic(sourceSrs.ToGeodetic(p), sourceSrs.Ellipsoid));
    //    }

    //    return result;
    //}

    public static Task<List<EsriShapeBase>> ProjectAsync(string shpFileName, SrsBase targetSrs)
    {
        return Task.Run(() => Project(shpFileName, targetSrs));
    }

    public static List<EsriShapeBase> Project(string shpFileName, SrsBase targetSrs)
    {
        var sourcePrj = GetPrjFileName(shpFileName);

        if (!System.IO.File.Exists(sourcePrj))
        {
            throw new System.IO.FileNotFoundException($"prj file not found. {sourcePrj}");
        }

        var sourceSrs = new EsriPrjFile(sourcePrj).AsMapProjection();

        var data = ReadShapes(shpFileName).ToList();

        return Project(data, sourceSrs, targetSrs);

        ////Old Method
        //var sourcePrj = GetPrjFileName(shpFileName);
        //if (!System.IO.File.Exists(sourcePrj))
        //{
        //    throw new System.IO.FileNotFoundException($"prj file not found. {sourcePrj}");
        //}
        //var sourceCrs = new Prj.PrjFile(sourcePrj).AsMapProjection();
        //var data = Read(shpFileName).ToList();
        //List<IShape> result = new List<IShape>(data.Count);
        //if (sourceCrs.Ellipsoid.AreTheSame(targetCrs.Ellipsoid))
        //{
        //    for (int i = 0; i < data.Count; i++)
        //    {
        //        var c1 = data[i].Transform(p => sourceCrs.ToGeodetic(p));
        //        result.Add(c1.Transform(p => targetCrs.FromGeodetic(p)));
        //    }
        //}
        //else
        //{
        //    for (int i = 0; i < data.Count; i++)
        //    {
        //        var c1 = data[i].Transform(p => sourceCrs.ToGeodetic(p));
        //        result.Add(c1.Transform(p => targetCrs.FromGeodetic(p, sourceCrs.Ellipsoid)));
        //    }
        //}
        //return result;
    }

    public static List<EsriShapeBase> Project(List<EsriShapeBase> values, string sourceEsriWktPrj, string targetEsriWktPrj) /*where TEsriPoint : IPoint, new()*/
    {
        var sourceSrs = EsriPrjFile.Parse(sourceEsriWktPrj).AsMapProjection();

        var targetSrs = EsriPrjFile.Parse(targetEsriWktPrj).AsMapProjection();

        return Project(values, sourceSrs, targetSrs);
    }

    public static List<EsriShapeBase> Project(List<EsriShapeBase> values, SrsBase sourceSrs, SrsBase targetSrs) /*where TEsriPoint : IPoint, new()*/
    {
        List<EsriShapeBase> result = new List<EsriShapeBase>(values.Count);

        if (sourceSrs.Ellipsoid.AreTheSame(targetSrs.Ellipsoid))
        {
            for (int i = 0; i < values.Count; i++)
            {
                var c1 = values[i].Transform(p => sourceSrs.ToGeodetic<EsriPoint>(new EsriPoint() { X = p.X, Y = p.Y }), targetSrs.Srid);

                result.Add(c1.Transform(p => targetSrs.FromGeodetic<EsriPoint>(new EsriPoint() { X = p.X, Y = p.Y }), targetSrs.Srid));
            }
        }
        else
        {
            for (int i = 0; i < values.Count; i++)
            {
                var c1 = values[i].Transform(p => sourceSrs.ToGeodetic<EsriPoint>(new EsriPoint() { X = p.X, Y = p.Y }), targetSrs.Srid);

                result.Add(c1.Transform(p => targetSrs.FromGeodetic<EsriPoint>(new EsriPoint() { X = p.X, Y = p.Y }, sourceSrs.Ellipsoid), targetSrs.Srid));
            }
        }

        return result;
    }

    public static void ProjectAndSaveAsShapefile(string sourceShapefileName, string targetShapefileName, SrsBase targetSrs, bool overwrite = false)
    {
        if (!CheckAllNeededFilesExists(sourceShapefileName, true))
        {
            throw new NotImplementedException();
        }

        var result = Project(sourceShapefileName, targetSrs);
        //result.ToList().Select(i=>i.MinimumBoundingBox).ToList()
        SaveAsShapefile(targetShapefileName, result, false, targetSrs, overwrite);

        var destinationDbfFileName = GetDbfFileName(targetShapefileName);

        System.IO.File.Copy(GetDbfFileName(sourceShapefileName), destinationDbfFileName, overwrite);
    }


    #endregion


    #region Helper Functions

    public static BoundingBox CalculateBoundingBox(MainFileHeader header, params MainFileHeader[] headers)
    {
        double xMin = header.XMin;

        double xMax = header.XMax;

        double yMin = header.YMin;

        double yMax = header.YMax;

        if (headers.Length > 0)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].XMin < xMin)
                {
                    xMin = headers[i].XMin;
                }
                if (headers[i].YMin < yMin)
                {
                    yMin = headers[i].YMin;
                }
                if (headers[i].XMax > xMax)
                {
                    xMax = headers[i].XMax;
                }
                if (headers[i].YMax > yMax)
                {
                    yMax = headers[i].YMax;
                }
            }
        }
        return new BoundingBox(xMin, yMin, xMax, yMax);
    }

    public static BoundingBox CalculateBoundingBox(MainFileHeader[] headers)
    {
        if (headers.Length < 1)
            throw new NotImplementedException();

        double xMin = headers[0].XMin;

        double xMax = headers[0].XMax;

        double yMin = headers[0].YMin;

        double yMax = headers[0].YMax;

        for (int i = 1; i < headers.Length; i++)
        {
            if (headers[i].XMin < xMin)
            {
                xMin = headers[i].XMin;
            }
            if (headers[i].YMin < yMin)
            {
                yMin = headers[i].YMin;
            }
            if (headers[i].XMax > xMax)
            {
                xMax = headers[i].XMax;
            }
            if (headers[i].YMax > yMax)
            {
                yMax = headers[i].YMax;
            }
        }

        return new BoundingBox(xMin, yMin, xMax, yMax);
    }

    public static string GetShxFileName(string shpFileName)
    {
        //string directoryName = System.IO.Path.GetDirectoryName(shpFileName);

        //string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(shpFileName);

        //return string.Format("{0}\\{1}.shx", directoryName, fileNameWithoutExtension);

        return System.IO.Path.ChangeExtension(shpFileName, "shx");
    }

    public static string GetDbfFileName(string shpFileName)
    {
        //string directoryName = System.IO.Path.GetDirectoryName(shpFileName);

        //string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(shpFileName);

        //return string.Format("{0}\\{1}.dbf", directoryName, fileNameWithoutExtension);

        return System.IO.Path.ChangeExtension(shpFileName, "dbf");
    }

    public static string GetPrjFileName(string shpFileName)
    {
        return System.IO.Path.ChangeExtension(shpFileName, "prj");
    }

    public static string GetCpgFileName(string shpFileName)
    {
        //string directoryName = System.IO.Path.GetDirectoryName(shpFileName);

        //string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(shpFileName);

        //return string.Format("{0}\\{1}.cpg", directoryName, fileNameWithoutExtension);

        return System.IO.Path.ChangeExtension(shpFileName, "cpg");
    }

    public static string GetIndexFileName(string shpFileName)
    {
        //string directoryName = System.IO.Path.GetDirectoryName(shpFileName);

        //string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(shpFileName);

        //return string.Format("{0}\\{1}.ind", directoryName, fileNameWithoutExtension);

        return System.IO.Path.ChangeExtension(shpFileName, "ind");
    }

    public static System.IO.FileMode GetMode(string fileName, bool overwrite)
    {
        return System.IO.File.Exists(fileName) && overwrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew;
    }

    public static bool CheckAllNeededFilesExists(string fileName, bool checkPrjFileExist = false)
    {
        var result =
            System.IO.File.Exists(fileName) &&
            System.IO.File.Exists(GetShxFileName(fileName)) &&
            System.IO.File.Exists(GetDbfFileName(fileName));

        return checkPrjFileExist ? result && System.IO.File.Exists(GetPrjFileName(fileName)) : result;

    }

    #endregion


    #region Indexing
    //Indexing
    public static async Task CreateIndex(string shpFileName)
    {
        ShxReader shxFile = new ShxReader(Shapefile.GetShxFileName(shpFileName));

        var shapes = await ReadShapesAsync(shpFileName);

        int numberOfRecords = shxFile.NumberOfRecords;

        List<Indexing.ShpIndex> indexes = new List<Indexing.ShpIndex>(numberOfRecords);

        for (int i = 0; i < numberOfRecords; i++)
        {
            int offset, contentLength;

            shxFile.GetRecord(i, out offset, out contentLength);

            if (contentLength == 2)
                continue;

            indexes.Add(new Indexing.ShpIndex()
            {
                RecordNumber = i,
                MinimumBoundingBox = shapes[indexes.Count].MinimumBoundingBox
            });
        }

        Indexing.IndexIO.Write(GetIndexFileName(shpFileName), indexes);

    }

    public static async Task<IEsriShapeCollection> Read(string shpFileName, BoundingBox boundingBox)
    {
        var indexFileName = GetIndexFileName(shpFileName);

        if (!CheckAllNeededFilesExists(shpFileName) || !System.IO.File.Exists(indexFileName))
        {
            throw new NotImplementedException();
        }

        var shpIndexes = await Indexing.IndexIO.Read(indexFileName);

        var filtered = shpIndexes.Where(i => i.MinimumBoundingBox.Intersects(boundingBox)).ToList();

        var shxFile = new ShxReader(GetShxFileName(shpFileName));

        int srid = TryGetSrid(shpFileName);

        using (System.IO.FileStream shpStream = new System.IO.FileStream(shpFileName, System.IO.FileMode.Open))
        {
            using (var shpReader = new System.IO.BinaryReader(shpStream))
            {
                switch (shxFile.MainHeader.ShapeType)
                {
                    case EsriShapeType.EsriPoint:
                    case EsriShapeType.EsriPointZM:
                    case EsriShapeType.EsriPointM:
                        return ExtractPoints(shpReader, shxFile, filtered, srid);

                    case EsriShapeType.EsriMultiPoint:
                    case EsriShapeType.EsriMultiPointZM:
                    case EsriShapeType.EsriMultiPointM:
                        return ExtractMultiPoints(shpReader, shxFile, filtered, srid);

                    case EsriShapeType.EsriPolyLine:
                    case EsriShapeType.EsriPolyLineZM:
                    case EsriShapeType.EsriPolyLineM:
                        return ExtractPolyLines(shpReader, shxFile, filtered, srid);

                    case EsriShapeType.EsriPolygon:
                    case EsriShapeType.EsriPolygonZM:
                    case EsriShapeType.EsriPolygonM:
                        return ExtractPolygons(shpReader, shxFile, filtered, srid);

                    case EsriShapeType.NullShape:
                    case EsriShapeType.EsriMultiPatch:
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    private static IEsriShapeCollection ExtractPoints(System.IO.BinaryReader shpReader, ShxReader shxReader, List<Indexing.ShpIndex> indexes, int srid)
    {
        List<EsriPoint> geometries = new List<EsriPoint>(indexes.Count);

        for (int i = 0; i < indexes.Count; i++)
        {
            int offset, contentLength;

            shxReader.GetRecord(indexes[i].RecordNumber, out offset, out contentLength);

            geometries[i] = PointReader.Read(shpReader, offset, contentLength, srid);
        }

        return new EsriShapeCollection<EsriPoint>(geometries);
    }

    private static IEsriShapeCollection ExtractMultiPoints(System.IO.BinaryReader shpReader, ShxReader shxReader, List<Indexing.ShpIndex> indexes, int srid)
    {
        List<EsriMultiPoint> geometries = new List<EsriMultiPoint>(indexes.Count);

        for (int i = 0; i < indexes.Count; i++)
        {
            int offset, contentLength;

            shxReader.GetRecord(indexes[i].RecordNumber, out offset, out contentLength);

            geometries[i] = MultiPointReader.Read(shpReader, offset, contentLength, srid);
        }

        return new EsriShapeCollection<EsriMultiPoint>(geometries);
    }

    private static IEsriShapeCollection ExtractPolyLines(System.IO.BinaryReader shpReader, ShxReader shxReader, List<Indexing.ShpIndex> indexes, int srid)
    {
        List<EsriPolyline> geometries = new List<EsriPolyline>(indexes.Count);

        for (int i = 0; i < indexes.Count; i++)
        {
            int offset, contentLength;

            shxReader.GetRecord(indexes[i].RecordNumber, out offset, out contentLength);

            geometries[i] = PolyLineReader.Read(shpReader, offset, contentLength, srid);
        }

        return new EsriShapeCollection<EsriPolyline>(geometries);
    }

    private static IEsriShapeCollection ExtractPolygons(System.IO.BinaryReader shpReader, ShxReader shxReader, List<Indexing.ShpIndex> indexes, int srid)
    {
        List<EsriPolygon> geometries = new List<EsriPolygon>(indexes.Count);

        for (int i = 0; i < indexes.Count; i++)
        {
            int offset, contentLength;

            shxReader.GetRecord(indexes[i].RecordNumber, out offset, out contentLength);

            geometries[i] = PolygonReader.Read(shpReader, offset, contentLength, srid);
        }

        return new EsriShapeCollection<EsriPolygon>(geometries);
    }

    #endregion

}
