using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace IRI.Maptor.Presentation.Wpf.Behaviors;

/// <summary>
/// Show-password toggle and Caps Lock warning for a <see cref="PasswordBox"/> whose template provides
/// the parts below (<c>IRI.Maptor.Styles.Security.PasswordBox</c> in Controls.SecurityInputs.xaml).
/// <para>
/// The password always lives in the PasswordBox: view models keep reading <see cref="PasswordBox.SecurePassword"/>.
/// While <see cref="IsRevealedProperty"/> is true, the template's <c>PART_RevealedText</c> TextBox mirrors it in
/// plain text and edits made there are written back. The text box is emptied as soon as the password is
/// hidden again, which happens when focus leaves the field or the field unloads, so a revealed password is not
/// left on screen. Copy, cut and drag out of the revealed text are blocked, like the PasswordBox itself.
/// </para>
/// </summary>
public static class PasswordBoxAssist
{
    public const string RevealedTextPartName = "PART_RevealedText";

    #region IsEnabled

    /// <summary>Turns the behaviour on; set by the style.</summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(PasswordBoxAssist), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box)
            return;

        // detach first so a style re-applied with the same value never doubles the handlers
        box.Loaded -= OnLoaded;
        box.Unloaded -= OnUnloaded;
        box.PasswordChanged -= OnPasswordChanged;
        box.IsKeyboardFocusWithinChanged -= OnIsKeyboardFocusWithinChanged;
        box.PreviewKeyDown -= OnKey;
        box.PreviewKeyUp -= OnKey;

        if (e.NewValue is not true)
            return;

        box.Loaded += OnLoaded;
        box.Unloaded += OnUnloaded;
        box.PasswordChanged += OnPasswordChanged;
        box.IsKeyboardFocusWithinChanged += OnIsKeyboardFocusWithinChanged;
        box.PreviewKeyDown += OnKey;
        box.PreviewKeyUp += OnKey;

        if (box.IsLoaded)
            AttachRevealedText(box);
    }

    #endregion

    #region IsRevealed

    /// <summary>True while the password is shown as plain text; bound two-way to the template's toggle.</summary>
    public static readonly DependencyProperty IsRevealedProperty =
        DependencyProperty.RegisterAttached("IsRevealed", typeof(bool), typeof(PasswordBoxAssist),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsRevealedChanged));

    public static bool GetIsRevealed(DependencyObject element) => (bool)element.GetValue(IsRevealedProperty);

    public static void SetIsRevealed(DependencyObject element, bool value) => element.SetValue(IsRevealedProperty, value);

    private static void OnIsRevealedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box || GetRevealedText(box) is not TextBox text)
            return;

        if (e.NewValue is true)
        {
            Sync(box, () => text.Text = box.Password);

            text.CaretIndex = text.Text.Length;

            // the template trigger makes the text box visible in this same pass; focus it once it is
            box.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (GetIsRevealed(box))
                    text.Focus();
            }), DispatcherPriority.Input);
        }
        else
        {
            var hadFocus = text.IsKeyboardFocusWithin;

            Sync(box, () => text.Clear());

            if (hadFocus)
                box.Focus();
        }
    }

    #endregion

    #region IsCapsLockWarningVisible

    /// <summary>True while the field has keyboard focus and Caps Lock is on; the template shows its warning line.</summary>
    public static readonly DependencyProperty IsCapsLockWarningVisibleProperty =
        DependencyProperty.RegisterAttached("IsCapsLockWarningVisible", typeof(bool), typeof(PasswordBoxAssist), new PropertyMetadata(false));

    public static bool GetIsCapsLockWarningVisible(DependencyObject element) => (bool)element.GetValue(IsCapsLockWarningVisibleProperty);

    public static void SetIsCapsLockWarningVisible(DependencyObject element, bool value) => element.SetValue(IsCapsLockWarningVisibleProperty, value);

    private static void UpdateCapsLock(PasswordBox box)
    {
        var visible = box.IsKeyboardFocusWithin && Keyboard.IsKeyToggled(Key.CapsLock);

        if (GetIsCapsLockWarningVisible(box) != visible)
            SetIsCapsLockWarningVisible(box, visible);
    }

    #endregion

    #region Revealed text box

    private static readonly DependencyProperty RevealedTextProperty =
        DependencyProperty.RegisterAttached("RevealedText", typeof(TextBox), typeof(PasswordBoxAssist), new PropertyMetadata(null));

    private static TextBox? GetRevealedText(PasswordBox box) => box.GetValue(RevealedTextProperty) as TextBox;

    // set while the two boxes are copied into each other, so neither change echoes back
    private static readonly DependencyProperty IsSyncingProperty =
        DependencyProperty.RegisterAttached("IsSyncing", typeof(bool), typeof(PasswordBoxAssist), new PropertyMetadata(false));

    private static void Sync(PasswordBox box, Action copy)
    {
        box.SetValue(IsSyncingProperty, true);

        try
        {
            copy();
        }
        finally
        {
            box.SetValue(IsSyncingProperty, false);
        }
    }

    private static bool IsSyncing(PasswordBox box) => (bool)box.GetValue(IsSyncingProperty);

    private static void AttachRevealedText(PasswordBox box)
    {
        box.ApplyTemplate();

        if (box.Template?.FindName(RevealedTextPartName, box) is not TextBox text || ReferenceEquals(GetRevealedText(box), text))
            return;

        if (GetRevealedText(box) is TextBox old)
        {
            old.TextChanged -= OnRevealedTextChanged;
            DataObject.RemoveCopyingHandler(old, OnCopying);
        }

        box.SetValue(RevealedTextProperty, text);

        text.Tag = box;
        text.TextChanged += OnRevealedTextChanged;

        // Ctrl+C, Ctrl+X and dragging the selection out all go through DataObject copying: cancel them
        DataObject.AddCopyingHandler(text, OnCopying);
    }

    private static void OnCopying(object sender, DataObjectCopyingEventArgs e) => e.CancelCommand();

    private static void OnRevealedTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox { Tag: PasswordBox box } text || IsSyncing(box) || !GetIsRevealed(box))
            return;

        Sync(box, () => box.Password = text.Text);
    }

    #endregion

    #region PasswordBox events

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
            AttachRevealedText(box);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box)
            return;

        SetIsRevealed(box, false);
        SetIsCapsLockWarningVisible(box, false);
    }

    // typed while hidden, or cleared by the view (ClearInputValues after a failed sign-in)
    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box || IsSyncing(box) || !GetIsRevealed(box) || GetRevealedText(box) is not TextBox text)
            return;

        Sync(box, () => text.Text = box.Password);
    }

    private static void OnIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not PasswordBox box)
            return;

        // focus left the field (another field, another window, the dialog closing): hide the password again
        if (e.NewValue is false)
            SetIsRevealed(box, false);

        UpdateCapsLock(box);
    }

    private static void OnKey(object sender, KeyEventArgs e)
    {
        if (sender is PasswordBox box)
            UpdateCapsLock(box);
    }

    #endregion
}
