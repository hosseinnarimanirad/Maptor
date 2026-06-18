using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Layers;

namespace IRI.Maptor.Jab.Common.Layers;

public abstract class BaseLayer : IRI.Maptor.Jab.Core.Layers.BaseLayer, IWpfLayer
{
    public BaseLayer() : base()
    {
    }

    #region WPF dispatching overrides

    protected override void InvokeOnUiThread(Action action)
    {
        var app = Application.Current;
        if (app?.Dispatcher == null || app.Dispatcher.HasShutdownStarted || app.Dispatcher.HasShutdownFinished)
        {
            action();
            return;
        }
        if (app.Dispatcher.CheckAccess())
            action();
        else
            app.Dispatcher.BeginInvoke(action);
    }

    protected override void OnIsInitializingChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }

    protected override void OnIsVisibleChanged(bool isVisible)
    {
        if (Element is null && isVisible)
            RequestChangeVisibility?.Invoke(this);
    }

    #endregion

    #region IsVisible / Visibility sync

    public override bool IsVisible
    {
        get => base.IsVisible;
        set
        {
            base.IsVisible = value;
            RaisePropertyChanged(nameof(Visibility));
        }
    }

    private Visibility _visibility;
    public virtual Visibility Visibility
    {
        get => base.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        set
        {
            IsVisible = (value == Visibility.Visible);
        }
    }

    #endregion

    #region ShowOptions override (adds Commands.Count check)

    public new bool ShowOptions => IsSelectedInToc && Commands?.Count > 0 && !IsGroupLayer;

    #endregion

    #region FrameworkElement binding

    private FrameworkElement? _element;
    public FrameworkElement? Element
    {
        get { return _element; }
        set
        {
            _element = value;
            if (value is not null)
                BindWithFrameworkElement(value);
            RaisePropertyChanged();
        }
    }

    protected virtual void BindWithFrameworkElement(FrameworkElement? element)
    {
        if (element is null)
            return;

        Binding binding4 = new Binding() { Source = this, Path = new System.Windows.PropertyPath(nameof(Visibility)), Mode = BindingMode.TwoWay };
        element.SetBinding(UIElement.VisibilityProperty, binding4);

        Binding binding5 = new Binding() { Source = this, Path = new System.Windows.PropertyPath(nameof(Opacity)), Mode = BindingMode.TwoWay };
        element.SetBinding(UIElement.OpacityProperty, binding5);
    }

    #endregion

    #region Visibility convenience methods

    public void SetVisibility(Visibility visibility) => IsVisible = (visibility == Visibility.Visible);

    public void ToggleVisibility() => IsVisible = !IsVisible;

    #endregion

    #region WPF commands (RequestMoveLayerUp/Down with ICollectionView)

    public Func<ILayer, System.ComponentModel.ICollectionView?, Task>? RequestMoveLayerDown { get; set; }
    public Func<ILayer, System.ComponentModel.ICollectionView?, Task>? RequestMoveLayerUp { get; set; }

    public Action<ILayer> RequestShowLayerSettings { get; set; }

    #endregion

    #region Commands (ILegendCommand / IFeatureTableCommand)

    private List<IFeatureTableCommand> _featureTableCommands = new();
    public List<IFeatureTableCommand> FeatureTableCommands
    {
        get => _featureTableCommands;
        set
        {
            _featureTableCommands = value;
            RaisePropertyChanged();
        }
    }

    private List<ILegendCommand> _commands = new();
    public List<ILegendCommand> Commands
    {
        get => _commands;
        set
        {
            _commands = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ShowOptions));
        }
    }

    #endregion

    #region RelayCommands

    private RelayCommand? _changeSymbologyCommand;
    public RelayCommand ChangeSymbologyCommand
    {
        get
        {
            if (_changeSymbologyCommand == null)
                _changeSymbologyCommand = new RelayCommand(param => { RequestChangeSymbology?.Invoke(this); });
            return _changeSymbologyCommand;
        }
    }

    private RelayCommand? _toggleExpandCommand;
    public RelayCommand ToggleExpandCommand
    {
        get
        {
            if (_toggleExpandCommand == null)
                _toggleExpandCommand = new RelayCommand(param => { IsExpandedInToc = !IsExpandedInToc; });
            return _toggleExpandCommand;
        }
    }

    private RelayCommand? _saveChangesCommand;
    public RelayCommand SaveChangesCommand
    {
        get
        {
            if (_saveChangesCommand == null)
                _saveChangesCommand = new RelayCommand(
                    async param => await SaveChangesAsync(),
                    param => HasPendingChanges && IsNotBusy);
            return _saveChangesCommand;
        }
    }

    private RelayCommand? _undoAllChangesCommand;
    public RelayCommand UndoAllChangesCommand
    {
        get
        {
            if (_undoAllChangesCommand == null)
                _undoAllChangesCommand = new RelayCommand(
                    async param => await UndoAllChanges(),
                    param => HasPendingChanges && IsNotBusy);
            return _undoAllChangesCommand;
        }
    }

    private RelayCommand? _reloadDataCommand;
    public RelayCommand? ReloadDataCommand
    {
        get
        {
            if (_reloadDataCommand == null)
                _reloadDataCommand = new RelayCommand(
                    async param => await ReloadDataAsync(),
                    param => DataSource != null && IsNotBusy);
            return _reloadDataCommand;
        }
    }

    private RelayCommand? _showLayerSettingsCommand;
    public RelayCommand ShowLayerSettingsCommand
    {
        get
        {
            if (_showLayerSettingsCommand == null)
                _showLayerSettingsCommand = new RelayCommand(param => RequestShowLayerSettings?.Invoke(this), _ => IsNotBusy);
            return _showLayerSettingsCommand;
        }
    }

    private RelayCommand? _moveLayerUpCommand;
    public RelayCommand MoveLayerUpCommand
    {
        get
        {
            if (_moveLayerUpCommand == null)
                _moveLayerUpCommand = new RelayCommand(
                    async param =>
                    {
                        if (RequestMoveLayerUp is not null)
                            await RequestMoveLayerUp.Invoke(this, param as System.ComponentModel.ICollectionView);
                    },
                    _ => CanMoveLayerUp);
            return _moveLayerUpCommand;
        }
    }

    private RelayCommand? _moveLayerDownCommand;
    public RelayCommand MoveLayerDownCommand
    {
        get
        {
            if (_moveLayerDownCommand == null)
                _moveLayerDownCommand = new RelayCommand(
                    async param =>
                    {
                        if (RequestMoveLayerDown is not null)
                            await RequestMoveLayerDown.Invoke(this, param as System.ComponentModel.ICollectionView);
                    },
                    _ => CanMoveLayerDown);
            return _moveLayerDownCommand;
        }
    }

    #endregion

    #region WPF-only events

    private event EventHandler<CustomEventArgs<VisualParameters>>? _onVisibilityChanged;
    public event EventHandler<CustomEventArgs<VisualParameters>> OnVisibilityChanged
    {
        remove { _onVisibilityChanged -= value; }
        add
        {
            if (_onVisibilityChanged == null)
                _onVisibilityChanged += value;
        }
    }

    #endregion
}
