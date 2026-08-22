using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.ColorBrushes;
using IRI.Maptor.Presentation.Wpf.Assets.Fonts;
using IRI.Maptor.Presentation.Core;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Map;

public class TextboxMarkerViewModel : Notifier
{
    public static readonly double[] FontSizeOptions = { 9, 10, 11, 12, 14, 16, 18, 20 };

    public static readonly string[] FontFamilyOptions;

    public const string IranSansFontName = "IRANSans";
    private const string TimesNewRoman = "TimesNewRoman";

    private double _formatting_FontSize = 14.0;
    public double Formatting_FontSize
    {
        get { return _formatting_FontSize; }
        set
        {
            _formatting_FontSize = value;
            RaisePropertyChanged();
        }
    }

    private string _formatting_FontFamilyName = IranSansFontName;// IranSansFontName;
    public string Formatting_FontFamilyName
    {
        get { return _formatting_FontFamilyName; }
        set
        {
            if (_formatting_FontFamilyName == value)
                return;

            _formatting_FontFamilyName = value;
            RaisePropertyChanged();

            if (string.Equals(_formatting_FontFamilyName, IranSansFontName, StringComparison.OrdinalIgnoreCase))
            {
                Formatting_FontFamily = IriFonts.IranSans;
                return;
            }

            try
            {
                Formatting_FontFamily = new System.Windows.Media.FontFamily(_formatting_FontFamilyName);
            }
            catch
            {
                Formatting_FontFamily = IriFonts.IranSans;
            }
        }
    }

    // initialized together with _formatting_FontFamilyName: the name setter's equality
    // guard skips re-deriving the family when the name is assigned its own initial value
    // (as the constructor does), so a null here would leave the TextBox on the default font
    private FontFamily _formatting_FontFamily = IriFonts.IranSans;
    public FontFamily Formatting_FontFamily
    {
        get { return _formatting_FontFamily; }
        set
        {
            _formatting_FontFamily = value;
            RaisePropertyChanged();
        }
    }

    private bool _formatting_IsBold;
    public bool Formatting_IsBold
    {
        get { return _formatting_IsBold; }
        set
        {
            _formatting_IsBold = value;
            RaisePropertyChanged();
        }
    }

    private bool _formatting_IsItalic;
    public bool Formatting_IsItalic
    {
        get { return _formatting_IsItalic; }
        set
        {
            _formatting_IsItalic = value;
            RaisePropertyChanged();
        }
    }

    private bool _formatting_IsUnderline;
    public bool Formatting_IsUnderline
    {
        get { return _formatting_IsUnderline; }
        set
        {
            _formatting_IsUnderline = value;
            RaisePropertyChanged();
        }
    }


    private System.Windows.TextAlignment _formatting_Alignment;
    public System.Windows.TextAlignment Formatting_Alignment
    {
        get { return _formatting_Alignment; }
        set
        {
            _formatting_Alignment = value;
            RaisePropertyChanged();
        }
    }

    private System.Windows.FlowDirection _formatting_FlowDirection = FlowDirection.RightToLeft;
    public System.Windows.FlowDirection Formatting_FlowDirection
    {
        get { return _formatting_FlowDirection; }
        set
        {
            _formatting_FlowDirection = value;
            RaisePropertyChanged();
        }
    }


    private bool _formatting_LeftAligned;
    public bool Formatting_LeftAligned
    {
        get { return _formatting_LeftAligned; }
        set
        {
            _formatting_LeftAligned = value;
            RaisePropertyChanged();
        }
    }

    private bool _formatting_RightAligned;
    public bool Formatting_RightAligned
    {
        get { return _formatting_RightAligned; }
        set
        {
            _formatting_RightAligned = value;
            RaisePropertyChanged();
        }
    }

    private bool _formatting_CenterAligned;
    public bool Formatting_CenterAligned
    {
        get { return _formatting_CenterAligned; }
        set
        {
            _formatting_CenterAligned = value;
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

    private string _tooltipValue = string.Empty;
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

    public TextboxMarkerViewModel()
    {
        Formatting_FontFamilyName = IranSansFontName;

    }


    public sealed record BackgroundChoice(string Name, System.Windows.Media.Color? ColorValue, bool IsTheme);

    public static readonly BackgroundChoice[] BackgroundChoices = BuildBackgroundChoices();

    private static BackgroundChoice[] BuildBackgroundChoices()
    {
        // Replaced inline hex definitions with ModernUIColors to avoid duplication.
        // Original: ColorHelper.ToWpfColor("#FF...") for each of the 21 colors.
        var colors = new[]
        {
            ModernUIColors.BlackColor,
            ModernUIColors.LimeGreenColor,
            ModernUIColors.LimeColor,
            ModernUIColors.GreenColor,
            ModernUIColors.TealColor,
            ModernUIColors.CyanColor,
            ModernUIColors.BlueColor,
            ModernUIColors.IndigoColor,
            ModernUIColors.VioletColor,
            ModernUIColors.PinkColor,
            ModernUIColors.MagentaColor,
            ModernUIColors.CrimsonColor,
            ModernUIColors.RedColor,
            ModernUIColors.OrangeColor,
            ModernUIColors.AmberColor,
            ModernUIColors.YellowColor,
            ModernUIColors.BrownColor,
            ModernUIColors.OliveColor,
            ModernUIColors.SteelColor,
            ModernUIColors.MauveColor,
            ModernUIColors.TaupeColor,
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
        var currentSize = Formatting_FontSize;

        var largerSizes = FontSizeOptions.Where(s => s > currentSize).ToArray();

        if (largerSizes.Length > 0)
        {
            Formatting_FontSize = largerSizes[0];
        }
        else if (currentSize < FontSizeOptions.Max())
        {
            Formatting_FontSize = Math.Min(currentSize + 1, FontSizeOptions.Max());
        }
    }

    public void DecreaseFontSize()
    {
        var currentSize = Formatting_FontSize;

        var smallerSizes = FontSizeOptions.Where(s => s < currentSize).ToArray();

        if (smallerSizes.Length > 0)
        {
            Formatting_FontSize = smallerSizes[smallerSizes.Length - 1];
        }
        else if (currentSize > FontSizeOptions.Min())
        {
            Formatting_FontSize = Math.Max(currentSize - 1, FontSizeOptions.Min());
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
