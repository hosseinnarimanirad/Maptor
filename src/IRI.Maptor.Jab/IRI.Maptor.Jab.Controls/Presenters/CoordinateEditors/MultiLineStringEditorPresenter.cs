using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;

namespace IRI.Maptor.Jab.Controls.Presenters.CoordinateEditors;

public class MultiLineStringEditorPresenter : Notifier
{
    private ObservableCollection<LineStringEditorPresenter> _parts = new ObservableCollection<LineStringEditorPresenter>();
    public ObservableCollection<LineStringEditorPresenter> Parts
    {
        get => _parts;
        set
        {
            if (_parts != null)
            {
                _parts.CollectionChanged -= Parts_CollectionChanged;
            }
            _parts = value ?? new ObservableCollection<LineStringEditorPresenter>();
            if (_parts != null)
            {
                _parts.CollectionChanged += Parts_CollectionChanged;
            }
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(TotalPartCount));
            RaisePropertyChanged(nameof(CurrentPart));
            RaisePropertyChanged(nameof(CurrentPartNumber));
            RaisePropertyChanged(nameof(IsNextPartAvailable));
            RaisePropertyChanged(nameof(IsPreviousPartAvailable));
            AdjustCurrentPartIndex();
        }
    }

    private int _currentPartIndex = 0;
    public int CurrentPartIndex
    {
        get => _currentPartIndex;
        set
        {
            if (value < 0 || (Parts != null && Parts.Count > 0 && value >= Parts.Count))
                return;
            
            _currentPartIndex = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentPart));
            RaisePropertyChanged(nameof(CurrentPartNumber));
            RaisePropertyChanged(nameof(IsNextPartAvailable));
            RaisePropertyChanged(nameof(IsPreviousPartAvailable));
        }
    }

    public LineStringEditorPresenter? CurrentPart
    {
        get
        {
            if (Parts == null || Parts.Count == 0 || CurrentPartIndex < 0 || CurrentPartIndex >= Parts.Count)
                return null;
            return Parts[CurrentPartIndex];
        }
    }

    public int CurrentPartNumber => CurrentPartIndex + 1;

    public int TotalPartCount => Parts?.Count ?? 0;

    public bool IsNextPartAvailable => Parts != null && Parts.Count > 0;

    public bool IsPreviousPartAvailable => Parts != null && Parts.Count > 0;

    private void Parts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GeometryChanged?.Invoke(Parts);
        RaisePropertyChanged(nameof(TotalPartCount));
        RaisePropertyChanged(nameof(CurrentPart));
        RaisePropertyChanged(nameof(CurrentPartNumber));
        RaisePropertyChanged(nameof(IsNextPartAvailable));
        RaisePropertyChanged(nameof(IsPreviousPartAvailable));
        AdjustCurrentPartIndex();
    }

    private void AdjustCurrentPartIndex()
    {
        if (Parts == null || Parts.Count == 0)
        {
            if (_currentPartIndex != 0)
            {
                _currentPartIndex = 0;
                RaisePropertyChanged(nameof(CurrentPartIndex));
            }
        }
        else if (_currentPartIndex >= Parts.Count)
        {
            _currentPartIndex = Parts.Count - 1;
            RaisePropertyChanged(nameof(CurrentPartIndex));
        }
        RaisePropertyChanged(nameof(CurrentPart));
    }

    public event Action<ObservableCollection<LineStringEditorPresenter>>? GeometryChanged;

    public event Action<LineStringEditorPresenter>? RequestZoomToPart;

    private RelayCommand _addPartCommand;
    public RelayCommand AddPartCommand =>
        _addPartCommand ??= new RelayCommand(param =>
        {
            var newPart = new LineStringEditorPresenter();
            Parts.Add(newPart);
            CurrentPartIndex = Parts.Count - 1;
            GeometryChanged?.Invoke(Parts);
        });

    private RelayCommand _deletePartCommand;
    public RelayCommand DeletePartCommand =>
        _deletePartCommand ??= new RelayCommand(param =>
        {
            if (param is LineStringEditorPresenter part)
            {
                int indexToDelete = Parts.IndexOf(part);
                if (indexToDelete < 0)
                    return;

                bool wasCurrentPart = indexToDelete == CurrentPartIndex;
                bool wasLastPart = indexToDelete == Parts.Count - 1;

                Parts.Remove(part);
                
                if (Parts.Count == 0)
                {
                    CurrentPartIndex = 0;
                }
                else if (wasCurrentPart)
                {
                    if (wasLastPart && CurrentPartIndex > 0)
                    {
                        CurrentPartIndex = CurrentPartIndex - 1;
                    }
                    else if (!wasLastPart)
                    {
                        CurrentPartIndex = CurrentPartIndex;
                    }
                }
                else if (indexToDelete < CurrentPartIndex)
                {
                    CurrentPartIndex = CurrentPartIndex - 1;
                }

                GeometryChanged?.Invoke(Parts);
            }
        });

    private RelayCommand _goToNextPartCommand;
    public RelayCommand GoToNextPartCommand =>
        _goToNextPartCommand ??= new RelayCommand(param =>
        {
            if (Parts == null || Parts.Count == 0)
                return;

            if (CurrentPartIndex < Parts.Count - 1)
            {
                CurrentPartIndex++;
            }
            else
            {
                CurrentPartIndex = 0;
            }
        }, param => IsNextPartAvailable);

    private RelayCommand _goToPreviousPartCommand;
    public RelayCommand GoToPreviousPartCommand =>
        _goToPreviousPartCommand ??= new RelayCommand(param =>
        {
            if (Parts == null || Parts.Count == 0)
                return;

            if (CurrentPartIndex > 0)
            {
                CurrentPartIndex--;
            }
            else
            {
                CurrentPartIndex = Parts.Count - 1;
            }
        }, param => IsPreviousPartAvailable);

    private RelayCommand _zoomToCurrentPartCommand;
    public RelayCommand ZoomToCurrentPartCommand =>
        _zoomToCurrentPartCommand ??= new RelayCommand(param =>
        {
            if (CurrentPart != null)
            {
                RequestZoomToPart?.Invoke(CurrentPart);
            }
        }, param => CurrentPart != null);

    public MultiLineStringEditorPresenter()
    {
        Parts = new ObservableCollection<LineStringEditorPresenter>();
        Parts.CollectionChanged += Parts_CollectionChanged;
    }
}




