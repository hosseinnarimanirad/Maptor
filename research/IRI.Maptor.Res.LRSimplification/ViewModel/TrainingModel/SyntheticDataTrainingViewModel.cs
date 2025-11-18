using System.Windows.Media;
using System.Collections.ObjectModel;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.MultiSelectItem.ViewModel;

using Sb = IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Res.LRSimplification.ViewModel.TrainingModel;

public class SyntheticDataTrainingViewModel : Notifier
{
    ApplicationPresenter _map;

    const string _syntheticLayerName = "#syntheticLayer";

    public SyntheticDataTrainingViewModel(ApplicationPresenter map)
    {
        _map = map;

        _currentItem = CreateNew("LINESTRING(100 100, 200 101, 300 100)", true);

        var list = new List<LRSimplificationFeatures>()
        {
            LRSimplificationFeatures.Area,
            LRSimplificationFeatures.BaseLength,
            LRSimplificationFeatures.CosineOfAngle,
            LRSimplificationFeatures.DistanceToNext,
            LRSimplificationFeatures.DistanceToPrevious,
            LRSimplificationFeatures.SquareCosineOfAngle,
            LRSimplificationFeatures.VerticalDistance,
            LRSimplificationFeatures.dX12,
            LRSimplificationFeatures.dX13,
            LRSimplificationFeatures.dX23,
            LRSimplificationFeatures.dY12,
            LRSimplificationFeatures.dY13,
            LRSimplificationFeatures.dY23

        }.Select(l => new LRSimplificationFeature() { Value = l }).ToList();

        this.SelectedFeatures = new MultiSelectItemViewModel<LRSimplificationFeature>(list, lrsf => lrsf.Value.GetName()) { DisplayMemberPath = "Value" };

        foreach (var item in list)
            this.SelectedFeatures.Add(item);
    }
     
    private SyntheticDataItemViewModel _currentItem;//= new SyntheticDataItemViewModel(() => PreviewSampleData()) { OriginalLineString = "LINESTRING(100 100, 200 101, 300 100)" };
    public SyntheticDataItemViewModel CurrentItem
    {
        get { return _currentItem; }
        set
        {
            _currentItem = value;
            RaisePropertyChanged();

            this.RetainCurrentItemPoints = value.IsRetained;
        }
    }


