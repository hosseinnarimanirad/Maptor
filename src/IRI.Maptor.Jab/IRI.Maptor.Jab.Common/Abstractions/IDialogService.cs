using System;
using System.Windows;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Jab.Common.ViewModels.Dialogs;
using IRI.Maptor.Sta.Common.Exceptions;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Jab.Common.Abstractions;

public interface IDialogService
{
    Task<string?> ShowOpenFolderDialogAsync<T>();

    Task<string?> ShowOpenFolderDialogAsync(object? ownerWindow = null);


    // ********************************************************************
    //                          Open File Dialog
    // ********************************************************************
    //string? ShowOpenFileDialog<T>(string filter);
    //string? ShowOpenFileDialog(string filter, object? owner = null);

    Task<string?> ShowOpenFileDialogAsync<T>(string filter);
    Task<string?> ShowOpenFileDialogAsync(string filter, object? ownerWindow = null);
    Task<string?> ShowOpenFileDialogAsync(DataSourceKind kind, object? ownerWindow = null);


    // ********************************************************************
    //                          Open Files Dialog
    // ********************************************************************
    //string[]? ShowOpenFilesDialog<T>(string filter);
    //string[]? ShowOpenFilesDialog(string filter, object? owner = null);

    Task<string[]?> ShowOpenFilesDialogAsync<T>(string filter);
    Task<string[]?> ShowOpenFilesDialogAsync(string filter, object? ownerWindow = null);


    // ********************************************************************
    //                          Save File Dialog
    // ********************************************************************
    //string? ShowSaveFileDialog<T>(string filter, string? fileName = null);
    //string? ShowSaveFileDialog(string filter, object? owner = null, string? fileName = null);

    Task<string?> ShowSaveFileDialogAsync<T>(string filter, string fileName);
    Task<string?> ShowSaveFileDialogAsync(string filter, object? ownerWindow = null, string? fileName = null);
    Task<string?> ShowSaveFileDialogAsync(DataSourceKind kind, object? ownerWindow = null, string? fileName = null);

    // ********************************************************************
    //                          Yes/No Dialog
    // ********************************************************************
    Task<bool?> ShowYesNoDialogAsync<T>(string message, string? title = null);
    Task<bool?> ShowYesNoDialogAsync(string message, string? title, object? owner = null);


    // ********************************************************************
    //                          Message Dialog
    // ********************************************************************
    Task ShowMessageAsync<T>(string message, string? title, string? pathMarkup);

    Task ShowLocalizedMessageAsync(string messageKey, string titleKey, object? ownerWindow = null, string? pathMarkup = null);

    Task ShowMessageAsync(string message, string? title, object? ownerWindow = null, string? pathMarkup = null);

    Task ShowErrorMessage(DomainException exception, object? ownerWindow = null);

    Task ShowMessage_DoneSuccessfully(object? ownerWindow = null);

    // ********************************************************************
    //                          SignUp Dialog
    // ********************************************************************
    //Task<SignUpDialogViewModel?> ShowUserNameSignUpDialogAsync<T>();

    //Task<SignUpDialogViewModel?> ShowUserNameSignUpDialogAsync(object? ownerWindow = null);


    #region Change Password Dialog

    Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog<T>(Func<IHavePassword, Task<bool>> requestAuthenticateAsync);

    Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog(object? ownerWindow, Func<IHavePassword, Task<bool>> requestAuthenticateAsync);

    Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog<T>(Func<IHavePassword, bool> requestAuthenticate);

    Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog(object? ownerWindow, Func<IHavePassword, bool> requestAuthenticate);

    Task<ChangePasswordDialogViewModel?> ShowChangePasswordDialog(object? ownerWindow, ChangePasswordDialogViewModel viewModel);

    #endregion


    // ********************************************************************
    //                          Show Dialog
    // ********************************************************************
    Task<bool?> ShowDialogAsync<TParent>(Window view, DialogViewModelBase viewModel);

    Task<bool?> ShowDialogAsync<TParent>(object? ownerWindow, Window view, DialogViewModelBase viewModel);


    // ********************************************************************
    //                          DXF Open Dialog
    // ********************************************************************
    Task<DxfOpenDialogResult?> ShowDxfOpenDialogAsync(object? ownerWindow = null, int? initialSrid = null);

    // ********************************************************************
    //                          CSV/TSV Open Dialog
    // ********************************************************************
    Task<CsvTsvOpenDialogResult?> ShowCsvTsvOpenDialogAsync(object? ownerWindow = null, bool initialIsCsv = true, int? initialSrid = null);

    // ********************************************************************
    //                          GeoJSON/TopoJSON Open Dialog
    // ********************************************************************
    Task<GeoJsonTopoJsonOpenDialogResult?> ShowGeoJsonTopoJsonOpenDialogAsync(object? ownerWindow = null, bool isGeoJson = true, int? initialSrid = null);

}
