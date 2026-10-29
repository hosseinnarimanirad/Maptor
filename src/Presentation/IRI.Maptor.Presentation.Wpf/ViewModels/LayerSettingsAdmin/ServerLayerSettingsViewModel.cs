using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettingsAdmin;
using IRI.Maptor.Presentation.Wpf.Services;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettingsAdmin;

/// <summary>Where a dragged row lands relative to the row under the pointer.</summary>
public enum ServerLayerDropPosition
{
    Before,
    After,

    /// <summary>Into a group, at its top.</summary>
    Into,
}

/// <summary>One entry of the "Move to" menu: a group, or the top level when <see cref="Group"/> is null.</summary>
public record ServerLayerMoveTarget(string Title, ServerLayerSettingItem? Group, bool IsCurrent);

/// <summary>
/// The layer-settings admin window: the server's TOC groups and layers as a tree, the selected row's
/// editor, and Save for every change. Value edits and moves (order, group) stay local until Save, which
/// sends the moves first (one order request) and then each edited row; every row keeps its own outcome.
/// Creating and deleting a group go to the server at once: a new group needs its id before anything can
/// be placed in it, and only an empty group can be deleted.
/// The host reacts to <see cref="Saved"/> (reload the map's server layers).
/// </summary>
public class ServerLayerSettingsViewModel : Notifier
{
    private static LocalizationManager Localization => LocalizationManager.Instance;

    private readonly ILayerSettingsAdminService _service;

    private readonly List<ServerLayerSettingItem> _items = new();

    public ServerLayerSettingsViewModel(ILayerSettingsAdminService service)
    {
        _service = service;

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave);
        RevertCommand = new RelayCommand(_ => RevertSelected(), _ => !IsBusy && SelectedItem is { IsDirty: true } or { HasServerMessage: true });
        RevertAllCommand = new RelayCommand(_ => RevertAll(), _ => !IsBusy && HasChanges);
        ReloadCommand = new RelayCommand(async _ => await ReloadAsync(), _ => !IsBusy);
        EditSldCommand = new RelayCommand(_ => EditSld(), _ => !IsBusy && SelectedItem is { IsLayer: true, CanEditStyles: true });
        RemoveSldCommand = new RelayCommand(_ => { if (SelectedItem is not null) SelectedItem.LayerSld = null; }, _ => !IsBusy && SelectedItem is { HasSld: true });

        AddStyleCommand = new RelayCommand(_ => AddStyle(), _ => !IsBusy && SelectedItem is { IsLayer: true, CanEditStyles: true });
        SwitchToStylesCommand = new RelayCommand(_ => SwitchToStyles(), _ => !IsBusy && SelectedItem is { IsLayer: true, IsSimpleMode: true });
        SwitchToSimpleCommand = new RelayCommand(async _ => await SwitchToSimpleAsync(), _ => !IsBusy && SelectedItem is { IsLayer: true, HasSld: true });
        CopyStyleCommand = new RelayCommand(_ => CopyStyle(), _ => CanActOnStyle());
        RemoveStyleCommand = new RelayCommand(_ => RemoveStyle(), _ => CanActOnStyle());
        SetDefaultStyleCommand = new RelayCommand(_ => WithStyle((item, style) => item.SetDefaultStyle(style)), _ => CanActOnStyle() && SelectedItem!.SelectedStyle!.IsDefault == false);
        MoveStyleUpCommand = new RelayCommand(_ => WithStyle((item, style) => item.MoveStyle(style, -1)), _ => CanActOnStyle() && SelectedItem!.SelectedStyle!.Index > 0);
        MoveStyleDownCommand = new RelayCommand(_ => WithStyle((item, style) => item.MoveStyle(style, +1)), _ => CanActOnStyle() && SelectedItem!.SelectedStyle!.Index < SelectedItem.Styles.Count - 1);
        ClearFilterCommand = new RelayCommand(_ => FilterText = string.Empty, _ => !string.IsNullOrEmpty(FilterText));

