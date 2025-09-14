using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Res.LRSimplification.ViewModel.TrainingModel;

public class SyntheticDataItemViewModel : Notifier
{
    public Action RequestPreview;

    // screen
    private string _originalLineString = string.Empty;
    public string OriginalLineString
    {
        get { return _originalLineString; }
        set
        {
            var isRetained = IsRetained;

            _originalLineString = value;
            RaisePropertyChanged();
            UpdateSimplified(isRetained);
        }
    }

    // screen
    private string _simplifiedLineString = string.Empty;
    public string SimplifiedLineString
    {
        get { return _simplifiedLineString; }
        set
        {
            _simplifiedLineString = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsRetained));
        }
    }


    private LRSimplificationParameters<Point> _parameters = new LRSimplificationParameters<Point>();
    public LRSimplificationParameters<Point> Parameters
    {
        get { return _parameters; }
        set
        {
            _parameters = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentLRParameters));
            RaisePropertyChanged(nameof(IsRetained));
        }
    }


    public string CurrentLRParameters => Parameters?.ToString() ?? string.Empty;

    public bool IsRetained => !string.IsNullOrWhiteSpace(OriginalLineString) && OriginalLineString == SimplifiedLineString;


    private string _title = string.Empty;
    public string Title
    {
        get { return _title; }
        set
        {
            _title = value;
            RaisePropertyChanged();
        }
    }


    private string _note = string.Empty;
    public string Note
    {
        get { return _note; }
        set
        {
            _note = value;
            RaisePropertyChanged();
        }
    }


    private bool _isChecked;
    public bool IsChecked
    {
        get { return _isChecked; }
        set
        {
            _isChecked = value;
            RaisePropertyChanged();
        }
    }

    public bool EnablePreview { get; set; } = true;

    public SyntheticDataItemViewModel(Action requestPreview, bool enablePreview)
    {
        this.RequestPreview = requestPreview;
        this.EnablePreview = enablePreview;
    }

    public void UpdateSimplified(bool isRetained)
    {
        if (string.IsNullOrWhiteSpace(this.OriginalLineString))
            return;

        if (isRetained)
        {
            SimplifiedLineString = this.OriginalLineString;
        }
        else
        {
            try
            {
                var original = Geometry<Point>.FromWkt(OriginalLineString, 0);

                var simplified = Geometry<Point>.CreatePointOrLineString(new List<Point>() { original.Points.First(), original.Points.Last() }, original.Srid);

                SimplifiedLineString = simplified.AsWkt();
            }
            catch { }
        }

        if (EnablePreview)
            this.RequestPreview?.Invoke();
    }
}
