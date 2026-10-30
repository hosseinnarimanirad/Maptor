using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

using IRI.Maptor.Presentation.Wpf.Helpers;
using IRI.Maptor.Presentation.Wpf.ColorBrushes;
using IRI.Maptor.Presentation.Wpf.Assets.Fonts;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models.MapText;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Map;

public class TextboxMarkerViewModel : Notifier
{
    // ascending; IncreaseFontSize/DecreaseFontSize step through it
    public static readonly double[] FontSizeOptions = { 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72 };

    public static readonly string[] FontFamilyOptions;

    public const string IranSansFontName = "IRANSans";

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


    // the TextBox's own TextAlignment, relative to its flow direction: under RightToLeft
    // WPF mirrors it, so Left shows on the right edge. The toolbar buttons bind to the
    // on-screen Formatting_*Aligned properties below instead.
    private System.Windows.TextAlignment _formatting_Alignment;
    public System.Windows.TextAlignment Formatting_Alignment
    {
        get { return _formatting_Alignment; }
        set
        {
            _formatting_Alignment = value;
            RaisePropertyChanged();
            RaiseScreenAlignmentChanged();
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
            RaiseFlowDirectionButtonsChanged();
            RaiseScreenAlignmentChanged();
        }
    }

    // LTR/RTL toolbar toggle buttons; same contract as the alignment buttons below:
    // false (clicking the already-checked button) is ignored and re-notified
    public bool Formatting_IsLeftToRight
    {
        get { return Formatting_FlowDirection == FlowDirection.LeftToRight; }
        set { SetFlowDirection(FlowDirection.LeftToRight, value); }
    }

    public bool Formatting_IsRightToLeft
    {
        get { return Formatting_FlowDirection == FlowDirection.RightToLeft; }
        set { SetFlowDirection(FlowDirection.RightToLeft, value); }
    }

    private void SetFlowDirection(FlowDirection flowDirection, bool isChecked)
    {
        if (isChecked)
            Formatting_FlowDirection = flowDirection;
        else
            RaiseFlowDirectionButtonsChanged();
    }

    private void RaiseFlowDirectionButtonsChanged()
    {
        RaisePropertyChanged(nameof(Formatting_IsLeftToRight));
        RaisePropertyChanged(nameof(Formatting_IsRightToLeft));
    }


    // on-screen alignment for the toolbar toggle buttons; true picks that alignment,
    // false (clicking the already-checked button) is ignored and re-notified so the
    // button stays checked
    public bool Formatting_LeftAligned
    {
        get { return Formatting_Alignment == ToTextAlignment(TextAlignment.Left); }
        set { SetScreenAlignment(TextAlignment.Left, value); }
    }

    public bool Formatting_RightAligned
    {
        get { return Formatting_Alignment == ToTextAlignment(TextAlignment.Right); }
        set { SetScreenAlignment(TextAlignment.Right, value); }
    }

    public bool Formatting_CenterAligned
    {
        get { return Formatting_Alignment == TextAlignment.Center; }
        set { SetScreenAlignment(TextAlignment.Center, value); }
    }

    // on-screen left/right → the TextAlignment that renders there under the current flow direction
    private TextAlignment ToTextAlignment(TextAlignment screenAlignment)
    {
        if (Formatting_FlowDirection != FlowDirection.RightToLeft)
            return screenAlignment;

        return screenAlignment switch
        {
            TextAlignment.Left => TextAlignment.Right,
            TextAlignment.Right => TextAlignment.Left,
            _ => screenAlignment,
        };
    }

    private void SetScreenAlignment(TextAlignment screenAlignment, bool isChecked)
    {
        if (isChecked)
            Formatting_Alignment = ToTextAlignment(screenAlignment);
        else
            RaiseScreenAlignmentChanged();
    }

