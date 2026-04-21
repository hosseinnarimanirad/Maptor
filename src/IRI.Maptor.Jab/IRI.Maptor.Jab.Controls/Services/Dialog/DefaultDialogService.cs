using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Jab.Common.ViewModels.Dialogs;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;

namespace IRI.Maptor.Jab.Controls.Services.Dialog;

/// <summary>
/// Default implementation of IDialogService that provides dialog functionality with automatic window resolution.
/// </summary>
public class DefaultDialogService : IDialogService
{
    private const int DefaultBlurRadius = 5;

    /// <summary>
    /// Gets or sets the blur radius applied to the owner window when showing dialogs.
    /// </summary>
    public int BlurRadius { get; set; } = DefaultBlurRadius;

    private readonly object? _defaultOwnerWindow;

    /// <summary>
    /// Initializes a new instance of the DefaultDialogService class.
    /// </summary>
    /// <param name="defaultOwnerWindow">The default owner window to use when no owner is specified.</param>
    public DefaultDialogService(object? defaultOwnerWindow)
    {
        _defaultOwnerWindow = defaultOwnerWindow;
    }

    #region Helper Methods


    /// <summary>
    /// Gets the owner window of the specified type from the application's windows.
    /// </summary>
    /// <typeparam name="T">The type of window to find.</typeparam>
    /// <returns>The first window of the specified type, or null if not found.</returns>
    private Window? GetOwnerWindowByType<T>()
    {
#pragma warning disable CS8978 // 'T' cannot be made nullable - this is a false positive, we're casting to Window, not making T nullable
        var windows = Application.Current?.Windows;
        if (windows == null) return null;
        return windows.OfType<T>().FirstOrDefault() as Window;
#pragma warning restore CS8978
    }

    /// <summary>
    /// Resolves the owner window using multiple fallback strategies:
    /// 1. Use provided owner window (if not null)
    /// 2. Use default owner window (from constructor)
    /// 3. Use Application.Current.MainWindow
    /// 4. Use active/foreground window
    /// 5. Use first open window
    /// </summary>
    /// <param name="ownerWindow">The owner window parameter provided by the caller, or null.</param>
    /// <returns>The resolved owner window, or null if no window could be found.</returns>
    private Window? ResolveOwnerWindow(object? ownerWindow)
    {
        // Strategy 1: Use provided owner window
        if (ownerWindow is Window window)
            return window;

        // Strategy 2: Use default owner window
        if (_defaultOwnerWindow is Window defaultWindow)
            return defaultWindow;

        // Strategy 3: Use Application.Current.MainWindow
        // Strategy 4: Use active/foreground window
        // Strategy 5: Use first open window
        return Application.Current?.MainWindow
            ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault();
    }

    /// <summary>
    /// Gets the appropriate dispatcher for UI operations.
    /// Prefers the owner window's dispatcher, falls back to Application.Current.Dispatcher.
    /// </summary>
    /// <param name="owner">The owner window, or null.</param>
    /// <returns>The dispatcher to use for UI operations.</returns>
    private System.Windows.Threading.Dispatcher GetDispatcher(Window? owner)
    {
        if (owner != null && owner.Dispatcher != null)
            return owner.Dispatcher;

        return Application.Current?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// Applies blur effect to the owner window and returns a disposable that restores the original effect.
    /// Ensures proper cleanup even if exceptions occur.
    /// </summary>
    /// <param name="owner">The owner window to apply blur effect to, or null.</param>
    /// <returns>A disposable that restores the original effect when disposed, or null if owner is null.</returns>
    private IDisposable? ApplyBlurEffect(Window? owner)
    {
        if (owner == null)
            return null;

        var originalEffect = owner.Effect;

        owner.Effect = new BlurEffect() { Radius = BlurRadius };

        return new BlurEffectDisposable(owner, originalEffect);
    }

    /// <summary>
    /// Shows a custom dialog asynchronously with common setup pattern.
    /// Handles owner window resolution, blur effect, event wiring, and exception handling.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the dialog.</typeparam>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="dialog">The window to show as a dialog.</param>
    /// <param name="viewModel">The dialog view model.</param>
    /// <param name="getResult">Function to extract the result from the view model when dialog closes.</param>
    /// <param name="setupViewModel">Optional action to set up the view model (e.g., RequestClose).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private Task<TResult> ShowCustomDialogAsync<TResult>(
        object? ownerWindow,
        Window dialog,
        object viewModel,
        Func<object, TResult> getResult,
        Action<object>? setupViewModel = null)
    {
        var tcs = new TaskCompletionSource<TResult>();
        var owner = ResolveOwnerWindow(ownerWindow);

        if (owner != null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        var blurDisposable = ApplyBlurEffect(owner);

        dialog.Closed += (sender, e) =>
        {
            blurDisposable?.Dispose();
            tcs.SetResult(getResult(viewModel));
        };

        setupViewModel?.Invoke(viewModel);

        dialog.DataContext = viewModel;

        try
        {
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            blurDisposable?.Dispose();
            tcs.SetException(ex);
        }

        return tcs.Task;
    }

    #endregion

    #region Open File Dialog

    /// <summary>
    /// Shows an open file dialog, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <returns>The selected file path, or null if the user cancelled.</returns>
    public string? ShowOpenFileDialog<T>(string filter)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowOpenFileDialog(filter, owner);
    }

