using System.ComponentModel;
using System.Globalization;

using Microsoft.SqlServer.Types;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Spatial.Analysis;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Res.LRSimplification.ViewModel.TrainingModel;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Jab.Common.Models.Settings;
using IRI.Maptor.Jab.Common.Data;
using IRI.Maptor.Jab.Common.Localization;

using IRI.Maptor.Jab.Common.Layers;
using IRI.Maptor.Res.LRSimplification.Common;


namespace IRI.Maptor.Res.LRSimplification.ViewModel;

public class ApplicationPresenter : MapViewModelBase
{

    private LogisticSimplification<Point> _lrsv7Model;
    private LogisticSimplification<Point> GetLRSv7Model()
    {
        if (_lrsv7Model is null)
        {
            _lrsv7Model = LoadTrainingDate(@"E:\University.Ph.D\4. Paper2\Model\LR7v23.json");
        }

        return _lrsv7Model;
    }


    private LogisticSimplification<Point> _lrsv4Model;
    private LogisticSimplification<Point> GetLRSv4Model()
    {
        if (_lrsv4Model is null)
        {
            _lrsv4Model = LoadTrainingDate(@"E:\University.Ph.D\4. Paper2\Model\LR4v23.json");
        }

        return _lrsv4Model;
    }


    private LogisticSimplification<Point> _lrsv4_lf_Model;
    private LogisticSimplification<Point> GetLRSv4LfModel()
    {
        if (_lrsv4_lf_Model is null)
        {
            _lrsv4_lf_Model = LoadTrainingDate(@"E:\University.Ph.D\4. Paper2\Model\LR4v23-LF.json");
        }

        return _lrsv4_lf_Model;
    }


    public ApplicationPresenter(/*MapViewer map*/)
    {
        this.SyntheticDataTrainingData = new SyntheticDataTrainingViewModel(this);

        //this.Geometries = new ObservableCollection<Model.CustomSqlGeometry>();

        //this.map = map;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        this.MapSettings.MaxGoogleZoomLevel = 20;

        //this.DefaultVectorLayerFeatureTableCommands.Add(p => ShowGeometryDetailsCommand.CreateShowGeometryDetailCommand(p));
        //this.DefaultVectorLayerFeatureTableCommands.Add(ShowGeometryDetailsCommand.ApplyPointCount);

        this.DrawingItemCommands.Add(layer => LegendCommand.CreateExportDrawingItemLayerAsPng(this, layer));
        //this.DataContext = new ViewModel.ApplicationPresenter(this.map);
        this.DrawingItemCommands.Add(layer => LegendCommand.CreateCloneDrawingItemCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateSimplifyByAreaCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateSimplifyByAngleCommand(this, layer));
        this.DrawingItemCommands.Add(layer => LegendCommand.CreateSimplifyByVWCommand(this, layer));
        this.DrawingItemCommands.Add(layer => LegendCommand.CreateSimplifyByRDPCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateGetExteriorRingCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateGetEnvelopeCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateGetConvexHullCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateGetBoundaryCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateBreakIntoGeometriesCommand(this, layer));
        //this.DrawingItemCommands.Add(layer => LegendCommand.CreateBreakIntoPointsCommand(this, layer));
         
        this.DrawingItemCommands.Add(layer =>
        {
            Action action = new Action(async () =>
            {
                if (this.LogisticGeometrySimplification == null)
                {
                    await this.DialogService.ShowMessageAsync<MainWindow>("no model available!", "error", null);

                    return;
                }

                //var toScreenMap = this.RequestGetToScreenMap?.Invoke();
                var toScreenMap = this.CreateMapToScreenFunc();

                var simplified = layer.Geometry.Simplify(this.LogisticGeometrySimplification, /*NearestGoogleZoomLevel,*/ toScreenMap, true);

                this.AddDrawingItem(simplified, $"{layer.LayerName} simplified by logistic-{this.LogisticGeometrySimplification.Title}-{this.NearestGoogleZoomLevel}");
            });

            var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Flash }.Data;

            return LegendCommand.Create(layer, action, markup, "ساده‌سازی لجستیک");
        });