    private void RaiseScreenAlignmentChanged()
    {
        RaisePropertyChanged(nameof(Formatting_LeftAligned));
        RaisePropertyChanged(nameof(Formatting_RightAligned));
        RaisePropertyChanged(nameof(Formatting_CenterAligned));
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


    private MapTextFrame _formatting_Frame = MapTextFrame.Box;
    public MapTextFrame Formatting_Frame
    {
        get { return _formatting_Frame; }
        set
        {
            _formatting_Frame = value;
            RaisePropertyChanged();
            RaiseFrameButtonsChanged();
        }
    }

    // Box/Plain/Halo toolbar toggle buttons; same contract as the direction buttons:
    // false (clicking the already-checked button) is ignored and re-notified
    public bool Formatting_IsBoxFrame
    {
        get { return Formatting_Frame == MapTextFrame.Box; }
        set { SetFrame(MapTextFrame.Box, value); }
    }

    public bool Formatting_IsPlainFrame
    {
        get { return Formatting_Frame == MapTextFrame.Plain; }
        set { SetFrame(MapTextFrame.Plain, value); }
    }

    public bool Formatting_IsHaloFrame
    {
        get { return Formatting_Frame == MapTextFrame.Halo; }
        set { SetFrame(MapTextFrame.Halo, value); }
    }

    private void SetFrame(MapTextFrame frame, bool isChecked)
    {
        if (isChecked)
            Formatting_Frame = frame;
        else
            RaiseFrameButtonsChanged();
    }

    private void RaiseFrameButtonsChanged()
    {
        RaisePropertyChanged(nameof(Formatting_IsBoxFrame));
        RaisePropertyChanged(nameof(Formatting_IsPlainFrame));
        RaisePropertyChanged(nameof(Formatting_IsHaloFrame));
    }


    private Color _formatting_TextColor = Colors.Black;
    public Color Formatting_TextColor
    {
        get { return _formatting_TextColor; }
        set
        {
            _formatting_TextColor = value;
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
        var list = new List<BackgroundChoice>
        {
            new("Theme highlight", null, true),
            new("Black", ModernUIColors.BlackColor, false),
            new("Lime Green", ModernUIColors.LimeGreenColor, false),
            new("Lime", ModernUIColors.LimeColor, false),
            new("Green", ModernUIColors.GreenColor, false),
            new("Teal", ModernUIColors.TealColor, false),
            new("Cyan", ModernUIColors.CyanColor, false),
            new("Blue", ModernUIColors.BlueColor, false),
            new("Indigo", ModernUIColors.IndigoColor, false),
            new("Violet", ModernUIColors.VioletColor, false),
            new("Pink", ModernUIColors.PinkColor, false),
            new("Magenta", ModernUIColors.MagentaColor, false),
            new("Crimson", ModernUIColors.CrimsonColor, false),
            new("Red", ModernUIColors.RedColor, false),
            new("Orange", ModernUIColors.OrangeColor, false),
            new("Amber", ModernUIColors.AmberColor, false),
            new("Yellow", ModernUIColors.YellowColor, false),
            new("Brown", ModernUIColors.BrownColor, false),
            new("Olive", ModernUIColors.OliveColor, false),
            new("Steel", ModernUIColors.SteelColor, false),
            new("Mauve", ModernUIColors.MauveColor, false),
            new("Taupe", ModernUIColors.TaupeColor, false),
        };

        return list.ToArray();
    }

    public sealed record TextColorChoice(string Name, Color ColorValue);

    // the toolbar combo binds SelectedValue (ColorValue) to Formatting_TextColor; a colour
    // outside this list (e.g. from ApplyStyle) still applies, the combo just shows no item
    public static readonly TextColorChoice[] TextColorChoices =
    [
        new("Black", Colors.Black),
        new("White", Colors.White),
        .. BackgroundChoices.Where(c => !c.IsTheme && c.ColorValue != ModernUIColors.BlackColor)
                            .Select(c => new TextColorChoice(c.Name, c.ColorValue!.Value)),
    ];

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


    #region Style

    public MapTextStyle ToStyle()
    {
        return new MapTextStyle
        {
            Frame = Formatting_Frame,
            FontFamilyName = Formatting_FontFamilyName,
            FontSize = Formatting_FontSize,
            IsBold = Formatting_IsBold,
            IsItalic = Formatting_IsItalic,
            IsUnderline = Formatting_IsUnderline,
            Alignment = ToStyleAlignment(Formatting_Alignment),
            IsRightToLeft = Formatting_FlowDirection == FlowDirection.RightToLeft,
            TextColor = Formatting_TextColor.ToString(),
            BackgroundColor = SelectedBackgroundChoice.IsTheme ? null : SelectedBackgroundChoice.ColorValue?.ToString(),
        };
    }

    // values that cannot be used (unparsable colour, non-positive size, background
    // colour not among BackgroundChoices) fall back to the defaults instead of throwing
    public void ApplyStyle(MapTextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        Formatting_Frame = Enum.IsDefined(style.Frame) ? style.Frame : MapTextFrame.Box;
        Formatting_FontFamilyName = string.IsNullOrWhiteSpace(style.FontFamilyName) ? IranSansFontName : style.FontFamilyName;
        Formatting_FontSize = double.IsFinite(style.FontSize) && style.FontSize > 0 ? style.FontSize : 14.0;
        Formatting_IsBold = style.IsBold;
        Formatting_IsItalic = style.IsItalic;
        Formatting_IsUnderline = style.IsUnderline;
        Formatting_FlowDirection = style.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Formatting_Alignment = ToTextAlignment(style.Alignment);
        Formatting_TextColor = TryParseColor(style.TextColor) ?? Colors.Black;

        var backgroundColor = TryParseColor(style.BackgroundColor);
        SelectedBackgroundChoice = BackgroundChoices.FirstOrDefault(c => !c.IsTheme && c.ColorValue == backgroundColor && backgroundColor is not null)
            ?? BackgroundChoices[0];
    }

    private static MapTextAlignment ToStyleAlignment(TextAlignment alignment)
    {
        return alignment switch
        {
            TextAlignment.Right => MapTextAlignment.Right,
            TextAlignment.Center => MapTextAlignment.Center,
            TextAlignment.Justify => MapTextAlignment.Justify,
            _ => MapTextAlignment.Left,
        };
    }

    private static TextAlignment ToTextAlignment(MapTextAlignment alignment)
    {
        return alignment switch
        {
            MapTextAlignment.Right => TextAlignment.Right,
            MapTextAlignment.Center => TextAlignment.Center,
            MapTextAlignment.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };
    }

    private static Color? TryParseColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            return null;

        try
        {
            return ColorHelper.ToWpfColor(hexColor);
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException)
        {
            return null;
        }
    }

    #endregion


    public void IncreaseFontSize()
    {
        var currentSize = Formatting_FontSize;

        // no-op at the largest option
        foreach (var size in FontSizeOptions)
        {
            if (size > currentSize)
            {
                Formatting_FontSize = size;
                return;
            }
        }
    }

    public void DecreaseFontSize()
    {
        var currentSize = Formatting_FontSize;

        // no-op at the smallest option
        for (int i = FontSizeOptions.Length - 1; i >= 0; i--)
        {
            if (FontSizeOptions[i] < currentSize)
            {
                Formatting_FontSize = FontSizeOptions[i];
                return;
            }
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
