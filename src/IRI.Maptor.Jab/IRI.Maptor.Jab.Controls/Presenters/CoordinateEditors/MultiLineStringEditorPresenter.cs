using System;
using System.Collections.ObjectModel;
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
            _parts = value ?? new ObservableCollection<LineStringEditorPresenter>();
            RaisePropertyChanged();
        }
    }

    public event Action<ObservableCollection<LineStringEditorPresenter>>? GeometryChanged;

    private RelayCommand _addPartCommand;
    public RelayCommand AddPartCommand =>
        _addPartCommand ??= new RelayCommand(param =>
        {
            var newPart = new LineStringEditorPresenter();
            Parts.Add(newPart);
            GeometryChanged?.Invoke(Parts);
        });

    private RelayCommand _deletePartCommand;
    public RelayCommand DeletePartCommand =>
        _deletePartCommand ??= new RelayCommand(param =>
        {
            if (param is LineStringEditorPresenter part)
            {
                Parts.Remove(part);
                GeometryChanged?.Invoke(Parts);
            }
        });

    public MultiLineStringEditorPresenter()
    {
        Parts = new ObservableCollection<LineStringEditorPresenter>();
        Parts.CollectionChanged += (s, e) => GeometryChanged?.Invoke(Parts);
    }
}