        this.DrawingItemCommands.Add(layer =>
        {
            Action action = new Action(async () =>
            {
                var model = LoadTrainingDate(@"E:\University.Ph.D\4. Paper2\Model\LR4v23.json");

                var toScreenMap = this.CreateMapToScreenFunc();

                var simplified = layer.Geometry.Simplify(model, toScreenMap, true);

                this.AddDrawingItem(simplified, $"{layer.LayerName} simplified by LRSv4-{this.NearestGoogleZoomLevel}", VisualParameters.GetStroke("#DE36A1"));
            });

            var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Flash }.Data;

            return LegendCommand.Create(layer, action, markup, "LRSv4");
        });


        this.DrawingItemCommands.Add(layer =>
        {
            Action action = new Action(async () =>
            {
                this.IsBusy = true;

                var model = LoadTrainingDate(@"E:\University.Ph.D\4. Paper2\Model\LR7v23.json");

                var toScreenMap = this.CreateMapToScreenFunc();

                var simplified = layer.Geometry.Simplify(model, toScreenMap, true);

                this.AddDrawingItem(simplified, $"{layer.LayerName} simplified by LRSv7-{this.NearestGoogleZoomLevel}", VisualParameters.GetStroke("#08686E"));

                this.IsBusy = false;

            });

            var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Flash}.Data;

            return LegendCommand.Create(layer, action, markup, "LRSv7");
        });


        this.DrawingItemCommands.Add(layer =>
        {
            Action action = new Action(async () =>
            {
                this.IsBusy = true;

                var toScreenMap = this.CreateMapToScreenFunc();
                var parameter = new SimplificationParameters() { Retain3Points = true };

                double originalNumberOfPoints = layer.Geometry.TotalNumberOfPoints;

                var lrsv7Simplified = layer.Geometry.Simplify(GetLRSv7Model(), toScreenMap, true);
                var lrsv4lfSimplified = layer.Geometry.Simplify(GetLRSv4LfModel(), toScreenMap, true);
                var lrsv4Simplified = layer.Geometry.Simplify(GetLRSv4Model(), toScreenMap, true);
                var bopwSimplified = layer.Geometry.Simplify(SimplificationType.BeforeOpeningWindow, this.NearestGoogleZoomLevel, parameter);
                var nopwSimplified = layer.Geometry.Simplify(SimplificationType.NormalOpeningWindow, this.NearestGoogleZoomLevel, parameter);
                var rwSimplified = layer.Geometry.Simplify(SimplificationType.ReumannWitkam, this.NearestGoogleZoomLevel, parameter);
                var rdpSimplified = layer.Geometry.Simplify(SimplificationType.RamerDouglasPeucker, this.NearestGoogleZoomLevel, parameter);

                var lrsv7Compression = layer.Geometry.Compression(lrsv7Simplified) * 100.0;
                var lrsv4lfCompression = layer.Geometry.Compression(lrsv4lfSimplified) * 100.0;
                var lrsv4Compression = layer.Geometry.Compression(lrsv4Simplified) * 100.0;
                var bopwCompression = layer.Geometry.Compression(bopwSimplified) * 100.0;
                var nopwCompression = layer.Geometry.Compression(nopwSimplified) * 100.0;
                var rwCompression = layer.Geometry.Compression(rwSimplified) * 100.0;
                var rdpCompression = layer.Geometry.Compression(rdpSimplified) * 100.0;

                var gray = VisualParameters.GetStroke("#ADADAD", 2);

                //this.AddDrawingItem(lrsv7Simplified, $"{layer.LayerName}-LRSv7-{this.NearestGoogleZoomLevel}-{lrsv7Simplified.TotalNumberOfPoints}", VisualParameters.GetStroke("#38A6A5", 2));
                //this.AddDrawingItem(lrsv4Simplified, $"{layer.LayerName}-LRSv4-{this.NearestGoogleZoomLevel}-{lrsv4Simplified.TotalNumberOfPoints}", VisualParameters.GetStroke("#CC503E", 2));

                //this.AddDrawingItem(bopwSimplified, $"{layer.LayerName}-BOPW-{this.NearestGoogleZoomLevel}-{bopwSimplified.TotalNumberOfPoints}", VisualParameters.GetStroke("#EDAD08", 2));
                //this.AddDrawingItem(nopwSimplified, $"{layer.LayerName}-NOPW-{this.NearestGoogleZoomLevel}-{nopwSimplified.TotalNumberOfPoints}", VisualParameters.GetStroke("#5F4690", 2));
                //this.AddDrawingItem(rwSimplified, $"{layer.LayerName}-RW-{this.NearestGoogleZoomLevel}-{rwSimplified.TotalNumberOfPoints}", VisualParameters.GetStroke("#73AF48", 2));
                //this.AddDrawingItem(rdpSimplified, $"{layer.LayerName}-RDP-{this.NearestGoogleZoomLevel}-{rdpSimplified.TotalNumberOfPoints}", VisualParameters.GetStroke("#0F8554", 2));

                this.AddDrawingItem(lrsv7Simplified, $"{layer.LayerName}-LRSv7-{this.NearestGoogleZoomLevel}-#{lrsv7Simplified.TotalNumberOfPoints}-c{lrsv7Compression:N1}", VisualParameters.GetStroke("#ADADAD", 2));
                this.AddDrawingItem(lrsv4lfSimplified, $"{layer.LayerName}-LRSv4lf-{this.NearestGoogleZoomLevel}-#{lrsv4lfSimplified.TotalNumberOfPoints}-c{lrsv4lfCompression:N1}", VisualParameters.GetStroke("#ADADAD", 2));
                this.AddDrawingItem(lrsv4Simplified, $"{layer.LayerName}-LRSv4-{this.NearestGoogleZoomLevel}-#{lrsv4Simplified.TotalNumberOfPoints}-c{lrsv4Compression:N1}", VisualParameters.GetStroke("#ADADAD", 2));

                this.AddDrawingItem(bopwSimplified, $"{layer.LayerName}-BOPW-{this.NearestGoogleZoomLevel}-#{bopwSimplified.TotalNumberOfPoints}-c{bopwCompression:N1}", VisualParameters.GetStroke("#ADADAD", 2));
                this.AddDrawingItem(nopwSimplified, $"{layer.LayerName}-NOPW-{this.NearestGoogleZoomLevel}-#{nopwSimplified.TotalNumberOfPoints}-c{nopwCompression:N1}", VisualParameters.GetStroke("#ADADAD", 2));
                this.AddDrawingItem(rwSimplified, $"{layer.LayerName}-RW-{this.NearestGoogleZoomLevel}-#{rwSimplified.TotalNumberOfPoints}-c{rwCompression:N1}", VisualParameters.GetStroke("#ADADAD", 2));
                this.AddDrawingItem(rdpSimplified, $"{layer.LayerName}-RDP-{this.NearestGoogleZoomLevel}-#{rdpSimplified.TotalNumberOfPoints}-c{rdpCompression:N1}", VisualParameters.GetStroke("#ADADAD", 2));

                var lrModels = new List<LogisticSimplification<Point>> { GetLRSv4LfModel(), GetLRSv4Model(), GetLRSv7Model() };
                var methods = new List<SimplificationType>()
                {
                    SimplificationType.BeforeOpeningWindow, SimplificationType.NormalOpeningWindow, SimplificationType.ReumannWitkam, SimplificationType.RamerDouglasPeucker
                };


                await SimplificationHelper.Compare(layer.Geometry, 0, @"E:\University.Ph.D\4. Paper2\Outputs", this.NearestGoogleZoomLevel, layer.LayerName, methods, lrModels);

                this.IsBusy = false;

            });

            var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Flash }.Data;

            return LegendCommand.Create(layer, action, markup, "ALL");
        });

        this.DrawingItemCommands.Add(layer =>
        {
            Action action = new Action(async () =>
            {
                var mapCenter = layer.Geometry.GetBoundingBox().Center;

                var screenToMap = this.CreateScreenToMapFunc();
                var mapToScreen = this.CreateMapToScreenFunc();

                var size = 128.0;

                var screenCenter = mapToScreen(mapCenter);

                var topLeftScreen = new Point(screenCenter.X - size / 2.0, screenCenter.Y - size / 2.0);
                var bottomRightScreen = new Point(screenCenter.X + size / 2.0, screenCenter.Y + size / 2.0);

                var topLeftMap = screenToMap(topLeftScreen);
                var bottomRightMap = screenToMap(bottomRightScreen);

                var mapBoundingBox = BoundingBox.Create(topLeftMap, bottomRightMap);

                this.PrintArea = mapBoundingBox;

                await this.DialogService.ShowMessageAsync<MainWindow>("print area set!", "info", null);
                //await ExportMapAsPngAsync(null, mapBoundingBox);
            });

            var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Printer }.Data;

            return LegendCommand.Create(layer, action, markup, "Print 128");
        });
    }


     
    private SyntheticDataTrainingViewModel _syntheticDataTrainingData;
    public SyntheticDataTrainingViewModel SyntheticDataTrainingData
    {
        get { return _syntheticDataTrainingData; }
        set
        {
            _syntheticDataTrainingData = value;
            RaisePropertyChanged();
        }
    }
     
     
    public LogisticSimplification<Point> LogisticGeometrySimplification { get; set; }

    public LRSimplificationTrainingData<Point> TrainingData { get; set; } = new LRSimplificationTrainingData<Point>();


    private string _newWkt;
    public string NewWkt
    {
        get { return _newWkt; }
        set
        {
            _newWkt = value;
            RaisePropertyChanged();
        }
    }


    private LogisticSimplification<Point> LoadTrainingDate(string fileName)
    {
        this.TrainingData = LRSimplificationTrainingData<Point>.LoadFromJson(fileName);

        RaisePropertyChanged(nameof(TrainingData));

        var title = System.IO.Path.GetFileNameWithoutExtension(fileName);

        return LogisticSimplification<Point>.Create(this.TrainingData, title);
    }
     


    private RelayCommand _saveTrainingDataAsJsonCommand;
    public RelayCommand SaveTrainingDataAsJsonCommand
    {
        get
        {
            if (_saveTrainingDataAsJsonCommand == null)
            {
                _saveTrainingDataAsJsonCommand = new RelayCommand(async param =>
                {
                    // ذخیره‌سازی در فایل
                    var fileName = await this.DialogService.ShowSaveFileDialogAsync("*.json|*.json", this);

                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        return;
                    }

                    this.TrainingData.SaveAsJson(fileName);

                });
            }

            return _saveTrainingDataAsJsonCommand;
        }
    }


    private RelayCommand _saveTrainingDataAsCsvCommand;
    public RelayCommand SaveTrainingDataAsCsvCommand
    {
        get
        {
            if (_saveTrainingDataAsCsvCommand == null)
            {
                _saveTrainingDataAsCsvCommand = new RelayCommand(async param =>
                {
                    // ذخیره‌سازی در فایل
                    var fileName = await this.DialogService.ShowSaveFileDialogAsync("*.csv|*.csv", this);

                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        return;
                    }

                    this.TrainingData.SaveAsCsv(fileName);
                });
            }

            return _saveTrainingDataAsCsvCommand;
        }
    }


    private RelayCommand _loadTrainingDataCommand;
    public RelayCommand LoadTrainingDataCommand
    {
        get
        {
            if (_loadTrainingDataCommand == null)
            {
                _loadTrainingDataCommand = new RelayCommand(async param =>
                {
                    // ذخیره‌سازی در فایل
                    var fileName = await this.DialogService.ShowOpenFileDialogAsync("*.json|*.json", this);

                    if (string.IsNullOrEmpty(fileName))
                    {
                        return;
                    }

                    this.LogisticGeometrySimplification = LoadTrainingDate(fileName);
                    //this.TrainingData = LRSimplificationTrainingData<Point>.LoadFromJson(fileName);

                    //RaisePropertyChanged(nameof(TrainingData));

                    //var title = System.IO.Path.GetFileNameWithoutExtension(fileName);

                    //LogisticGeometrySimplification = LogisticSimplification<Point>.Create(this.TrainingData, title);
                });
            }

            return _loadTrainingDataCommand;
        }
    }


    private RelayCommand _clearTrainingDataCommand;
    public RelayCommand ClearTrainingDataCommand
    {
        get
        {
            if (_clearTrainingDataCommand == null)
            {
                _clearTrainingDataCommand = new RelayCommand(param =>
                {
                    this.TrainingData.Records = new List<LRSimplificationParameters<Point>>();

                    this.LogisticGeometrySimplification = null;
                });
            }

            return _clearTrainingDataCommand;
        }
    }


    private RelayCommand _buildModelCommand;
    public RelayCommand BuildModelCommand
    {
        get
        {
            if (_buildModelCommand == null)
            {
                _buildModelCommand = new RelayCommand(param =>
               {
                   LogisticGeometrySimplification = LogisticSimplification<Point>.Create(this.TrainingData, "No title");
               });
            }

            return _buildModelCommand;
        }
    }

    private RelayCommand _analyzeCommand;

    public RelayCommand AnalyzeCommand
    {
        get
        {
            if (_analyzeCommand == null)
            {
                _analyzeCommand = new RelayCommand(async param =>
                {
                    await Common.LRHelper.GeneralTest();
                });
            }

            return _analyzeCommand;
        }
    }


    internal void AddWkt()
    {
        if (string.IsNullOrEmpty(this.NewWkt))
        {
            return;
        }

        var geometry = SqlGeometry.Parse(new System.Data.SqlTypes.SqlString(this.NewWkt)).MakeValid();

        AddDrawingItem(geometry.AsGeometry());
        //AddGeometry(geometry, $"Geometry # {this.Geometries.Count}");
    }

    //public override void Initialize(
    //    IDialogService dialogService,
    //    Action<Point> requestShowGoToView,
    //    Action<ILayer> requestShowSymbologyView)
    //{
    //    base.Initialize(dialogService, requestShowGoToView, requestShowSymbologyView);

    //    if (this.BaseMapSettings is not null)
    //    {
    //        this.BaseMapSettings.OnBaseMapUrlChanged -= BaseMapSettings_OnBaseMapUrlChanged;
    //        this.BaseMapSettings.OnBaseMapUrlChanged += BaseMapSettings_OnBaseMapUrlChanged;

    //        this.BaseMapSettings.PropertyChanged -= BaseMapSettings_PropertyChanged;
    //        this.BaseMapSettings.PropertyChanged += BaseMapSettings_PropertyChanged;
    //    }

    //    if (this.MapSettings is not null)
    //    {
    //        this.MapSettings.PropertyChanged -= MapSettings_PropertyChanged;
    //        this.MapSettings.PropertyChanged += MapSettings_PropertyChanged;
    //    }

    //    if (this.GeneralSettings is not null)
    //    {
    //        this.GeneralSettings.PropertyChanged -= GeneralSettings_PropertyChanged;
    //        this.GeneralSettings.PropertyChanged += GeneralSettings_PropertyChanged;
    //    }
    //}

    //private void BaseMapSettings_OnBaseMapUrlChanged(object? sender, EventArgs e)
    //{
    //}

    //private void BaseMapSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName is null)
    //        return;

    //    if (e.PropertyName is not nameof(BaseMapSettingsModel.BaseMapOpacity)
    //        and not nameof(BaseMapSettingsModel.SelectedTileMapAccessMode)
    //        and not nameof(BaseMapSettingsModel.InitialBaseMap))
    //        return;

    //    var data = this.BaseMapSettings?.GetData();
    //    if (data is not null)
    //        SettingsHelper.SaveBaseMapSettings(data);
    //}

    //private void MapSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName is null)
    //        return;

    //    var data = this.MapSettings?.GetData();
    //    if (data is not null)
    //        SettingsHelper.SaveMapSettings(data);
    //}

    //private void GeneralSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName is null)
    //        return;

    //    if (e.PropertyName == nameof(GeneralSettingsModel.MahAppsTheme))
    //    {
    //        var theme = this.GeneralSettings?.MahAppsTheme;
    //        if (theme.HasValue)
    //            ThemeHelper.ApplyTheme(theme.Value);
    //    }

    //    if (e.PropertyName == nameof(GeneralSettingsModel.CurrentLanguage))
    //    {
    //        var culture = LanguageItem.Create(this.GeneralSettings?.CurrentLanguage ?? LanguageType.en_US).GetCultureInfo();
    //        try
    //        {
    //            LocalizationManager.Instance.SetCulture(culture);
    //        }
    //        catch (CultureNotFoundException) { }
    //    }

    //    var data = this.GeneralSettings?.GetData();
    //    if (data is not null)
    //        SettingsHelper.SaveGeneralSettings(data);
    //}
}

