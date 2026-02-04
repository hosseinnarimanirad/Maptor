using IRI.Maptor.Jab.Common.Assets.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Helpers;

namespace IRI.Maptor.Jab.Common.ViewModels.Map;

public class TextboxMarkerViewModel : Notifier
{
    public static readonly double[] FontSizeOptions = { 9, 10, 11, 12, 14, 16, 18 };

    public static readonly string[] FontFamilyOptions;

    public const string IranSansFontName = "IRANSans";


    private double _formating_FontSize = 11.0;
    public double Formating_FontSize
    {
        get { return _formating_FontSize; }
        set
        {
            _formating_FontSize = value;
            RaisePropertyChanged();
        }
    }

    private string _formating_FontFamilyName = IranSansFontName;
    public string Formating_FontFamilyName
    {
        get { return _formating_FontFamilyName; }
        set
        {
            if (_formating_FontFamilyName == value)
                return;

            _formating_FontFamilyName = value;
            RaisePropertyChanged();

            if (string.Equals(_formating_FontFamilyName, IranSansFontName, StringComparison.OrdinalIgnoreCase))
            {
                Formating_FontFamily = IriFonts.IranSans;
                return;
            }
            try
            {
                Formating_FontFamily = new System.Windows.Media.FontFamily(_formating_FontFamilyName);
            }
            catch
            {
                Formating_FontFamily = IriFonts.IranSans;
            }
        }
    }

    private System.Windows.Media.FontFamily _formating_FontFamily = IriFonts.IranSans;
    public System.Windows.Media.FontFamily Formating_FontFamily
    {
        get { return _formating_FontFamily; }
        set
        {
            _formating_FontFamily = value;
            RaisePropertyChanged();
        }
    }

    private bool _formating_IsBold;
    public bool Formating_IsBold
    {
        get { return _formating_IsBold; }
        set
        {
            _formating_IsBold = value;
            RaisePropertyChanged();
        }
    }

    private bool _formating_IsItalic;
    public bool Formating_IsItalic
    {
        get { return _formating_IsItalic; }
        set
        {
            _formating_IsItalic = value;
            RaisePropertyChanged();
        }
    }

    private bool _formating_IsUnderline;
    public bool Formating_IsUnderline
    {
        get { return _formating_IsUnderline; }
        set
        {
            _formating_IsUnderline = value;
            RaisePropertyChanged();
        }
    }


    private System.Windows.TextAlignment _formating_Alignment;
    public System.Windows.TextAlignment Formating_Alignment
    {
        get { return _formating_Alignment; }
        set
        {
            _formating_Alignment = value;
            RaisePropertyChanged();
        }
    }

    private System.Windows.FlowDirection _formating_FlowDirection = FlowDirection.RightToLeft;
    public System.Windows.FlowDirection Formating_FlowDirection
    {
        get { return _formating_FlowDirection; }
        set
        {
            _formating_FlowDirection = value;
            RaisePropertyChanged();
        }
    }


    private bool _formating_LeftAligned;
    public bool Formating_LeftAligned
    {
        get { return _formating_LeftAligned; }
        set
        {
            _formating_LeftAligned = value;
            RaisePropertyChanged();
        }
    }

    private bool _formating_RightAligned;
    public bool Formating_RightAligned
    {
        get { return _formating_RightAligned; }
        set
        {
            _formating_RightAligned = value;
            RaisePropertyChanged();
        }
    }

    private bool _formating_CenterAligned;
    public bool Formating_CenterAligned
    {
        get { return _formating_CenterAligned; }
        set
        {
            _formating_CenterAligned = value;
            RaisePropertyChanged();
        }
    }


    private string _labelValue = string.Empty;
    public string LabelValue
    {
        get { return _labelValue; }
        set
        {
            _labelValue = value;
            RaisePropertyChanged();
        }
    }

    private string _tooltipValue;
    public string TooltipValue
    {
        get { return _tooltipValue; }
        set
        {
            _tooltipValue = value;
            RaisePropertyChanged();
        }
    }


    private SolidColorBrush? _backgroundColor;
    public SolidColorBrush? BackgroundColor
    {
        get { return _backgroundColor; }
        set
        {
            _backgroundColor = value;
            RaisePropertyChanged();
        }
    }


    static TextboxMarkerViewModel()
    {
        // Initialize font family options: IranSans first (from project resources), then system fonts
        var fontFamilies = new List<string> { IranSansFontName };
        foreach (var fontFamily in System.Windows.Media.Fonts.SystemFontFamilies)
        {
            var name = fontFamily.FamilyNames.Values.FirstOrDefault() ?? fontFamily.Source;
            if (!string.IsNullOrEmpty(name) && !fontFamilies.Contains(name))
            {
                fontFamilies.Add(name);
            }
        }
        fontFamilies.Sort();
        FontFamilyOptions = fontFamilies.ToArray();
    }


    public sealed record BackgroundChoice(string Name, System.Windows.Media.Color? ColorValue, bool IsTheme);

