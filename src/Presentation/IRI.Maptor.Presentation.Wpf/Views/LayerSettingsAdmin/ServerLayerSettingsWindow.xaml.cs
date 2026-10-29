using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using MahApps.Metro.Controls;

using IRI.Maptor.Extensions;

using IRI.Maptor.Core.Ogc.SLD;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Presentation.Core.Layers;
using IRI.Maptor.Presentation.Wpf.Layers;
using IRI.Maptor.Presentation.Wpf.Cartography.Symbologies;
using IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettingsAdmin;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettingsAdmin;
using IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

namespace IRI.Maptor.Presentation.Wpf.Controls.LayerSettingsAdmin;

/// <summary>
/// The layer-settings admin window. Open it with <see cref="ShowDialog(Window, ILayerSettingsAdminService, int?, IDialogService?, Action{IReadOnlyList{LayerSettingAdminDto}}?)"/>;
/// it reads the list itself, asks before closing with unsaved changes, and hosts the SLD editor.
/// </summary>
public partial class ServerLayerSettingsWindow : MetroWindow
{
    private readonly IDialogService? _dialogService;

    private readonly int? _initialSelectionId;

    // the map layer of a setting id, when it is on the map: its fields, geometry and features feed the SLD
    // editor's rule generator (the admin list itself carries field names only)
    private readonly Func<int, ILayer?>? _findMapLayer;

    private bool _closeConfirmed;

    public ServerLayerSettingsWindow(ServerLayerSettingsViewModel viewModel, int? initialSelectionId = null, IDialogService? dialogService = null, Func<int, ILayer?>? findMapLayer = null)
    {
        InitializeComponent();

        _dialogService = dialogService;
        _initialSelectionId = initialSelectionId;
        _findMapLayer = findMapLayer;

        DataContext = viewModel;

        viewModel.RequestEditSld = EditSld;
        viewModel.RequestNewStyleTemplate = CreateNewStyleTemplate;
        viewModel.RequestConfirmAsync = ConfirmAsync;
        viewModel.RequestSaveImportModelPath = AskImportModelPath;

        Loaded += OnLoaded;
    }

    public ServerLayerSettingsViewModel ViewModel => (ServerLayerSettingsViewModel)DataContext;

    /// <summary>
    /// Opens the window modally over <paramref name="owner"/>, on <paramref name="layerId"/> when given (the
    /// legend shortcut). <paramref name="onSaved"/> runs after every save, with every row as the server now
    /// has it. Returns true when anything was saved.
    /// </summary>
    public static bool ShowDialog(
        Window? owner,
        ILayerSettingsAdminService service,
        int? layerId = null,
        IDialogService? dialogService = null,
        Action<IReadOnlyList<LayerSettingAdminDto>>? onSaved = null,
        Func<int, ILayer?>? findMapLayer = null)
    {
        var viewModel = new ServerLayerSettingsViewModel(service);

        if (onSaved is not null)
            viewModel.Saved += onSaved;

        var window = new ServerLayerSettingsWindow(viewModel, layerId, dialogService, findMapLayer);

        if (owner is not null)
            window.Owner = owner;
        else
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        window.ShowDialog();

        return viewModel.HasSavedChanges;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync(_initialSelectionId);

        if (_initialSelectionId is null)
            editor.FocusSearch();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            editor.FocusSearch();
            e.Handled = true;
        }
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (e.Cancel || _closeConfirmed || (!ViewModel.HasChanges && !ViewModel.IsBusy))
            return;

        // ask after the close request has been turned down, then close for real
        e.Cancel = true;

        if (!await ViewModel.CanCloseAsync())
            return;

        _closeConfirmed = true;
        _ = Dispatcher.BeginInvoke(Close);
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.Detach();
        base.OnClosed(e);
    }

    private string? AskImportModelPath(string suggestedName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = suggestedName,
            DefaultExt = ".json",
            Filter = "JSON (*.json)|*.json",
            Title = IRI.Maptor.Presentation.Core.Localization.LocalizationManager.Instance["serverLayerSettings_importModel_export"],
        };

        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private async Task<bool> ConfirmAsync(string message)
    {
        if (_dialogService is not null)
            return await _dialogService.ShowYesNoDialogAsync(message, this) == true;

        return MessageBox.Show(this, message, Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    /// <summary>
    /// The SLD editor on one style of the row's SLD (null: the default style); returns the edited SLD with
    /// all its styles, or null when nothing was applied.
    /// </summary>
    private StyledLayerDescriptor? EditSld(ServerLayerSettingItem item, string? styleName)
    {
        var sld = item.LayerSld is null ? null : SldHelper.Parse(item.LayerSld);

        var mapLayer = _findMapLayer?.Invoke(item.Id) as SymbolizableLayer;

        var editor = SldEditorViewModel.Create(item.Title, sld, item.FieldNames.ToList(), mapLayer?.SpatialModelMode, mapLayer?.GetFields(), styleName);

        editor.ValuesProvider = SldRuleGeneratorViewModel.ValuesProviderFor(mapLayer);

        StyledLayerDescriptor? result = null;

        editor.RequestApplyAction = p => result = p.ToStyledLayerDescriptor();

        var window = new SldEditorWindow(editor)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        window.ShowDialog();

        return result;
    }

    /// <summary>
    /// A new style drawn like the row's simple colours (fill, stroke, width, opacity), for the layer's
    /// geometry when the layer is on the map. The simple label settings are not copied: that label is drawn
    /// beside whichever style is active.
    /// </summary>
    private UserStyle? CreateNewStyleTemplate(ServerLayerSettingItem item)
    {
        var settings = item.ToUpdateDto();

        var mapLayer = _findMapLayer?.Invoke(item.Id) as SymbolizableLayer;

        var symbolizer = SimpleSymbolizer.Create(settings.HexFill, settings.HexStroke, settings.StrokeThickness, settings.Opacity);

        return new ISymbolizer[] { symbolizer }.ParseToSld(mapLayer?.SpatialModelMode).FindStyleAt(0);
    }
}
