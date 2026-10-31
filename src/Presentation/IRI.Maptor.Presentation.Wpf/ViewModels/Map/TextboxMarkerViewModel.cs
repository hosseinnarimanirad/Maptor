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
            if (_formatting_TextColor == value)
                return;

            _formatting_TextColor = value;
            RaisePropertyChanged();

            // a halo colour chosen for the old text colour (e.g. read from a saved SLD) could match
            // the new one and vanish; picking a text colour goes back to the automatic contrast
            Formatting_HaloColor = null;
        }
    }


    // null = automatic halo colour (contrasting with the text); set from a saved style
    private Color? _formatting_HaloColor;
    public Color? Formatting_HaloColor
    {
        get { return _formatting_HaloColor; }
        set
        {
            if (_formatting_HaloColor == value)
                return;

            _formatting_HaloColor = value;
            RaisePropertyChanged();
        }
    }


    // the label's width range in px; the Box frame also keeps a larger minimum of its own (XAML)
    public const double MinTextWidth = 40;
    public const double MaxTextWidth = 600;

    // degrees, clockwise on screen, around the anchor (bottom-centre); kept in (-180, 180]
    private double _formatting_Rotation;
    public double Formatting_Rotation
    {
        get { return _formatting_Rotation; }
        set
        {
            var rotation = NormalizeRotation(value);

            if (_formatting_Rotation == rotation)
                return;

            _formatting_Rotation = rotation;
            RaisePropertyChanged();
        }
    }

    // null = automatic: the label grows with its text up to MaxTextWidth; else the text wraps
    // inside this width (px), kept within MinTextWidth..MaxTextWidth
    private double? _formatting_TextWidth;
    public double? Formatting_TextWidth
    {
        get { return _formatting_TextWidth; }
        set
        {
            var width = value is { } w && double.IsFinite(w) ? Math.Clamp(w, MinTextWidth, MaxTextWidth) : (double?)null;

            if (_formatting_TextWidth == width)
                return;

            _formatting_TextWidth = width;
            RaisePropertyChanged();
        }
    }

    // a non-finite angle is upright; -180 is shown as 180
    private static double NormalizeRotation(double degrees)
    {
        if (!double.IsFinite(degrees))
            return 0;

        var rotation = degrees % 360;

        if (rotation > 180)
            rotation -= 360;
        else if (rotation <= -180)
            rotation += 360;

        return rotation;
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


    // an entry of the background and text colour lists. NameKey: resource key of the display name
    // (textMarker_color_*), looked up in the view so it follows a language change. IsTheme: the
    // theme highlight brush (background list only), ColorValue is then null
    public sealed record ColorChoice(string NameKey, System.Windows.Media.Color? ColorValue, bool IsTheme);

    public static readonly ColorChoice[] BackgroundChoices = BuildBackgroundChoices();

    private static ColorChoice[] BuildBackgroundChoices()
    {
        var list = new List<ColorChoice>
        {
            new("textMarker_color_themeHighlight", null, true),
            new("textMarker_color_black", ModernUIColors.BlackColor, false),
            new("textMarker_color_limeGreen", ModernUIColors.LimeGreenColor, false),
            new("textMarker_color_lime", ModernUIColors.LimeColor, false),
            new("textMarker_color_green", ModernUIColors.GreenColor, false),
            new("textMarker_color_teal", ModernUIColors.TealColor, false),
            new("textMarker_color_cyan", ModernUIColors.CyanColor, false),
            new("textMarker_color_blue", ModernUIColors.BlueColor, false),
            new("textMarker_color_indigo", ModernUIColors.IndigoColor, false),
            new("textMarker_color_violet", ModernUIColors.VioletColor, false),
            new("textMarker_color_pink", ModernUIColors.PinkColor, false),
            new("textMarker_color_magenta", ModernUIColors.MagentaColor, false),
            new("textMarker_color_crimson", ModernUIColors.CrimsonColor, false),
            new("textMarker_color_red", ModernUIColors.RedColor, false),
            new("textMarker_color_orange", ModernUIColors.OrangeColor, false),
            new("textMarker_color_amber", ModernUIColors.AmberColor, false),
            new("textMarker_color_yellow", ModernUIColors.YellowColor, false),
            new("textMarker_color_brown", ModernUIColors.BrownColor, false),
            new("textMarker_color_olive", ModernUIColors.OliveColor, false),
            new("textMarker_color_steel", ModernUIColors.SteelColor, false),
            new("textMarker_color_mauve", ModernUIColors.MauveColor, false),
            new("textMarker_color_taupe", ModernUIColors.TaupeColor, false),
        };

        return list.ToArray();
    }

    // the toolbar combo binds SelectedValue (ColorValue) to Formatting_TextColor; a colour
    // outside this list (e.g. from ApplyStyle) still applies, the combo just shows no item
    public static readonly ColorChoice[] TextColorChoices =
    [
        new("textMarker_color_black", Colors.Black, false),
        new("textMarker_color_white", Colors.White, false),
        .. BackgroundChoices.Where(c => !c.IsTheme && c.ColorValue != ModernUIColors.BlackColor),
    ];

    private ColorChoice _selectedBackgroundChoice = BackgroundChoices[0];
    public ColorChoice SelectedBackgroundChoice
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
            HaloColor = Formatting_HaloColor?.ToString(),
            BackgroundColor = SelectedBackgroundChoice.IsTheme ? null : SelectedBackgroundChoice.ColorValue?.ToString(),
            Rotation = Formatting_Rotation,
            Width = Formatting_TextWidth,
        };
    }

    // values that cannot be used (unparsable colour, non-positive size, background
    // colour not among BackgroundChoices) fall back to the defaults instead of throwing;
    // rotation and width are normalized and clamped by their setters
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
        Formatting_TextColor = ColorHelper.TryToWpfColor(style.TextColor) ?? Colors.Black;
        // after the text colour, whose setter resets it
        Formatting_HaloColor = ColorHelper.TryToWpfColor(style.HaloColor);

        var backgroundColor = ColorHelper.TryToWpfColor(style.BackgroundColor);
        SelectedBackgroundChoice = BackgroundChoices.FirstOrDefault(c => !c.IsTheme && c.ColorValue == backgroundColor && backgroundColor is not null)
            ?? BackgroundChoices[0];

        Formatting_Rotation = style.Rotation;
        Formatting_TextWidth = style.Width;
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


    private RelayCommand? _resetRotationCommand;
    public RelayCommand ResetRotationCommand
    {
        get
        {
            if (_resetRotationCommand == null)
                _resetRotationCommand = new RelayCommand(param => Formatting_Rotation = 0);

            return _resetRotationCommand;
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
