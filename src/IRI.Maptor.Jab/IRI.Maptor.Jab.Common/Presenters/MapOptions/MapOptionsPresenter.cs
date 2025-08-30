using System;

using MahApps.Metro.IconPacks;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Sta.Common.Abstrations;


namespace IRI.Maptor.Jab.Common.Presenters;

public class MapOptionsPresenter : Notifier, ILocateable
{
    private Point _point = Point.NaN;

    public Point Location
    {
        get => _point;
        set
        {
            _point = value;
            RaisePropertyChanged();
        }
    }

    public Action<object>? RightCommandAction;
    public Action<object>? LeftCommandAction;
    public Action<object>? MiddleCommandAction;
    public Action<object>? UpperRightCommandAction;
    public Action<object>? UpperLeftCommandAction;

    #region Symbols

    private PackIconModernKind? _rightSymbol;
    public PackIconModernKind? RightSymbol
    {
        get { return _rightSymbol; }
        set
        {
            _rightSymbol = value;
            RaisePropertyChanged();
        }
    }


    private PackIconModernKind? _leftSymbol;
    public PackIconModernKind? LeftSymbol
    {
        get { return _leftSymbol; }
        set
        {
            _leftSymbol = value;
            RaisePropertyChanged();
        }
    }


    private PackIconModernKind? _middleSymbol;
    public PackIconModernKind? MiddleSymbol
    {
        get { return _middleSymbol; }
        set
        {
            _middleSymbol = value;
            RaisePropertyChanged();
        }
    }


    private PackIconModernKind? _upperRightSymbol;
    public PackIconModernKind? UpperRightSymbol
    {
        get { return _upperRightSymbol; }
        set
        {
            _upperRightSymbol = value;
            RaisePropertyChanged();
        }
    }


    private PackIconModernKind? _upperLeftSymbol;
    public PackIconModernKind? UpperLeftSymbol
    {
        get { return _upperLeftSymbol; }
        set
        {
            _upperLeftSymbol = value;
            RaisePropertyChanged();
        }
    }



    #endregion


    #region Tooltips

    private string? _rightToolTip;
    public string? RightToolTip
    {
        get { return _rightToolTip; }
        set
        {
            _rightToolTip = value;
            RaisePropertyChanged();
        }
    }


    private string? _leftToolTip;
    public string? LeftToolTip
    {
        get { return _leftToolTip; }
        set
        {
            _leftToolTip = value;
            RaisePropertyChanged();
        }
    }


    private string? _middleToolTip;
    public string? MiddleToolTip
    {
        get { return _middleToolTip; }
        set
        {
            _middleToolTip = value;
            RaisePropertyChanged();
        }
    }


    private string? _upperRightTooltip;
    public string? UpperRightToolTip
    {
        get { return _upperRightTooltip; }
        set
        {
            _upperRightTooltip = value;
            RaisePropertyChanged();
        }
    }


    private string? _upperLeftToolTip;
    public string? UpperLeftToolTip
    {
        get { return _upperLeftToolTip; }
        set
        {
            _upperLeftToolTip = value;
            RaisePropertyChanged();
        }
    }

    #endregion


    #region Commands

    private RelayCommand? _rightCommand;
    public RelayCommand RightCommand
    {
        get
        {
            if (_rightCommand == null)
            {
                _rightCommand = new RelayCommand(param =>
                {
                    RightCommandAction?.Invoke(param);
                });
            }
            return _rightCommand;
        }
    }


    private RelayCommand? _leftCommand;
    public RelayCommand LeftCommand
    {
        get
        {
            if (_leftCommand == null)
            {
                _leftCommand = new RelayCommand(param =>
                {
                    LeftCommandAction?.Invoke(param);
                });
            }
            return _leftCommand;
        }
    }


    private RelayCommand? _middleCommand;
    public RelayCommand MiddleCommand
    {
        get
        {
            if (_middleCommand == null)
            {
                _middleCommand = new RelayCommand(param =>
                {
                    MiddleCommandAction?.Invoke(param);
                });
            }
            return _middleCommand;
        }
    }


    private RelayCommand? _upperRightCommand;
    public RelayCommand UpperRightCommand
    {
        get
        {
            if (_upperRightCommand == null)
            {
                _upperRightCommand = new RelayCommand(param =>
                {
                    UpperRightCommandAction?.Invoke(param);
                });
            }
            return _upperRightCommand;
        }
    }


    private RelayCommand? _upperLeftCommand;
    public RelayCommand UpperLeftCommand
    {
        get
        {
            if (_upperLeftCommand == null)
            {
                _upperLeftCommand = new RelayCommand(param =>
                {
                    UpperLeftCommandAction?.Invoke(param);
                });
            }
            return _upperLeftCommand;
        }
    }

    #endregion

    public MapOptionsPresenter(string rightToolTip, string leftToolTip, string middleToolTip,
                                PackIconModernKind? rightSymbol, PackIconModernKind? leftSymbol, PackIconModernKind? middleSymbol)
    {
        RightToolTip = rightToolTip;
        LeftToolTip = leftToolTip;
        MiddleToolTip = middleToolTip;

        RightSymbol = rightSymbol;
        LeftSymbol = leftSymbol;
        MiddleSymbol = middleSymbol;
    }
}
