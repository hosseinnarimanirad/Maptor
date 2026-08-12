using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Jab.Controls.Dialogs;

/// <summary>
/// The action row shared by every dialog: a divider, then a right-aligned
/// [secondary][primary] pair. Keeps button styling, icon+label layout and the
/// IsDefault/IsCancel wiring in one place instead of copy-pasted per dialog.
/// </summary>
public partial class DialogFooterView : UserControl
{
    public DialogFooterView()
    {
        InitializeComponent();
    }

    public ICommand? PrimaryCommand
    {
        get { return (ICommand?)GetValue(PrimaryCommandProperty); }
        set { SetValue(PrimaryCommandProperty, value); }
    }

    public static readonly DependencyProperty PrimaryCommandProperty =
        DependencyProperty.Register(nameof(PrimaryCommand), typeof(ICommand), typeof(DialogFooterView), new PropertyMetadata(null));


    public object? PrimaryCommandParameter
    {
        get { return GetValue(PrimaryCommandParameterProperty); }
        set { SetValue(PrimaryCommandParameterProperty, value); }
    }

    public static readonly DependencyProperty PrimaryCommandParameterProperty =
        DependencyProperty.Register(nameof(PrimaryCommandParameter), typeof(object), typeof(DialogFooterView), new PropertyMetadata(null));


    public string? PrimaryText
    {
        get { return (string?)GetValue(PrimaryTextProperty); }
        set { SetValue(PrimaryTextProperty, value); }
    }

    public static readonly DependencyProperty PrimaryTextProperty =
        DependencyProperty.Register(nameof(PrimaryText), typeof(string), typeof(DialogFooterView), new PropertyMetadata(null));


    public PackIconMaterialKind PrimaryIconKind
    {
        get { return (PackIconMaterialKind)GetValue(PrimaryIconKindProperty); }
        set { SetValue(PrimaryIconKindProperty, value); }
    }

    public static readonly DependencyProperty PrimaryIconKindProperty =
        DependencyProperty.Register(nameof(PrimaryIconKind), typeof(PackIconMaterialKind), typeof(DialogFooterView), new PropertyMetadata(PackIconMaterialKind.CheckBold));


    public ICommand? SecondaryCommand
    {
        get { return (ICommand?)GetValue(SecondaryCommandProperty); }
        set { SetValue(SecondaryCommandProperty, value); }
    }

    public static readonly DependencyProperty SecondaryCommandProperty =
        DependencyProperty.Register(nameof(SecondaryCommand), typeof(ICommand), typeof(DialogFooterView), new PropertyMetadata(null));


    public string? SecondaryText
    {
        get { return (string?)GetValue(SecondaryTextProperty); }
        set { SetValue(SecondaryTextProperty, value); }
    }

    public static readonly DependencyProperty SecondaryTextProperty =
        DependencyProperty.Register(nameof(SecondaryText), typeof(string), typeof(DialogFooterView), new PropertyMetadata(null));


    public PackIconMaterialKind SecondaryIconKind
    {
        get { return (PackIconMaterialKind)GetValue(SecondaryIconKindProperty); }
        set { SetValue(SecondaryIconKindProperty, value); }
    }

    public static readonly DependencyProperty SecondaryIconKindProperty =
        DependencyProperty.Register(nameof(SecondaryIconKind), typeof(PackIconMaterialKind), typeof(DialogFooterView), new PropertyMetadata(PackIconMaterialKind.CloseThick));


    /// <summary>
    /// Optional status line shown opposite the buttons (e.g. "3 users loaded").
    /// </summary>
    public string? StatusText
    {
        get { return (string?)GetValue(StatusTextProperty); }
        set { SetValue(StatusTextProperty, value); }
    }

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(DialogFooterView), new PropertyMetadata(null));


    /// <summary>
    /// Only needed when the command itself carries no CanExecute.
    /// </summary>
    public bool IsPrimaryEnabled
    {
        get { return (bool)GetValue(IsPrimaryEnabledProperty); }
        set { SetValue(IsPrimaryEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsPrimaryEnabledProperty =
        DependencyProperty.Register(nameof(IsPrimaryEnabled), typeof(bool), typeof(DialogFooterView), new PropertyMetadata(true));


    /// <summary>
    /// Only needed when the command itself carries no CanExecute.
    /// </summary>
    public bool IsSecondaryEnabled
    {
        get { return (bool)GetValue(IsSecondaryEnabledProperty); }
        set { SetValue(IsSecondaryEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsSecondaryEnabledProperty =
        DependencyProperty.Register(nameof(IsSecondaryEnabled), typeof(bool), typeof(DialogFooterView), new PropertyMetadata(true));


    /// <summary>
    /// False for dialogs whose only action is Close (nothing to cancel).
    /// </summary>
    public bool ShowSecondary
    {
        get { return (bool)GetValue(ShowSecondaryProperty); }
        set { SetValue(ShowSecondaryProperty, value); }
    }

    public static readonly DependencyProperty ShowSecondaryProperty =
        DependencyProperty.Register(nameof(ShowSecondary), typeof(bool), typeof(DialogFooterView), new PropertyMetadata(true));


    /// <summary>
    /// Close-only dialogs leave <see cref="PrimaryCommand"/> unset and the button simply
    /// closes the window it lives in. Done here rather than via IsCancel so it also works
    /// for windows opened with Show() instead of ShowDialog().
    /// </summary>
    private void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
    {
        if (PrimaryCommand is not null)
            return;

        Window.GetWindow(this)?.Close();
    }
}
