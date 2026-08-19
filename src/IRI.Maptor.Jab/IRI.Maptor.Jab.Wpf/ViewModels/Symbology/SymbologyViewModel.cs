using IRI.Maptor.Jab.Core;
using System;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Symbology;

public class SymbologyViewModel : Notifier
{
    private VisualParameters _symbology;

    public VisualParameters Symbology
    {
        get { return _symbology; }
        set
        {
            _symbology = value;
            RaisePropertyChanged();
        }
    }

    public Action RequestCloseAction;

    public Action<SymbologyViewModel> RequestApplyAction;

    /// <summary>
    /// Opens the full SLD editor for the same layer. Wired by hosts that can offer it
    /// (see <see cref="ShowAdvancedOption"/>); unwired hosts keep the plain dialog.
    /// </summary>
    public Action? RequestShowAdvancedAction;

    private bool _showAdvancedOption;

    /// <summary>Shows the "Advanced (SLD)" button when the host wired <see cref="RequestShowAdvancedAction"/>.</summary>
    public bool ShowAdvancedOption
    {
        get { return _showAdvancedOption; }
        set
        {
            _showAdvancedOption = value;
            RaisePropertyChanged();
        }
    }

    private RelayCommand? _showAdvancedCommand;

    public RelayCommand ShowAdvancedCommand
    {
        get
        {
            if (_showAdvancedCommand == null)
            {
                _showAdvancedCommand = new RelayCommand(param =>
                {
                    RequestShowAdvancedAction?.Invoke();
                });
            }

            return _showAdvancedCommand;
        }
    }

    /// <summary>
    /// Restores the layer's default symbology. Wired by hosts whose layer carries a
    /// captured default (see <see cref="ShowResetOption"/>).
    /// </summary>
    public Action? RequestResetAction;

    private bool _showResetOption;

    /// <summary>Shows the "Reset to default" button when the host wired <see cref="RequestResetAction"/>.</summary>
    public bool ShowResetOption
    {
        get { return _showResetOption; }
        set
        {
            _showResetOption = value;
            RaisePropertyChanged();
        }
    }

    private RelayCommand? _resetCommand;

    public RelayCommand ResetCommand
    {
        get
        {
            if (_resetCommand == null)
            {
                _resetCommand = new RelayCommand(param =>
                {
                    RequestResetAction?.Invoke();
                });
            }

            return _resetCommand;
        }
    }

    private RelayCommand _closeCommand;

    public RelayCommand CloseCommand
    {
        get
        {
            if (_closeCommand == null)
            {
                _closeCommand = new RelayCommand(param =>
                {
                    RequestCloseAction?.Invoke();
                });
            }

            return _closeCommand;
        }
    }

    private RelayCommand _applyCommand;

    public RelayCommand ApplyCommand
    {
        get
        {
            if (_applyCommand == null)
            {
                _applyCommand = new RelayCommand(param =>
                {
                    RequestApplyAction?.Invoke(this);
                });
            }

            return _applyCommand;
        }
    }

}
