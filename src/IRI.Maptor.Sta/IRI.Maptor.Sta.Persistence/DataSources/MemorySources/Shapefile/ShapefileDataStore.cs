//using System;
//using System.Text;
//using System.Threading.Tasks;

//using IRI.Maptor.Sta.ShapefileFormat;
//using IRI.Maptor.Sta.Common.Primitives;
//using IRI.Maptor.Sta.Persistence.Abstractions;

//namespace IRI.Maptor.Ket.Persistence.DataSources;

//public class ShapefileDataStore : IDataSource
//{
//    string _shpFileName, _spatialColumnName, _labelColumnName;

//    int _srid;

//    public DataSourceKind DataSourceKind => DataSourceKind.Shapefile;

//    public BoundingBox WebMercatorExtent { get { throw new NotImplementedException(); } }

//    public int Srid => throw new NotImplementedException();

//    // ShapefileDataStore exposes indexing logic only; it does not participate
//    // in loading/saving or client-side filtering directly.
//    public bool IsBusy => false;

//    public bool IsLoaded => true;

//    public bool HasPendingChanges => false;

//    public bool HasClientFilter => false;

//    public bool HasError => false;

//    public bool IsInitializing => false;

//    public bool IsProcessing => false;

//    public event EventHandler<bool>? IsInitializingChanged;

//    public event EventHandler<bool>? IsProcessingChanged;
     
//    public event EventHandler<bool>? IsLoadedChanged;

//    public event EventHandler<bool>? HasPendingChangesChanged;

//    public event EventHandler<bool>? IsClientFilteredChanged;

//    public event EventHandler<bool>? HasErrorChanged;

//    public Task LoadAsync() => Task.CompletedTask;

//    private ShapefileDataStore()
//    {

//    }

//    private ShapefileDataStore(string shpFileName, string spatialColumnName, int srid, Encoding encoding, string labelColumnName = null)
//    {
//        if (!System.IO.File.Exists(shpFileName))
//        {
//            throw new NotImplementedException();
//        }

//        this._shpFileName = shpFileName;

//        this._spatialColumnName = spatialColumnName;

//        this._labelColumnName = labelColumnName;

//        this._srid = srid;


//    }        

//    public async Task MakeIndex(bool overwrite = true)
//    {
//        var indexFileName = IRI.Maptor.Sta.ShapefileFormat.Shapefile.GetIndexFileName(_shpFileName);

//        if (overwrite && System.IO.File.Exists(indexFileName))
//        {
//            System.IO.File.Delete(indexFileName);
//        }

//        await Shapefile.CreateIndex(_shpFileName);
//    }

//    public async static Task<ShapefileDataStore> Create(string shpFileName, string spatialColumnName, int srid, Encoding encoding, string labelColumnName = null)
//    {
//        var result = new ShapefileDataStore(shpFileName, spatialColumnName, srid, encoding, labelColumnName);

//        await result.MakeIndex();

//        return result;
//    }

//}
