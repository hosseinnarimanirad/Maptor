using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Wpf.Layers;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>
/// One <see cref="IdentifyFeatureAction"/> bound to the feature selected in the identify window: the
/// button the view shows. A new item is made for every selection and cancelled when the selection
/// moves on, so a slow state request for an earlier feature never relabels the current one.
/// </summary>
public class IdentifyFeatureActionItem : Notifier
{
    private readonly CancellationToken _selectionToken;

    private bool _isRunning;

    public IdentifyFeatureActionItem(IdentifyFeatureAction action, VectorLayer layer, Feature<Point> feature, CancellationToken selectionToken)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        Feature = feature ?? throw new ArgumentNullException(nameof(feature));

        _selectionToken = selectionToken;

        _text = action.Text;
        _toolTip = action.ToolTip ?? action.Text;

        Command = new RelayCommand(async _ => await ExecuteAsync(), _ => IsEnabled && !_isRunning);
    }

    public IdentifyFeatureAction Action { get; }

    public VectorLayer Layer { get; }

    public Feature<Point> Feature { get; }

    public string? PathMarkup => Action.PathMarkup;

    public bool HasIcon => !string.IsNullOrEmpty(Action.PathMarkup);

    public ICommand Command { get; }

    private string _text;
    public string Text
    {
        get { return _text; }
        private set
        {
            _text = value;
            RaisePropertyChanged();
        }
    }

    private string? _toolTip;
    public string? ToolTip
    {
        get { return _toolTip; }
        private set
        {
            _toolTip = value;
            RaisePropertyChanged();
        }
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get { return _isEnabled; }
        private set
        {
            _isEnabled = value;
            RaisePropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get { return _isVisible; }
        private set
        {
            _isVisible = value;
            RaisePropertyChanged();
        }
    }

    private string? _badge;
    /// <summary>Short text drawn on the circle (e.g. a count); null for none.</summary>
    public string? Badge
    {
        get { return _badge; }
        private set
        {
            _badge = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasBadge));
        }
    }

    public bool HasBadge => !string.IsNullOrEmpty(Badge);

    private bool _isLoading;
    /// <summary>A state request is in flight.</summary>
    public bool IsLoading
    {
        get { return _isLoading; }
        private set
        {
            _isLoading = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>Asks the action for this feature's state; a failed or late answer keeps what is shown.</summary>
    public async Task RefreshAsync()
    {
        if (Action.GetStateAsync is null || _selectionToken.IsCancellationRequested)
            return;

        try
        {
            IsLoading = true;

            var state = await Action.GetStateAsync(Layer, Feature, _selectionToken);

            if (_selectionToken.IsCancellationRequested || state is null)
                return;

            Apply(state);
        }
        catch (Exception)
        {
            // cancelled, offline, server error: the default label still works as a button
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(IdentifyFeatureActionState state)
    {
        Text = state.Text;
        ToolTip = state.ToolTip ?? state.Text;
        IsEnabled = state.IsEnabled;
        IsVisible = state.IsVisible;
        Badge = state.Badge;
    }

    private async Task ExecuteAsync()
    {
        if (_isRunning)
            return;

        try
        {
            _isRunning = true;
            CommandManager.InvalidateRequerySuggested();

            await Action.ExecuteAsync(Layer, Feature);
        }
        catch (Exception)
        {
            // the action reports its own errors; an escaping one would take down the
            // application from this async-void command
        }
        finally
        {
            _isRunning = false;
            CommandManager.InvalidateRequerySuggested();
        }

        // what the action did (an upload, a delete) may have changed the label
        await RefreshAsync();
    }
}