    private bool _retainCurrentItemPoints;
    public bool RetainCurrentItemPoints
    {
        get { return _retainCurrentItemPoints; }
        set
        {
            _retainCurrentItemPoints = value;
            RaisePropertyChanged();

            if (this.CurrentItem == null)
                return;

            this.CurrentItem.UpdateSimplified(value);
             
            BuildCurrentItem();
        }
    }


    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get { return this.Items?.All(i => i.IsChecked) == true; }
        set
        {
            _isAllSelected = value;

            for (int i = 0; i < Items.Count; i++)
                this.Items[i].IsChecked = value;

            RaisePropertyChanged();
        }
    }


    private MultiSelectItemViewModel<LRSimplificationFeature> _selectedFeatures;
    public MultiSelectItemViewModel<LRSimplificationFeature> SelectedFeatures
    {
        get { return _selectedFeatures; }
        set
        {
            _selectedFeatures = value;
            RaisePropertyChanged();
        }
    }

     
    private ObservableCollection<SyntheticDataItemViewModel> _items = new ObservableCollection<SyntheticDataItemViewModel>();
    public ObservableCollection<SyntheticDataItemViewModel> Items
    {
        get { return _items; }
        set
        {
            _items = value;
            RaisePropertyChanged();
        }
    }


    private (Geometry<Point> original, Geometry<Point> simplified) BuildCurrentItem()
    {
        if (string.IsNullOrWhiteSpace(CurrentItem?.OriginalLineString) ||
            string.IsNullOrWhiteSpace(CurrentItem?.SimplifiedLineString))
            return (null, null);

        //wkt in screen 
        var originalScreenGeometry = Geometry<Point>.FromWkt(CurrentItem?.OriginalLineString, 0) as Geometry<Point>;
        var simplifiedScreenGeometry = Geometry<Point>.FromWkt(CurrentItem?.SimplifiedLineString, 0) as Geometry<Point>;

        var originalGeometry = _map.TransformScreenGeometryToWebMercatorGeometry(originalScreenGeometry);
        var simplifiedGeometry = _map.TransformScreenGeometryToWebMercatorGeometry(simplifiedScreenGeometry);

        ////var simplifiedGeometry = string.IsNullOrWhiteSpace(CurrentItem?.SimplifiedLineString) ?
        ////    Geometry<Point>.CreatePointOrLineString(new List<Point>() { originalGeometry.Points.First(), originalGeometry.Points.Last() }, SridHelper.WebMercator)
        ////    : _map.TransformScreenGeometryToWebMercatorGeometry(Geometry<Point>.Parse(CurrentItem?.SimplifiedLineString, 0));

        // گرفتن جئومتری‌ها و زووم لول 
        var g1 = originalGeometry.GetLastPart();
        var g2 = simplifiedGeometry.GetLastPart();

        ////var mapScale = WebMercatorUtility.CalculateMapScale(vm.ZoomLevel, 35);
        //// انجام محاسبات
        ////this.LogisticGeometrySimplification = LogisticGeometrySimplification.Create(g1, g2, vm.ZoomLevel);

        //var toScreenMap = this._map.RequestGetToScreenMap?.Invoke();
        var toScreenMap = this._map.CreateMapToScreenFunc();

        var isRingMode = originalGeometry.IsRingBase();

        var trainingData = LRSimplificationTrainingData<Sb.Point>.Create(
            originalScreenGeometry.GetLastPart(),
            simplifiedScreenGeometry.GetLastPart(),
            isRingMode,
            this.GetSelectedFeatures());

        var trainingData2 = LRSimplificationTrainingData<Sb.Point>.Create(g1, g2, isRingMode, this.GetSelectedFeatures(), toScreenMap);

        #region test
        var first = trainingData.Records.First();
        var second = trainingData2.Records.First();

        if (first.IsRetained != second.IsRetained)
            throw new NotImplementedException();
        if (Math.Abs(first.Area!.Value - second.Area!.Value) > 1E-6)
            throw new NotImplementedException();
        if (Math.Abs(first.DistanceToNext!.Value - second.DistanceToNext!.Value) > 1E-6)
            throw new NotImplementedException();
        if (Math.Abs(first.DistanceToPrevious!.Value - second.DistanceToPrevious!.Value) > 1E-6)
            throw new NotImplementedException();
        if (Math.Abs(first.VerticalDistance!.Value - second.VerticalDistance!.Value) > 1E-6)
            throw new NotImplementedException();
        if (Math.Abs(first.SquareCosineOfAngle!.Value - second.SquareCosineOfAngle!.Value) > 1E-6)
            throw new NotImplementedException();
        if (Math.Abs(first.CosineOfAngle!.Value - second.CosineOfAngle!.Value) > 1E-6)
            throw new NotImplementedException();
        if (Math.Abs(first.BaseLength!.Value - second.BaseLength!.Value) > 1E-6)
            throw new NotImplementedException();

        #endregion

        CurrentItem.Parameters = trainingData.Records.First();

        return (originalGeometry, simplifiedGeometry);
    }

    private SyntheticDataItemViewModel CreateNew(string? originalLineStringWkt = null, bool enablePreview = true)
    {
        return new SyntheticDataItemViewModel(PreviewSampleData, enablePreview) { OriginalLineString = originalLineStringWkt };
    }

    private void PreviewSampleData()
    {
        var result = BuildCurrentItem();

        if (result.original == null || result.simplified == null)
            return;

        _map.RemoveAllDrawingItems();

        var visualParameters1 = VisualParameters.GetStroke(Colors.Red, .7, .9);
        var visualParameters2 = VisualParameters.GetStroke(Colors.Blue, .7, .9);
         
        _map.AddDrawingItem(result.original, CurrentItem.Title, visualParameters1);
        _map.AddDrawingItem(result.simplified, $"{CurrentItem.Title}-simplified", visualParameters2);
    }

    private void ClearAllSampleDate()
    {
        this.Items.Clear();
    }

    private void Add()
    {
        if (this.CurrentItem == null)
            return;

        if (!this.Items.Contains(CurrentItem))
        {
            this.Items.Add(CurrentItem);
        }

        CurrentItem = CreateNew();
    }

    private void Remove(SyntheticDataItemViewModel item)
    {
        if (item == null)
            return;

        if (this.Items.Contains(item))
        {
            this.Items.Remove(item);
        }
    }

    private async Task Save()
    {
        if (this.Items?.Any() != true)
            return;

        var fileName = await _map.DialogService.ShowSaveFileDialogAsync("*.sdjson|*.sdjson", null, $"{DateTime.Now.ToLongTimeString().Replace(':', '-')}.sdjson");

        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var items = this.Items.Select(i => new SyntheticDataItem()
        {
            Note = i.Note,
            OriginalLineString = i.OriginalLineString,
            SimplifiedLineString = i.SimplifiedLineString,
            Title = i.Title,
        }).ToList();

        SyntheticDataFile file = new SyntheticDataFile() { LineSamples = items, Features = GetSelectedFeatures() };

        file.Save(fileName);
         
    }

    private async Task Load()
    {
        try
        {
            var fileName = await _map.DialogService.ShowOpenFileDialogAsync("*.sdjson|*.sdjson|*.json|*.json", null);

            if (!System.IO.File.Exists(fileName))
                return;

            var projfile = SyntheticDataFile.Load(fileName);

            if (projfile == null)
                return;

            foreach (var item in projfile.LineSamples)
            {
                var newItem = CreateNew(null, false);

                newItem.Note = item.Note;
                newItem.OriginalLineString = item.OriginalLineString;
                newItem.SimplifiedLineString = item.SimplifiedLineString;
                newItem.Title = item.Title;

                newItem.EnablePreview = true;

                this.Items.Add(newItem); 
            }

            SetSelectedFeatures(projfile.Features);
        }
        catch (Exception ex)
        {
            await this._map.DialogService.ShowMessageAsync<MainWindow>(ex.Message, "Error", null);
        }
    }

    private void SetSelectedFeatures(List<LRSimplificationFeatures> features)
    {
        SelectedFeatures.SelectedItems.Clear();

        foreach (var item in SelectedFeatures.AllItems)
            if (features.Contains(item.Value))
                SelectedFeatures.Add(item);
    }

    public List<LRSimplificationFeatures> GetSelectedFeatures()
    {
        if (SelectedFeatures.SelectedItems.IsNullOrEmpty())
            return new List<LRSimplificationFeatures>();

        return SelectedFeatures.SelectedItems.Select(s => s.Value.Value).ToList();/*.ConvertToFlag();*/
    }

    private List<LRSimplificationParameters<Sb.Point>> GenerateTrainingData(SyntheticDataItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(item?.OriginalLineString) ||
           string.IsNullOrWhiteSpace(item?.SimplifiedLineString))
            return new List<LRSimplificationParameters<Sb.Point>>();

        //wkt in screen 
        var originalScreenGeometry = Geometry<Point>.FromWkt(item?.OriginalLineString, 0) as Geometry<Point>;
        var simplifiedScreenGeometry = Geometry<Point>.FromWkt(item?.SimplifiedLineString, 0) as Geometry<Point>;

        var isRingMode = originalScreenGeometry.IsRingBase();

        var trainingData = LRSimplificationTrainingData<Sb.Point>.Create(
            originalScreenGeometry.GetLastPart(),
            simplifiedScreenGeometry.GetLastPart(),
            isRingMode,
            this.GetSelectedFeatures());

        // 1400.06.06
        // enable choosing model features dynamically
        var flag = GetSelectedFeatures();

        for (int i = 0; i < trainingData.Records.Count; i++)
        {
            trainingData.Records[i].Features = flag;
        }

        return trainingData.Records;
    }



    private async void BuildLogisticRegressionModel()
    {
        var items = this.Items?.Where(i => i.IsChecked);

        if (items.IsNullOrEmpty())
        {
            await this._map.DialogService.ShowMessageAsync<MainWindow>("No item selected!", "error", null);

            return;
        }

        List<LRSimplificationParameters<Point>> records = new List<LRSimplificationParameters<Point>>();

        foreach (var item in items)
        {
            records.AddRange(GenerateTrainingData(item));
        }

        _map.TrainingData = new LRSimplificationTrainingData<Sb.Point>(records);

        _map.LogisticGeometrySimplification = LogisticSimplification<Sb.Point>.Create(_map.TrainingData, "No Title 3");

        await this._map.DialogService.ShowMessageAsync<MainWindow>("Model Built", "Info", null);
    }

    #region Commands

    private RelayCommand _clearAllSampleDateCommand;

    public RelayCommand ClearAllSampleDateCommand
    {
        get
        {
            if (_clearAllSampleDateCommand == null)
            {
                _clearAllSampleDateCommand = new RelayCommand(param =>
                {
                    ClearAllSampleDate();
                });
            }

            return _clearAllSampleDateCommand;
        }
    }


    private RelayCommand _newSampleCommand;
    public RelayCommand NewSampleCommand
    {
        get
        {
            if (_newSampleCommand == null)
            {
                _newSampleCommand = new RelayCommand(param =>
                {
                    //_map.ClearLayer(_syntheticLayerName, true, true);
                    this.CurrentItem = CreateNew();
                });
            }

            return _newSampleCommand;
        }
    }


    private RelayCommand _previewSampleDataCommand;
    public RelayCommand PreviewSampleDataCommand
    {
        get
        {
            if (_previewSampleDataCommand == null)
            {
                _previewSampleDataCommand = new RelayCommand(param =>
                {
                    PreviewSampleData();
                });
            }

            return _previewSampleDataCommand;
        }
    }


    private RelayCommand _addSampleDataCommand;
    public RelayCommand AddSampleDataCommand
    {
        get
        {
            if (_addSampleDataCommand == null)
            {
                _addSampleDataCommand = new RelayCommand(param =>
                {
                    Add();
                });
            }

            return _addSampleDataCommand;
        }
    }



    private RelayCommand _saveSampleDataCommand;
    public RelayCommand SaveSampleDataCommand
    {
        get
        {
            if (_saveSampleDataCommand == null)
            {
                _saveSampleDataCommand = new RelayCommand(async param =>
                {
                    await Save();
                });
            }

            return _saveSampleDataCommand;
        }
    }


    private RelayCommand _loadSampleDataCommand;
    public RelayCommand LoadSampleDataCommand
    {
        get
        {
            if (_loadSampleDataCommand == null)
            {
                _loadSampleDataCommand = new RelayCommand(async param =>
                {
                    await Load();
                });
            }

            return _loadSampleDataCommand;
        }
    }


    private RelayCommand _buildModelWithSampleDataCommand;
    public RelayCommand BuildModelWithSampleDataCommand
    {
        get
        {
            if (_buildModelWithSampleDataCommand == null)
            {
                _buildModelWithSampleDataCommand = new RelayCommand(param =>
                {
                    BuildLogisticRegressionModel();
                });
            }

            return _buildModelWithSampleDataCommand;
        }
    }



    private RelayCommand _previewItemCommand;

    public RelayCommand PreviewItemCommand
    {
        get
        {
            if (_previewItemCommand == null)
            {
                _previewItemCommand = new RelayCommand(param =>
                {
                    this.CurrentItem = param as SyntheticDataItemViewModel;

                    //PreviewSampleData();
                });
            }

            return _previewItemCommand;
        }
    }


    private RelayCommand _removeItemCommand;
    public RelayCommand RemoveItemCommand
    {
        get
        {
            if (_removeItemCommand == null)
            {
                _removeItemCommand = new RelayCommand(param =>
                {
                    Remove(param as SyntheticDataItemViewModel);
                });
            }

            return _removeItemCommand;
        }
    }

    #endregion
}