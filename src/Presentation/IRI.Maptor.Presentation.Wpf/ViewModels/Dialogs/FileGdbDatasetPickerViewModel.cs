using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Models.FileGdb;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

/// <summary>
/// The file geodatabase picker: which datasets to add, grouped by feature dataset, with the reason for the ones that
/// cannot be added, and one coordinate-system choice for the ticked ones the geodatabase gives none for.
/// </summary>
public class FileGdbDatasetPickerViewModel : DialogViewModelBase
{
    /// <summary>Up to this many datasets that can be added are all ticked at first; above it, none.</summary>
    public const int PreselectLimit = 20;

    private bool _updatingSelection;

    /// <param name="geodatabaseName">The folder's name, shown in the header.</param>
    /// <param name="items">One row per dataset (<see cref="FileGdbDatasetPickerItem.Create"/>).</param>
    public FileGdbDatasetPickerViewModel(string geodatabaseName, IReadOnlyList<FileGdbDatasetPickerItem> items)
    {
        GeodatabaseName = geodatabaseName;

        // the root first, then the feature datasets by name; the catalog order within each
        Items = items.Select((item, index) => (item, index))
                     .OrderBy(x => x.item.FeatureDatasetName is null ? 0 : 1)
                     .ThenBy(x => x.item.FeatureDatasetName, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(x => x.index)
                     .Select(x => x.item)
                     .ToList();

        var available = Items.Where(i => i.IsAvailable).ToList();

        if (available.Count <= PreselectLimit)
            available.ForEach(i => i.IsSelected = true);

        foreach (var item in Items)
            item.PropertyChanged += Item_PropertyChanged;

        View = CollectionViewSource.GetDefaultView(Items);
        View.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FileGdbDatasetPickerItem.GroupName)));

        SrsChoice = new SrsChoiceViewModel();

        AddCommand = new RelayCommand(_ => Add(), _ => SelectedCount > 0);
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    public string Title => LocalizationManager.Instance["dialog_fileGdbPicker_title"];

    public string GeodatabaseName { get; }

    public IReadOnlyList<FileGdbDatasetPickerItem> Items { get; }

    public ICollectionView View { get; }

    public SrsChoiceViewModel SrsChoice { get; }

    public int SelectedCount => Items.Count(i => i.IsSelected);

    public int AvailableCount => Items.Count(i => i.IsAvailable);

    public string SelectionText => string.Format(LocalizationManager.Instance.CurrentCulture, LocalizationManager.Instance["dialog_fileGdbPicker_selectedCount"], SelectedCount, AvailableCount);

    /// <summary>The coordinate-system choice is shown, and used, when a ticked dataset needs it.</summary>
    public bool ShowsSrsChoice => Items.Any(i => i.IsSelected && i.NeedsSrs);

    /// <summary>Every dataset that can be added is ticked (true), none (false), or some (null); setting it ticks all or none.</summary>
    public bool? AreAllSelected
    {
        get
        {
            var selected = SelectedCount;

            return selected == 0 ? false : selected == AvailableCount ? true : null;
        }
        set
        {
            // a tri-state check box clicked while "some" goes to null: tick all then
            var select = value != false;

            _updatingSelection = true;

            try
            {
                foreach (var item in Items)
                    item.IsSelected = select;
            }
            finally
            {
                _updatingSelection = false;
            }

            OnSelectionChanged();
        }
    }

    public string? ValidationMessage => SelectedCount == 0 ? LocalizationManager.Instance["dialog_fileGdbPicker_validationNone"] : null;

    /// <summary>The footer's line: the validation message, else how many are ticked.</summary>
    public string StatusText => ValidationMessage ?? SelectionText;

    public RelayCommand AddCommand { get; }

    public RelayCommand CancelCommand { get; }

    /// <summary>The ticked datasets, and the SRID for those that need one (null when none does).</summary>
    public (IReadOnlyList<FileGdbDataset> Datasets, int? FallbackSrid)? Result { get; private set; }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileGdbDatasetPickerItem.IsSelected) && !_updatingSelection)
            OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        RaisePropertyChanged(nameof(SelectedCount));
        RaisePropertyChanged(nameof(SelectionText));
        RaisePropertyChanged(nameof(AreAllSelected));
        RaisePropertyChanged(nameof(ShowsSrsChoice));
        RaisePropertyChanged(nameof(ValidationMessage));
        RaisePropertyChanged(nameof(StatusText));
        AddCommand.RaiseCanExecuteChanged();
    }

    private void Add()
    {
        if (SelectedCount == 0)
            return;

        Result = (Items.Where(i => i.IsSelected).Select(i => i.Dataset).ToList(), ShowsSrsChoice ? SrsChoice.EffectiveSrid : null);

        DialogResult = true;
        RequestClose?.Invoke();
    }

    private void Cancel()
    {
        Result = null;
        DialogResult = null;
        RequestClose?.Invoke();
    }
}