        MoveUpCommand = new RelayCommand(_ => MoveBy(-1), _ => CanMoveBy(-1));
        MoveDownCommand = new RelayCommand(_ => MoveBy(+1), _ => CanMoveBy(+1));
        MoveToCommand = new RelayCommand(p => MoveTo(p as ServerLayerMoveTarget), p => !IsBusy && p is ServerLayerMoveTarget { IsCurrent: false } && SelectedItem is { IsLayer: true });
        NewGroupCommand = new RelayCommand(async _ => await NewGroupAsync(), _ => !IsBusy && IsLoaded);
        ExportImportModelCommand = new RelayCommand(async _ => await ExportImportModelAsync(), _ => !IsBusy && IsLoaded);
        RenameCommand = new RelayCommand(_ => RequestFocusTitle?.Invoke(), _ => SelectedItem is not null);
        DeleteGroupCommand = new RelayCommand(async _ => await DeleteGroupAsync(), _ => !IsBusy && SelectedItem is { IsGroup: true });

        Localization.LanguageChanged += OnLanguageChanged;
    }

    #region Host callbacks

    /// <summary>
    /// Opens the SLD editor on one style (by name; null: the default) of the layer's SLD, or on an empty
    /// one, and returns the edited SLD with all its styles, or null when the editor was cancelled. Set by
    /// the window, which owns the editor window.
    /// </summary>
    public Func<ServerLayerSettingItem, string?, StyledLayerDescriptor?>? RequestEditSld { get; set; }

    /// <summary>
    /// The content of a new style: the layer as the map draws it now (its simple colours and label when it
    /// has no SLD). Set by the window, which can find the map layer; without it a new style is empty.
    /// </summary>
    public Func<ServerLayerSettingItem, UserStyle?>? RequestNewStyleTemplate { get; set; }

    /// <summary>Asks a yes/no question (discard unsaved changes?); null answers yes.</summary>
    public Func<string, Task<bool>>? RequestConfirmAsync { get; set; }

    /// <summary>Set by the window: where to save the import model (the suggested file name in); null when the admin cancels.</summary>
    public Func<string, string?>? RequestSaveImportModelPath { get; set; }

    /// <summary>Puts the cursor in the selected row's title (new group, Rename).</summary>
    public Action? RequestFocusTitle { get; set; }

    /// <summary>Something reached the server; the argument is every row as the window now knows it.</summary>
    public event Action<IReadOnlyList<LayerSettingAdminDto>>? Saved;

    /// <summary>True once anything was saved while the window was open.</summary>
    public bool HasSavedChanges { get; private set; }

    #endregion

    #region Tree

    /// <summary>Top-level rows (groups, and layers without a group), listed with the higher TocOrder first, as the map's legend does.</summary>
    public ObservableCollection<ServerLayerSettingItem> Roots { get; } = new();

    public IReadOnlyList<ServerLayerSettingItem> Items => _items;

    private ServerLayerSettingItem? _selectedItem;

    public ServerLayerSettingItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value))
                return;

            var previous = _selectedItem;

            _selectedItem = value;

            // a selection made in code (the legend shortcut, a reload, a move) must also move the tree's highlight
            if (previous is not null)
                previous.IsSelected = false;

            if (value is not null)
            {
                if (value.Parent is not null)
                    value.Parent.IsExpanded = true;

                value.IsSelected = true;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSelection));
            RaisePropertyChanged(nameof(MoveTargets));
            RaisePropertyChanged(nameof(DeleteGroupHint));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HasSelection => SelectedItem is not null;

    /// <summary>The "Move to" menu for the selected layer: every group, then the top level.</summary>
    public IReadOnlyList<ServerLayerMoveTarget> MoveTargets
    {
        get
        {
            if (SelectedItem is not { IsLayer: true } layer)
                return [];

            var groups = Roots.Where(r => r.IsGroup).Select(g => new ServerLayerMoveTarget(g.Title, g, ReferenceEquals(layer.Parent, g)));

            return groups.Append(new ServerLayerMoveTarget(Localization["serverLayerSettings_moveTo_topLevel"], null, layer.Parent is null)).ToList();
        }
    }

    private string _filterText = string.Empty;

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText == value)
                return;

            _filterText = value ?? string.Empty;
            RaisePropertyChanged();
            ApplyFilter();
        }
    }

    public bool HasVisibleItems => Roots.Any(r => r.IsVisible);

    #endregion

    #region State

    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CanSave));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _isLoaded;

    /// <summary>The list has been read at least once (the tree and the editor are shown).</summary>
    public bool IsLoaded
    {
        get => _isLoaded;
        private set { _isLoaded = value; RaisePropertyChanged(); }
    }

    private string? _loadError;

    /// <summary>Why the list could not be read; shown instead of the editor, with Try again.</summary>
    public string? LoadError
    {
        get => _loadError;
        private set
        {
            _loadError = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasLoadError));
        }
    }

    public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

    private string? _resultMessage;

    /// <summary>The outcome of the last save, create or delete, above the editor.</summary>
    public string? ResultMessage
    {
        get => _resultMessage;
        private set
        {
            _resultMessage = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasResultMessage));
        }
    }

    public bool HasResultMessage => !string.IsNullOrEmpty(ResultMessage);

    private bool _resultIsError;

    public bool ResultIsError
    {
        get => _resultIsError;
        private set { _resultIsError = value; RaisePropertyChanged(); }
    }

    public int ChangedCount => _items.Count(i => i.IsDirty);

    public int InvalidCount => _items.Count(i => i.HasValueChanges && i.HasErrors);

    public bool HasChanges => ChangedCount > 0;

    public bool HasMoves => _items.Any(i => i.IsMoved);

    /// <summary>Something to save, and every edited row passes the rules.</summary>
    public bool CanSave => !IsBusy && HasChanges && InvalidCount == 0;

    public string StatusText
    {
        get
        {
            if (!HasChanges)
                return Localization["serverLayerSettings_status_noChanges"];

            var text = string.Format(Localization["serverLayerSettings_status_changes"], ChangedCount);

            return InvalidCount == 0 ? text : text + " · " + string.Format(Localization["serverLayerSettings_status_invalid"], InvalidCount);
        }
    }

    /// <summary>Why Delete group is refused for the selected group; null when it can be deleted.</summary>
    public string? DeleteGroupHint => SelectedItem switch
    {
        { IsGroup: true, Children.Count: > 0 } => Localization["serverLayerSettings_deleteGroup_notEmpty"],
        { IsGroup: true } when HasMoves => Localization["serverLayerSettings_deleteGroup_pendingMoves"],
        _ => null,
    };

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    /// <summary>The selected row back to the server's copy (values and place).</summary>
    public ICommand RevertCommand { get; }

    public ICommand RevertAllCommand { get; }

    /// <summary>Reads everything from the server again (asks first when there are unsaved changes).</summary>
    public ICommand ReloadCommand { get; }

    public ICommand EditSldCommand { get; }

    public ICommand RemoveSldCommand { get; }

    /// <summary>A new style (a copy of how the layer is drawn now), then the SLD editor on it.</summary>
    public ICommand AddStyleCommand { get; }

    /// <summary>The Display tab's switch to Styles: the layer gets a first style drawn like its simple colours.</summary>
    public ICommand SwitchToStylesCommand { get; }

    /// <summary>The switch back to Simple: removes every style (after asking), so the simple colours apply again.</summary>
    public ICommand SwitchToSimpleCommand { get; }

    public ICommand CopyStyleCommand { get; }

    /// <summary>Removes the selected style; removing the last one removes the SLD.</summary>
    public ICommand RemoveStyleCommand { get; }

    /// <summary>The selected style becomes the one users start on.</summary>
    public ICommand SetDefaultStyleCommand { get; }

    public ICommand MoveStyleUpCommand { get; }

    public ICommand MoveStyleDownCommand { get; }

    public ICommand ClearFilterCommand { get; }

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }

    /// <summary>Parameter: a <see cref="ServerLayerMoveTarget"/> of <see cref="MoveTargets"/>.</summary>
    public ICommand MoveToCommand { get; }

    public ICommand NewGroupCommand { get; }

    public ICommand RenameCommand { get; }

    public ICommand DeleteGroupCommand { get; }

    /// <summary>Saves the data model of every active layer as the file the import tool reads.</summary>
    public ICommand ExportImportModelCommand { get; }

    #endregion

    #region Load

    /// <summary>Reads the list and selects <paramref name="selectId"/> (the legend shortcut's layer) or the first row.</summary>
    public async Task LoadAsync(int? selectId = null)
    {
        IsBusy = true;
        LoadError = null;

        try
        {
            var response = await _service.GetAllAsync();

            if (!response.IsSuccess || response.Value is null)
            {
                LoadError = response.ErrorMessage ?? Localization["serverLayerSettings_loadFailed"];
                return;
            }

            Build(response.Value);

            IsLoaded = true;

            Select(selectId is int id ? _items.FirstOrDefault(i => i.Id == id) : null);
        }
        catch (Exception ex)
        {
            LoadError = $"{Localization["serverLayerSettings_loadFailed"]} {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private int _maxTitleLength;

    private void Build(LayerSettingAdminListDto list)
    {
        foreach (var item in _items)
            item.PropertyChanged -= OnItemPropertyChanged;

        _items.Clear();
        _maxTitleLength = list.MaxTitleLength;

        foreach (var row in list.Items)
            Add(row);

        Layout();
    }

    private ServerLayerSettingItem Add(LayerSettingAdminDto row)
    {
        var item = new ServerLayerSettingItem(row, _maxTitleLength);
        item.PropertyChanged += OnItemPropertyChanged;
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Lists the tree from every row's current place (group, then TocOrder descending). The only way the
    /// tree changes shape, so moves, reverts and saves can never disagree with what is shown.
    /// </summary>
    private void Layout()
    {
        var selected = SelectedItem;

        foreach (var item in _items)
            item.Children.Clear();

        Roots.Clear();

        var groups = _items.Where(i => i.IsGroup).ToDictionary(i => i.Id);

        foreach (var item in _items.OrderByDescending(i => i.TocOrder).ThenBy(i => i.Id))
        {
            // a layer whose group is gone (or a group placed in a group) is shown at the top level
            if (item.IsLayer && item.GroupLayerId is int parentId && groups.TryGetValue(parentId, out var parent))
            {
                item.Parent = parent;
                parent.Children.Add(item);
            }
            else
            {
                item.Parent = null;
                Roots.Add(item);
            }
        }

        foreach (var item in _items)
            item.RaiseStructureChanged();

        ApplyFilter();
        RaiseSummary();

        // the containers were regenerated: give the selection back to the tree
        if (selected is not null && _items.Contains(selected))
        {
            _selectedItem = null;
            SelectedItem = selected;
        }
    }

    private void Select(ServerLayerSettingItem? item) => SelectedItem = item ?? Roots.FirstOrDefault(r => r.IsVisible);

    private async Task ReloadAsync()
    {
        if (HasChanges && !await ConfirmAsync(Localization["serverLayerSettings_confirmDiscard"]))
            return;

        ResultMessage = null;

        await LoadAsync(SelectedItem?.Id);
    }

    #endregion

    #region Moves

    /// <summary>The rows listed beside <paramref name="item"/> (same group, or the top level), top first.</summary>
    private IList<ServerLayerSettingItem> SiblingsOf(ServerLayerSettingItem item) => item.Parent?.Children ?? (IList<ServerLayerSettingItem>)Roots;

    private bool CanMoveBy(int step)
    {
        if (IsBusy || SelectedItem is null)
            return false;

        var index = SiblingsOf(SelectedItem).IndexOf(SelectedItem) + step;

        return index >= 0 && index < SiblingsOf(SelectedItem).Count;
    }

    private void MoveBy(int step)
    {
        if (!CanMoveBy(step) || SelectedItem is not { } item)
            return;

        var siblings = SiblingsOf(item).ToList();
        var index = siblings.IndexOf(item);

        siblings.RemoveAt(index);
        siblings.Insert(index + step, item);

        Renumber(siblings, item.Parent?.Id);
        Layout();
    }

    private void MoveTo(ServerLayerMoveTarget? target)
    {
        if (target is null || SelectedItem is not { IsLayer: true } item)
            return;

        Move(item, target.Group, 0);
    }

    /// <summary>Whether <paramref name="dragged"/> may be dropped there: groups stay at the top level, only a group takes rows into it.</summary>
    public bool CanDrop(ServerLayerSettingItem dragged, ServerLayerSettingItem target, ServerLayerDropPosition position)
    {
        if (IsBusy || ReferenceEquals(dragged, target))
            return false;

        // into a group: layers only (dropping a layer on its own group moves it to the group's top)
        if (position == ServerLayerDropPosition.Into)
            return dragged.IsLayer && target.IsGroup;

        // before/after: the dragged row joins the target's list; a group may only join the top level
        return dragged.IsLayer || target.Parent is null;
    }

    public void Drop(ServerLayerSettingItem dragged, ServerLayerSettingItem target, ServerLayerDropPosition position)
    {
        if (!CanDrop(dragged, target, position))
            return;

        if (position == ServerLayerDropPosition.Into)
        {
            Move(dragged, target, 0);
            return;
        }

        var siblings = SiblingsOf(target).Where(s => !ReferenceEquals(s, dragged)).ToList();
        var index = siblings.IndexOf(target) + (position == ServerLayerDropPosition.After ? 1 : 0);

        Move(dragged, target.Parent, index);
    }

    /// <summary>Puts <paramref name="item"/> at <paramref name="index"/> of <paramref name="group"/> (null: the top level) and renumbers both lists.</summary>
    private void Move(ServerLayerSettingItem item, ServerLayerSettingItem? group, int index)
    {
        if (item.IsGroup && group is not null || group is { IsGroup: false })
            return;

        var oldList = SiblingsOf(item).Where(s => !ReferenceEquals(s, item)).ToList();

        var newList = (group?.Children ?? (IList<ServerLayerSettingItem>)Roots).Where(s => !ReferenceEquals(s, item)).ToList();

        newList.Insert(Math.Clamp(index, 0, newList.Count), item);

        if (!ReferenceEquals(item.Parent, group))
            Renumber(oldList, item.Parent?.Id);

        Renumber(newList, group?.Id);

        if (group is not null)
            group.IsExpanded = true;

        Layout();

        SelectedItem = item;
    }

    /// <summary>Top first: the first row gets the highest TocOrder (n), the last one 1.</summary>
    private static void Renumber(IReadOnlyList<ServerLayerSettingItem> topFirst, int? groupId)
    {
        for (int i = 0; i < topFirst.Count; i++)
            topFirst[i].Place(groupId, topFirst.Count - i);
    }

    #endregion

    #region Import model

    private async Task ExportImportModelAsync()
    {
        IsBusy = true;
        ResultMessage = null;

        try
        {
            var response = await _service.GetImportModelAsync();

            if (!response.IsSuccess || response.Value is null)
            {
                ShowResult(response.ErrorMessage ?? Localization["serverLayerSettings_serverRejected"], isError: true);
                return;
            }

            var model = response.Value;

            var path = RequestSaveImportModelPath?.Invoke($"{model.Application}-import-model-{DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)}.json");

            if (string.IsNullOrWhiteSpace(path))
                return;

            await System.IO.File.WriteAllTextAsync(path, model.ToJson());

            ShowResult(string.Format(Localization["serverLayerSettings_importModel_saved"], model.Layers.Count, System.IO.Path.GetFileName(path)), isError: false);
        }
        catch (Exception ex)
        {
            ShowResult($"{Localization["serverLayerSettings_importModel_failed"]} {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Groups

    private async Task NewGroupAsync()
    {
        IsBusy = true;
        ResultMessage = null;

        try
        {
            var title = Localization["serverLayerSettings_newGroup_title"];

            var response = await _service.CreateGroupAsync(new LayerGroupCreateDto { Title = title, IsExpandedInToc = true });

            if (!response.IsSuccess || response.Value is null)
            {
                ShowResult(response.ErrorMessage ?? Localization["serverLayerSettings_serverRejected"], isError: true);
                return;
            }

            var group = Add(response.Value);

            Layout();

            SelectedItem = group;

            NotifySaved();
            ShowResult(string.Format(Localization["serverLayerSettings_group_created"], group.Title), isError: false);
        }
        catch (Exception ex)
        {
            ShowResult($"{Localization["serverLayerSettings_saveFailed"]} {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }

        // the admin names it next
        RequestFocusTitle?.Invoke();
    }

    private async Task DeleteGroupAsync()
    {
        if (SelectedItem is not { IsGroup: true } group)
            return;

        if (DeleteGroupHint is string why)
        {
            ShowResult(why, isError: true);
            return;
        }

        if (!await ConfirmAsync(string.Format(Localization["serverLayerSettings_deleteGroup_confirm"], group.Title)))
            return;

        IsBusy = true;
        ResultMessage = null;

        try
        {
            var response = await _service.DeleteGroupAsync(group.Id);

            if (response.IsSuccess || response.Status == LayerSettingAdminStatus.NotFound)
            {
                var index = Roots.IndexOf(group);

                group.PropertyChanged -= OnItemPropertyChanged;
                _items.Remove(group);
                _selectedItem = null;

                Layout();

                Select(Roots.Count == 0 ? null : Roots[Math.Clamp(index, 0, Roots.Count - 1)]);

                NotifySaved();
                ShowResult(string.Format(Localization["serverLayerSettings_group_deleted"], group.Title), isError: false);
                return;
            }

            ShowResult(response.Status == LayerSettingAdminStatus.GroupNotEmpty
                ? Localization["serverLayerSettings_deleteGroup_notEmpty"]
                : response.ErrorMessage ?? Localization["serverLayerSettings_serverRejected"], isError: true);
        }
        catch (Exception ex)
        {
            ShowResult($"{Localization["serverLayerSettings_saveFailed"]} {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Save

    /// <summary>
    /// Saves the moves (one order request), then every row with edited values (one request each); returns
    /// true when everything was saved.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        if (!CanSave)
            return false;

        IsBusy = true;
        ResultMessage = null;

        var saved = new HashSet<ServerLayerSettingItem>();
        var refused = 0;
        string? failure = null;

        try
        {
            // 1. the order first: it gives the moved rows a new UpdatedAt, which their value saves must carry
            var moved = _items.Where(i => i.IsMoved).ToList();

            if (moved.Count > 0)
            {
                var response = await _service.UpdateOrderAsync(new LayerSettingOrderDto { Items = moved.Select(i => i.ToOrderItem()).ToList() });

                if (response.IsSuccess && response.Value is not null)
                {
                    var byId = response.Value.Items.ToDictionary(r => r.Id);

                    foreach (var item in _items)
                    {
                        if (byId.TryGetValue(item.Id, out var row) && (item.IsMoved || row.UpdatedAt != item.UpdatedAt))
                        {
                            if (item.IsMoved)
                                saved.Add(item);

                            item.AcceptPlacement(row);
                        }
                    }
                }
                else
                {
                    failure = response.Status is null
                        ? response.ErrorMessage ?? Localization["serverLayerSettings_saveFailed"]
                        : Localization["serverLayerSettings_orderRejected"];
                }
            }

            // 2. the edited values, one row at a time
            if (failure is null)
            {
                foreach (var item in _items.Where(i => i.HasValueChanges).ToList())
                {
                    var response = await _service.UpdateAsync(item.Id, item.ToUpdateDto());

                    if (response.IsSuccess && response.Value is not null)
                    {
                        item.Accept(response.Value);
                        saved.Add(item);
                        continue;
                    }

                    if (response.Status is null)
                    {
                        // no answer from the server: the remaining rows would fail the same way
                        failure = response.ErrorMessage ?? Localization["serverLayerSettings_saveFailed"];
                        break;
                    }

                    refused++;
                    ApplyRefusal(item, response);
                }
            }
        }
        catch (Exception ex)
        {
            failure = $"{Localization["serverLayerSettings_saveFailed"]} {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }

        if (saved.Count > 0)
            NotifySaved();

        // a refused row keeps its edits; the tree shows every row where the server now has it
        Layout();

        ResultIsError = failure is not null || refused > 0;

        ResultMessage = failure is not null
            ? (saved.Count > 0 ? string.Format(Localization["serverLayerSettings_result_partial"], saved.Count, ChangedCount) + " " : string.Empty) + failure
            : refused > 0
                ? string.Format(Localization["serverLayerSettings_result_partial"], saved.Count, refused)
                : string.Format(Localization["serverLayerSettings_result_saved"], saved.Count);

        RaiseSummary();

        return failure is null && refused == 0;
    }

    private static void ApplyRefusal(ServerLayerSettingItem item, LayerSettingsAdminResponse<LayerSettingAdminDto> response)
    {
        switch (response.Status)
        {
            case LayerSettingAdminStatus.Conflict:
                item.HasConflict = true;
                item.ServerMessage = Localization["serverLayerSettings_conflict"];
                break;

            case LayerSettingAdminStatus.NotFound:
                item.ServerMessage = Localization["serverLayerSettings_notFound"];
                break;

            case LayerSettingAdminStatus.Invalid:
                item.SetServerErrors(response.Errors);
                item.ServerMessage = Localization["serverLayerSettings_serverRejected"];
                break;

            default:
                item.ServerMessage = response.ErrorMessage ?? Localization["serverLayerSettings_serverRejected"];
                break;
        }
    }

    private void NotifySaved()
    {
        HasSavedChanges = true;
        Saved?.Invoke(_items.Select(SnapshotOf).ToList());
    }

    private void ShowResult(string message, bool isError)
    {
        ResultIsError = isError;
        ResultMessage = message;
    }

    // The saved rows' server copies; unsaved rows as they are now.
    private static LayerSettingAdminDto SnapshotOf(ServerLayerSettingItem item)
    {
        var edit = item.ToUpdateDto();

        return new LayerSettingAdminDto
        {
            Id = item.Id,
            IsGroupLayer = item.IsGroup,
            GroupLayerId = item.GroupLayerId,
            TocOrder = item.TocOrder,
            TableName = item.TableName,
            ServiceUrl = item.ServiceUrl,
            FieldNames = item.FieldNames.ToList(),
            Title = edit.Title,
            IsActive = edit.IsActive,
            IsLayerOn = edit.IsLayerOn,
            IsExpandedInToc = edit.IsExpandedInToc,
            IsSearchable = edit.IsSearchable,
            MinLayerZoomLevel = edit.MinLayerZoomLevel,
            MaxLayerZoomLevel = edit.MaxLayerZoomLevel,
            HexFill = edit.HexFill,
            HexStroke = edit.HexStroke,
            StrokeThickness = edit.StrokeThickness,
            Opacity = edit.Opacity,
            LayerSld = edit.LayerSld,
            IsLabeled = edit.IsLabeled,
            IsLabelOn = edit.IsLabelOn,
            LabelColumn = edit.LabelColumn,
            LabelSize = edit.LabelSize,
            LabelHexColor = edit.LabelHexColor,
            MinLabelZoomLevel = edit.MinLabelZoomLevel,
            MaxLabelZoomLevel = edit.MaxLabelZoomLevel,
            LoadMode = edit.LoadMode,
            UpdatedAt = item.UpdatedAt,
        };
    }

    #endregion

    #region Revert, SLD, close

    private void RevertSelected()
    {
        var moved = SelectedItem?.IsMoved == true;

        SelectedItem?.Revert();

        if (moved)
            Layout();

        RaiseSummary();
    }

    private void RevertAll()
    {
        foreach (var item in _items.Where(i => i.IsDirty || i.HasServerMessage))
            item.Revert();

        ResultMessage = null;
        Layout();
    }

    /// <summary>The SLD editor on the selected style (the default one when none is selected; a first style when there is none).</summary>
    private void EditSld()
    {
        if (SelectedItem is not { IsLayer: true } item || RequestEditSld is null)
            return;

        if (!item.HasStyles)
        {
            AddStyle();
            return;
        }

        EditStyle(item, item.SelectedStyle?.Name);
    }

    private void EditStyle(ServerLayerSettingItem item, string? styleName)
    {
        var sld = RequestEditSld?.Invoke(item, styleName);

        if (sld is not null)
            item.ReplaceSld(sld, styleName);
    }

    private void AddStyle()
    {
        if (SelectedItem is not { IsLayer: true } item)
            return;

        var template = RequestNewStyleTemplate?.Invoke(item) ?? new UserStyle();

        var title = string.Format(Localization["layerStyles_newTitle"], item.Styles.Count + 1);

        item.AddStyle(template, title);

        // straight into the editor on the new style; cancelling keeps it as created
        if (item.SelectedStyle?.Name is { } name)
            EditStyle(item, name);
    }

    private void SwitchToStyles()
    {
        if (SelectedItem is not { IsLayer: true, IsSimpleMode: true } item)
            return;

        var template = RequestNewStyleTemplate?.Invoke(item) ?? new UserStyle();

        item.AddStyle(template, string.Format(Localization["layerStyles_newTitle"], 1));
    }

    private async Task SwitchToSimpleAsync()
    {
        if (SelectedItem is not { IsLayer: true, HasSld: true } item)
            return;

        var question = string.Format(Localization["layerStyles_switchToSimpleConfirm"], Math.Max(1, item.Styles.Count));

        if (RequestConfirmAsync is not null && !await RequestConfirmAsync(question))
            return;

        item.LayerSld = null;
    }

    private void CopyStyle()
        => WithStyle((item, style) => item.CopyStyle(style, string.Format(Localization["layerStyles_copyTitle"], style.DisplayTitle)));

    private void RemoveStyle() => WithStyle((item, style) => item.RemoveStyle(style));

    private bool CanActOnStyle() => !IsBusy && SelectedItem is { IsLayer: true, CanEditStyles: true, SelectedStyle: not null };

    private void WithStyle(Action<ServerLayerSettingItem, LayerStyleItem> action)
    {
        if (SelectedItem is { IsLayer: true, SelectedStyle: { } style } item)
            action(item, style);
    }

    /// <summary>True when the window may close: nothing unsaved, or the admin agreed to discard it.</summary>
    public async Task<bool> CanCloseAsync()
    {
        if (IsBusy)
            return false;

        return !HasChanges || await ConfirmAsync(Localization["serverLayerSettings_confirmDiscard"]);
    }

    private async Task<bool> ConfirmAsync(string message) => RequestConfirmAsync is null || await RequestConfirmAsync(message);

    #endregion

    #region Filter and summary

    private void ApplyFilter()
    {
        var text = FilterText.Trim();

        foreach (var root in Roots)
        {
            var groupMatches = Matches(root, text);

            var anyChild = false;

            foreach (var child in root.Children)
            {
                // a matching group shows all its layers
                child.IsVisible = groupMatches || Matches(child, text);
                anyChild |= child.IsVisible;
            }

            root.IsVisible = groupMatches || anyChild;

            if (text.Length > 0 && anyChild)
                root.IsExpanded = true;
        }

        RaisePropertyChanged(nameof(HasVisibleItems));
    }

    private static bool Matches(ServerLayerSettingItem item, string text) =>
        text.Length == 0
        || item.Title.Contains(text, StringComparison.CurrentCultureIgnoreCase)
        || (item.TableName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
        || item.Id.ToString() == text;

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ServerLayerSettingItem.IsDirty) or nameof(ServerLayerSettingItem.HasErrors) or nameof(ServerLayerSettingItem.HasValueChanges))
            RaiseSummary();

        else if (e.PropertyName == nameof(ServerLayerSettingItem.IsSelected) && sender is ServerLayerSettingItem { IsSelected: true } item)
            SelectedItem = item;

        // a renamed group: its layers' header line and the Move to menu show the new title
        else if (e.PropertyName == nameof(ServerLayerSettingItem.Title) && sender is ServerLayerSettingItem { IsGroup: true } group)
        {
            foreach (var child in group.Children)
                child.RaiseStructureChanged();

            RaisePropertyChanged(nameof(MoveTargets));
        }
    }

    private void RaiseSummary()
    {
        RaisePropertyChanged(nameof(ChangedCount));
        RaisePropertyChanged(nameof(InvalidCount));
        RaisePropertyChanged(nameof(HasChanges));
        RaisePropertyChanged(nameof(HasMoves));
        RaisePropertyChanged(nameof(CanSave));
        RaisePropertyChanged(nameof(StatusText));
        RaisePropertyChanged(nameof(DeleteGroupHint));
        CommandManager.InvalidateRequerySuggested();
    }

    private void OnLanguageChanged()
    {
        foreach (var item in _items)
        {
            item.Revalidate();
            item.RaiseStructureChanged();
        }

        RaisePropertyChanged(nameof(StatusText));
        RaisePropertyChanged(nameof(MoveTargets));
        RaisePropertyChanged(nameof(DeleteGroupHint));
    }

    /// <summary>Stops listening to language changes; the window calls it when it closes.</summary>
    public void Detach() => Localization.LanguageChanged -= OnLanguageChanged;

    #endregion
}