    public static readonly BackgroundChoice[] BackgroundChoices = BuildBackgroundChoices();

    private static BackgroundChoice[] BuildBackgroundChoices()
    {
        var colors = new[]
        {
            ColorHelper.ToWpfColor("#FF000000"),
            ColorHelper.ToWpfColor("#FF61A917"),
            ColorHelper.ToWpfColor("#FFA4C401"),
            ColorHelper.ToWpfColor("#FF008A00"),
            ColorHelper.ToWpfColor("#FF00ACAA"),
            ColorHelper.ToWpfColor("#FF1CA1E2"),
            ColorHelper.ToWpfColor("#FF0050EF"),
            ColorHelper.ToWpfColor("#FF6900FF"),
            ColorHelper.ToWpfColor("#FFAA00FF"),
            ColorHelper.ToWpfColor("#FFF572D0"),
            ColorHelper.ToWpfColor("#FFD80072"),
            ColorHelper.ToWpfColor("#FFA10024"),
            ColorHelper.ToWpfColor("#FFE51400"),
            ColorHelper.ToWpfColor("#FFFA6900"),
            ColorHelper.ToWpfColor("#FFF1A30B"),
            ColorHelper.ToWpfColor("#FFE4C802"),
            ColorHelper.ToWpfColor("#FF835A2C"),
            ColorHelper.ToWpfColor("#FF6D8764"),
            ColorHelper.ToWpfColor("#FF637685"),
            ColorHelper.ToWpfColor("#FF756089"),
            ColorHelper.ToWpfColor("#FF88794E"),
        };

        var list = new List<BackgroundChoice> { new("Theme highlight", null, true) };
        foreach (var c in colors)
            list.Add(new(c.ToString(), c, false));
        return list.ToArray();
    }

    private BackgroundChoice _selectedBackgroundChoice = BackgroundChoices[0];
    public BackgroundChoice SelectedBackgroundChoice
    {
        get { return _selectedBackgroundChoice; }
        set
        {
            _selectedBackgroundChoice = value;
            RaisePropertyChanged();

            if (_selectedBackgroundChoice.IsTheme || _selectedBackgroundChoice.ColorValue == null)
            {
                BackgroundColor = null;
                RaisePropertyChanged(nameof(BackgroundColor));
                return;
            }

            BackgroundColor = new SolidColorBrush(_selectedBackgroundChoice.ColorValue.Value);
        }
    }


    public void IncreaseFontSize()
    {
        var currentSize = Formating_FontSize;

        var largerSizes = FontSizeOptions.Where(s => s > currentSize).ToArray();

        if (largerSizes.Length > 0)
        {
            Formating_FontSize = largerSizes[0];
        }
        else if (currentSize < FontSizeOptions.Max())
        {
            Formating_FontSize = Math.Min(currentSize + 1, FontSizeOptions.Max());
        }
    }

    public void DecreaseFontSize()
    {
        var currentSize = Formating_FontSize;

        var smallerSizes = FontSizeOptions.Where(s => s < currentSize).ToArray();

        if (smallerSizes.Length > 0)
        {
            Formating_FontSize = smallerSizes[smallerSizes.Length - 1];
        }
        else if (currentSize > FontSizeOptions.Min())
        {
            Formating_FontSize = Math.Max(currentSize - 1, FontSizeOptions.Min());
        }
    }


    private RelayCommand? _increaseFontSizeCommand;
    public RelayCommand IncreaseFontSizeCommand
    {
        get
        {
            if (_increaseFontSizeCommand == null)
                _increaseFontSizeCommand = new RelayCommand(param => IncreaseFontSize());

            return _increaseFontSizeCommand;
        }
    }


    private RelayCommand? _decreaseFontSizeCommand;
    public RelayCommand DecreaseFontSizeCommand
    {
        get
        {
            if (_decreaseFontSizeCommand == null)
                _decreaseFontSizeCommand = new RelayCommand(param => DecreaseFontSize());

            return _decreaseFontSizeCommand;
        }
    }


    private RelayCommand? _copyTextCommand;
    public RelayCommand CopyTextCommand
    {
        get
        {
            if (_copyTextCommand == null)
                _copyTextCommand = new RelayCommand(param => ClipboardHelper.CopyText(this.LabelValue));

            return _copyTextCommand;
        }
    }

    private RelayCommand? _clearTextCommand;
    public RelayCommand ClearTextCommand
    {
        get
        {
            if (_clearTextCommand == null)
                _clearTextCommand = new RelayCommand(param => LabelValue = string.Empty);

            return _clearTextCommand;
        }
    }

    private RelayCommand? _deleteCommand;
    public RelayCommand DeleteCommand
    {
        get
        {
            if (_deleteCommand == null)
                _deleteCommand = new RelayCommand(param => RequestDelete?.Invoke());

            return _deleteCommand;
        }
    }

    public Action? RequestDelete { get; set; }
}
