using IRI.Maptor.Jab.Common.Models.Security;

using System;
using System.Windows;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Common.Presenters;

namespace IRI.Maptor.Jab.Common.Abstractions;

public interface IDialogService
{
    // ********************************************************************
    //                          Open File Dialog
    // ********************************************************************
    string ShowOpenFileDialog<T>(string filter);
    string ShowOpenFileDialog(string filter, object owner);

    Task<string> ShowOpenFileDialogAsync<T>(string filter);
    Task<string> ShowOpenFileDialogAsync(string filter, object ownerWindow);


    // ********************************************************************
    //                          Open Files Dialog
    // ********************************************************************
    string[] ShowOpenFilesDialog<T>(string filter);
    string[] ShowOpenFilesDialog(string filter, object owner);

    Task<string[]> ShowOpenFilesDialogAsync<T>(string filter);
    Task<string[]> ShowOpenFilesDialogAsync(string filter, object ownerWindow);


    // ********************************************************************
    //                          Save File Dialog
    // ********************************************************************
    string ShowSaveFileDialog<T>(string filter, string fileName = null);
    string ShowSaveFileDialog(string filter, object owner, string fileName = null);

    Task<string> ShowSaveFileDialogAsync<T>(string filter, string fileName = null);
    Task<string> ShowSaveFileDialogAsync(string filter, object ownerWindow, string fileName = null);


    // ********************************************************************
    //                          Yes/No Dialog
    // ********************************************************************
    Task<bool?> ShowYesNoDialogAsync<T>(string message, string title = null);
    Task<bool?> ShowYesNoDialogAsync(string message, string title, object owner);


    // ********************************************************************
    //                          Message Dialog
    // ********************************************************************
    Task ShowMessageAsync<T>(string message, string? title, string? pathMarkup);

    Task ShowMessageAsync(string message, string? title, object ownerWindow, string? pathMarkup = null);


    // ********************************************************************
    //                          SignUp Dialog
    // ********************************************************************
    Task<SignUpDialogViewModel> ShowUserNameSignUpDialogAsync<T>();

    Task<SignUpDialogViewModel> ShowUserNameSignUpDialogAsync(object ownerWindow);


    #region Change Password Dialog

    Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog<T>(Func<IHavePassword, Task<bool>> requestAuthenticateAsync);

    Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, Func<IHavePassword, Task<bool>> requestAuthenticateAsync);

    Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog<T>(Func<IHavePassword, bool> requestAuthenticate);

    Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, Func<IHavePassword, bool> requestAuthenticate);

    Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, ChangePasswordDialogViewModel viewModel);

    #endregion


    // ********************************************************************
    //                          Show Dialog
    // ********************************************************************
    Task<bool?> ShowDialogAsync<TParent>(Window view, DialogViewModelBase viewModel);

    Task<bool?> ShowDialogAsync<TParent>(object ownerWindow, Window view, DialogViewModelBase viewModel);

}
