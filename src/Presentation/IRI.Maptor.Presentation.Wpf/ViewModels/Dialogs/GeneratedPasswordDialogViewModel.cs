using System;
using System.Windows.Input;

using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Wpf.Helpers;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Dialogs;

/// <summary>
/// Hands an administrator a password the server just generated (an account's password reset). The
/// password exists only in that answer, so the dialog shows it selectable and offers a Copy button; it is
/// put on the clipboard only when that button is pressed, kept out of clipboard history, and cleared again
/// after <see cref="ClipboardClearDelay"/> (<see cref="ClipboardHelper.CopySensitiveText"/>).
/// </summary>
public class GeneratedPasswordDialogViewModel : DialogViewModelBase
{
    public static readonly TimeSpan ClipboardClearDelay = TimeSpan.FromSeconds(60);

    public GeneratedPasswordDialogViewModel(string userName, string password)
    {
        UserName = userName ?? string.Empty;

        Password = password ?? string.Empty;

        CopyCommand = new RelayCommand(_ => Copy(), _ => Password.Length > 0);

        CloseCommand = new RelayCommand(_ => DialogResult = true);
    }

    public string UserName { get; }

    public string Password { get; }

    /// <summary>"The password of {user} has been reset. Give the user this new password:"</summary>
    public string Message => string.Format(LocalizationManager.Instance["dialog_generatedPassword_message"], UserName);

    private bool _isCopied;
    public bool IsCopied
    {
        get => _isCopied;
        private set
        {
            if (_isCopied == value)
                return;

            _isCopied = value;
            RaisePropertyChanged();
        }
    }

    private bool _copyFailed;
    /// <summary>The clipboard was locked by another process; the password can still be selected and copied by hand.</summary>
    public bool CopyFailed
    {
        get => _copyFailed;
        private set
        {
            if (_copyFailed == value)
                return;

            _copyFailed = value;
            RaisePropertyChanged();
        }
    }

    public ICommand CopyCommand { get; }

    public ICommand CloseCommand { get; }

    private void Copy()
    {
        var copied = ClipboardHelper.CopySensitiveText(Password, ClipboardClearDelay);

        IsCopied = copied;

        CopyFailed = !copied;
    }
}