    /// <summary>
    /// Shows an open file dialog with the specified owner window.
    /// </summary>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <returns>The selected file path, or null if the user cancelled.</returns>
    public string? ShowOpenFileDialog(string filter, object? ownerWindow = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        var owner = ResolveOwnerWindow(ownerWindow);
        OpenFileDialog dialog = new OpenFileDialog()
        {
            Filter = filter,
            Multiselect = false,
            Title = LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_openfile_title)]
        };

        string? result = null;

        using (ApplyBlurEffect(owner))
        {
            if (dialog.ShowDialog(owner) == true)
                result = dialog.FileName;
        }

        return result;
    }

    /// <summary>
    /// Shows an open file dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the selected file path, or null if cancelled.</returns>
    public Task<string?> ShowOpenFileDialogAsync<T>(string filter)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowOpenFileDialogAsync(filter, owner);
    }

    /// <summary>
    /// Shows an open file dialog asynchronously with the specified owner window.
    /// </summary>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the selected file path, or null if cancelled.</returns>
    public Task<string?> ShowOpenFileDialogAsync(string filter, object? ownerWindow = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        var tcs = new TaskCompletionSource<string?>();
        var owner = ResolveOwnerWindow(ownerWindow);
        OpenFileDialog dialog = new OpenFileDialog()
        {
            Filter = filter,
            Multiselect = false,
            Title = LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_openfile_title)]
        };

        var blurDisposable = ApplyBlurEffect(owner);
        var dispatcher = GetDispatcher(owner);

        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var dialogResult = dialog.ShowDialog(owner);

                if (dialogResult == true)
                    tcs.SetResult(dialog.FileName);
                else
                {
                    tcs.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                blurDisposable?.Dispose();
            }
        }));

        return tcs.Task;
    }

    #endregion


    #region Open Files Dialog

    /// <summary>
    /// Shows an open files dialog (multiple selection), finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <returns>An array of selected file paths, or null if the user cancelled.</returns>
    public string[]? ShowOpenFilesDialog<T>(string filter)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowOpenFilesDialog(filter, owner);
    }

    /// <summary>
    /// Shows an open files dialog (multiple selection) with the specified owner window.
    /// </summary>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <returns>An array of selected file paths, or null if the user cancelled.</returns>
    public string[]? ShowOpenFilesDialog(string filter, object? ownerWindow = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        var owner = ResolveOwnerWindow(ownerWindow);
        OpenFileDialog dialog = new OpenFileDialog()
        {
            Filter = filter,
            Multiselect = true,
            Title = LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_openfile_title)]
        };

        string[]? result = null;

        using (ApplyBlurEffect(owner))
        {
            if (dialog.ShowDialog(owner) == true)
                result = dialog.FileNames;
        }

        return result;
    }

    /// <summary>
    /// Shows an open files dialog (multiple selection) asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <returns>A task that represents the asynchronous operation. The result contains an array of selected file paths, or null if cancelled.</returns>
    public Task<string[]?> ShowOpenFilesDialogAsync<T>(string filter)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowOpenFilesDialogAsync(filter, owner);
    }

    /// <summary>
    /// Shows an open files dialog (multiple selection) asynchronously with the specified owner window.
    /// </summary>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains an array of selected file paths, or null if cancelled.</returns>
    public Task<string[]?> ShowOpenFilesDialogAsync(string filter, object? ownerWindow = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        var tcs = new TaskCompletionSource<string[]?>();
        var owner = ResolveOwnerWindow(ownerWindow);
        OpenFileDialog dialog = new OpenFileDialog()
        {
            Filter = filter,
            Multiselect = true,
            Title = LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_openfile_title)]
        };

        var blurDisposable = ApplyBlurEffect(owner);
        var dispatcher = GetDispatcher(owner);

        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var dialogResult = dialog.ShowDialog(owner);

                if (dialogResult == true)
                    tcs.SetResult(dialog.FileNames);
                else
                {
                    tcs.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                blurDisposable?.Dispose();
            }
        }));

        return tcs.Task;
    }

    #endregion


    #region Save File Dialog

    /// <summary>
    /// Shows a save file dialog, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="fileName">The default file name, or null to use an empty string.</param>
    /// <returns>The selected file path, or null if the user cancelled.</returns>
    public string? ShowSaveFileDialog<T>(string filter, string? fileName = null)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowSaveFileDialog(filter, owner, fileName);
    }

    /// <summary>
    /// Shows a save file dialog with the specified owner window.
    /// </summary>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="fileName">The default file name, or null to use an empty string.</param>
    /// <returns>The selected file path, or null if the user cancelled.</returns>
    public string? ShowSaveFileDialog(string filter, object? ownerWindow = null, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        var owner = ResolveOwnerWindow(ownerWindow);
        SaveFileDialog dialog = new SaveFileDialog()
        {
            Filter = filter,
            FileName = fileName ?? string.Empty,
            Title = LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_savefile_title)]
        };

        string? result = null;

        using (ApplyBlurEffect(owner))
        {
            if (dialog.ShowDialog(owner) == true)
                result = dialog.FileName;
        }

        return result;
    }

    /// <summary>
    /// Shows a save file dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="fileName">The default file name, or null to use an empty string.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the selected file path, or null if cancelled.</returns>
    public Task<string?> ShowSaveFileDialogAsync<T>(string filter, string? fileName = null)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowSaveFileDialogAsync(filter, owner, fileName);
    }

    /// <summary>
    /// Shows a save file dialog asynchronously with the specified owner window.
    /// </summary>
    /// <param name="filter">The file filter string (e.g., "Text files (*.txt)|*.txt|All files (*.*)|*.*").</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="fileName">The default file name, or null to use an empty string.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the selected file path, or null if cancelled.</returns>
    public Task<string?> ShowSaveFileDialogAsync(string filter, object? ownerWindow = null, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        var tcs = new TaskCompletionSource<string?>();
        var owner = ResolveOwnerWindow(ownerWindow);
        SaveFileDialog dialog = new SaveFileDialog()
        {
            Filter = filter,
            FileName = fileName ?? string.Empty,
            Title = LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_savefile_title)]
        };

        var blurDisposable = ApplyBlurEffect(owner);
        var dispatcher = GetDispatcher(owner);

        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var dialogResult = dialog.ShowDialog(owner);

                if (dialogResult == true)
                    tcs.SetResult(dialog.FileName);
                else
                {
                    tcs.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                blurDisposable?.Dispose();
            }
        }));

        return tcs.Task;
    }

    #endregion


    #region Yes No Dialog

    /// <summary>
    /// Shows a yes/no confirmation dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The dialog title, or null to use a default.</param>
    /// <returns>A task that represents the asynchronous operation. The result is true for yes, false for no, or null if cancelled.</returns>
    public Task<bool?> ShowYesNoDialogAsync<T>(string message, string? title = null)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowYesNoDialogAsync(message, title, owner);
    }

    /// <summary>
    /// Shows a yes/no confirmation dialog asynchronously with the specified owner window.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The dialog title, or null to use a default.</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <returns>A task that represents the asynchronous operation. The result is true for yes, false for no, or null if cancelled.</returns>
    public Task<bool?> ShowYesNoDialogAsync(string message, string? title, object? ownerWindow = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));

        var owner = ResolveOwnerWindow(ownerWindow);

        DialogViewModel viewModel = new(true)
        {
            Message = message,
            Title = title ?? string.Empty,
            IsTwoOptionsMode = true
        };

        Views.Dialogs.DialogView dialog = new Views.Dialogs.DialogView();

        return ShowCustomDialogAsync(
            ownerWindow,
            dialog,
            viewModel,
            vm => ((DialogViewModel)vm).DialogResult,
            vm => ((DialogViewModel)vm).RequestClose = () => dialog.Close());
    }

    #endregion


    #region Show Message

    /// <summary>
    /// Shows a message dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The dialog title, or null to use a default.</param>
    /// <param name="pathMarkup">The icon path markup, or null to use a default information icon.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ShowMessageAsync<T>(string message, string? title, string? pathMarkup)
    {
        var owner = GetOwnerWindowByType<T>();

        await ShowMessageAsync(message, title, owner, pathMarkup);
    }


    public async Task ShowLocalizedMessageAsync(string messageKey, string titleKey, object? ownerWindow = null, string? pathMarkup = null)
    {
        var message = LocalizationManager.Instance[messageKey];

        var title = LocalizationManager.Instance[titleKey];

        await ShowMessageAsync(message, title, ownerWindow, pathMarkup);
    }


    /// <summary>
    /// Shows a message dialog asynchronously with the specified owner window.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The dialog title, or null to use a default.</param>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="pathMarkup">The icon path markup, or null to use a default information icon.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ShowMessageAsync(string message, string? title, object? ownerWindow = null, string? pathMarkup = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));

        var markup = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.InformationSymbol }.Data;
        var okPath = new MahApps.Metro.IconPacks.PackIconMaterial() { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.CheckBold }.Data;

        DialogViewModel viewModel = new(true)
        {
            Message = message,
            Title = title ?? LocalizationManager.Instance[nameof(IRI.Maptor.Jab.Common.Properties.Resources.dialog_showMessage_title)],
            IsTwoOptionsMode = false,
            IconPathMarkup = pathMarkup ?? markup,
            FirstOptionPathMarkup = okPath
        };

        Views.Dialogs.MessageBoxView dialog = new Views.Dialogs.MessageBoxView();

        return ShowCustomDialogAsync(
            ownerWindow,
            dialog,
            viewModel,
            vm => ((DialogViewModel)vm).DialogResult ?? false,
            vm => ((DialogViewModel)vm).RequestClose = () => dialog.Close())
            .ContinueWith(_ => { }, TaskContinuationOptions.None);
    }


    #endregion


    #region Change Password Dialog

    /// <summary>
    /// Shows a change password dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="requestAuthenticateAsync">The async authentication function.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the change password view model if successful, or null if cancelled.</returns>
    public Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog<T>(Func<IHavePassword, Task<bool>> requestAuthenticateAsync)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowChangePasswordDialog(owner, requestAuthenticateAsync);
    }

    /// <summary>
    /// Shows a change password dialog asynchronously with the specified owner window.
    /// </summary>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="requestAuthenticateAsync">The async authentication function.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the change password view model if successful, or null if cancelled.</returns>
    public Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog(object? ownerWindow, Func<IHavePassword, Task<bool>> requestAuthenticateAsync)
    {
        //requestClose parameter for viewModel is set in the next function
        ChangePasswordDialogViewModel? viewModel = ChangePasswordDialogViewModel.Create(() => { }, requestAuthenticateAsync);

        if (viewModel == null)
            throw new InvalidOperationException("Failed to create ChangePasswordDialogViewModel.");

        return ShowChangePasswordDialog(ownerWindow, viewModel);
    }

    /// <summary>
    /// Shows a change password dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="T">The type of window to use as owner.</typeparam>
    /// <param name="requestAuthenticate">The authentication function.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the change password view model if successful, or null if cancelled.</returns>
    public Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog<T>(Func<IHavePassword, bool> requestAuthenticate)
    {
        var owner = GetOwnerWindowByType<T>();
        return ShowChangePasswordDialog(owner, requestAuthenticate);
    }

    /// <summary>
    /// Shows a change password dialog asynchronously with the specified owner window.
    /// </summary>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="requestAuthenticate">The authentication function.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the change password view model if successful, or null if cancelled.</returns>
    public Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog(object? ownerWindow, Func<IHavePassword, bool> requestAuthenticate)
    {
        //requestClose parameter for viewModel is set in the next function
        ChangePasswordDialogViewModel? viewModel = ChangePasswordDialogViewModel.Create(() => { }, requestAuthenticate);
        if (viewModel == null)
            throw new InvalidOperationException("Failed to create ChangePasswordDialogViewModel.");
        return ShowChangePasswordDialog(ownerWindow, viewModel);
    }

    /// <summary>
    /// Shows a change password dialog asynchronously with the specified owner window and view model.
    /// </summary>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="viewModel">The change password dialog view model.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the change password view model if successful, or null if cancelled.</returns>
    public Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog(object? ownerWindow, ChangePasswordDialogViewModel viewModel)
    {
        if (viewModel == null)
            throw new ArgumentNullException(nameof(viewModel));

        Views.Dialogs.ChangePasswordDialogView dialog = new Views.Dialogs.ChangePasswordDialogView();

        return ShowCustomDialogAsync(
            ownerWindow,
            dialog,
            viewModel,
            vm => ((ChangePasswordDialogViewModel)vm).IsOk ? (ChangePasswordDialogViewModel)vm : null,
            vm => ((ChangePasswordDialogViewModel)vm).RequestClose = () => dialog.Close());
    }

    #endregion


    #region DXF, CSV, TSV, GeoJson Open Dialog

    /// <summary>
    /// Shows the DXF open dialog where user selects file, SRS, and views sample points.
    /// </summary>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="initialSrid">Optional initial SRID to pre-select (e.g. from CommandParameter).</param>
    /// <returns>The result with FilePath and SelectedSrid if user confirmed, or null if cancelled.</returns>
    public Task<DxfOpenDialogResult?> ShowDxfOpenDialogAsync(object? ownerWindow = null, int? initialSrid = null)
    {
        var viewModel = new DxfOpenDialogViewModel(this, initialSrid);
        var dialog = new Views.Dialogs.DxfOpenDialogView();

        return ShowCustomDialogAsync(
            ownerWindow,
            dialog,
            viewModel,
            vm => ((DxfOpenDialogViewModel)vm).DialogResult == true
                ? new DxfOpenDialogResult(((DxfOpenDialogViewModel)vm).FilePath, ((DxfOpenDialogViewModel)vm).EffectiveSelectedSrid)
                : null,
            vm => ((DxfOpenDialogViewModel)vm).RequestClose = () => dialog.Close());
    }

    public Task<CsvTsvOpenDialogResult?> ShowCsvTsvOpenDialogAsync(object? ownerWindow = null, bool initialIsCsv = true, int? initialSrid = null)
    {
        var viewModel = new CsvTsvOpenDialogViewModel(this, initialIsCsv, initialSrid);
        var dialog = new Views.Dialogs.CsvTsvOpenDialogView();

        return ShowCustomDialogAsync(
            ownerWindow,
            dialog,
            viewModel,
            vm => ((CsvTsvOpenDialogViewModel)vm).DialogResult == true && ((CsvTsvOpenDialogViewModel)vm).CsvTsvResult != null
                ? ((CsvTsvOpenDialogViewModel)vm).CsvTsvResult
                : null,
            vm => ((CsvTsvOpenDialogViewModel)vm).RequestClose = () => dialog.Close());
    }

    public Task<GeoJsonTopoJsonOpenDialogResult?> ShowGeoJsonTopoJsonOpenDialogAsync(object? ownerWindow = null, bool isGeoJson = true, int? initialSrid = null)
    {
        var viewModel = new GeoJsonTopoJsonOpenDialogViewModel(this, isGeoJson, initialSrid);
        var dialog = new Views.Dialogs.GeoJsonTopoJsonOpenDialogView();

        return ShowCustomDialogAsync(
            ownerWindow,
            dialog,
            viewModel,
            vm => ((GeoJsonTopoJsonOpenDialogViewModel)vm).DialogResult == true && ((GeoJsonTopoJsonOpenDialogViewModel)vm).Result != null
                ? ((GeoJsonTopoJsonOpenDialogViewModel)vm).Result
                : null,
            vm => ((GeoJsonTopoJsonOpenDialogViewModel)vm).RequestClose = () => dialog.Close());
    }

    #endregion


    /// <summary>
    /// Shows a custom dialog asynchronously, finding the owner window by type.
    /// </summary>
    /// <typeparam name="TParent">The type of window to use as owner.</typeparam>
    /// <param name="view">The window to show as a dialog.</param>
    /// <param name="viewModel">The dialog view model.</param>
    /// <returns>A task that represents the asynchronous operation. The result is true if accepted, false if cancelled, or null if closed without result.</returns>
    public Task<bool?> ShowDialogAsync<TParent>(Window view, DialogViewModelBase viewModel)
    {
        var owner = GetOwnerWindowByType<TParent>();
        return ShowDialogAsync<TParent>(owner, view, viewModel);
    }

    /// <summary>
    /// Shows a custom dialog asynchronously with the specified owner window.
    /// </summary>
    /// <typeparam name="TParent">The type parameter (used for consistency with overload).</typeparam>
    /// <param name="ownerWindow">The owner window for the dialog, or null to use automatic resolution.</param>
    /// <param name="view">The window to show as a dialog.</param>
    /// <param name="viewModel">The dialog view model.</param>
    /// <returns>A task that represents the asynchronous operation. The result is true if accepted, false if cancelled, or null if closed without result.</returns>
    public Task<bool?> ShowDialogAsync<TParent>(object? ownerWindow, Window view, DialogViewModelBase viewModel)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));
        if (viewModel == null)
            throw new ArgumentNullException(nameof(viewModel));

        return ShowCustomDialogAsync(
            ownerWindow,
            view,
            viewModel,
            vm => ((DialogViewModelBase)vm).DialogResult,
            vm => ((DialogViewModelBase)vm).OnSetResult += (sender, e) => view.Close());
    }
}


