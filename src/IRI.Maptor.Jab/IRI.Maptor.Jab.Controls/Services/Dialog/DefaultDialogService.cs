using IRI.Maptor.Jab.Common.Abstractions;
using IRI.Maptor.Jab.Common.Models.Security;
using IRI.Maptor.Jab.Common.Presenters;
using IRI.Maptor.Jab.Controls.ViewModel.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;

namespace IRI.Maptor.Jab.Controls.Services.Dialog
{
    public class DefaultDialogService : IDialogService
    {

        public int BlurRadius { get; set; } = 5;

        public object _defaultOwnerWindow;

        const string _defaultShowMessageTitle = "پیام";

        const string _defaultSaveTitle = "ذخیره‌سازی";

        const string _defaultOpenTitle = "انتخاب فایل";

        public DefaultDialogService(object defaultOwnerWindow)
        {
            _defaultOwnerWindow = defaultOwnerWindow;
        }

        #region Open File Dialog

        public string ShowOpenFileDialog<T>(string filter)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowOpenFileDialog(filter, owner);
        }

        public string ShowOpenFileDialog(string filter, object ownerWindow)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = filter, Multiselect = false, Title = _defaultOpenTitle };

            string result = null;

            Effect defaultEffect = null;

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                defaultEffect = owner.Effect;

                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            if (dialog.ShowDialog(owner) == true)
                result = dialog.FileName;

            if (owner != null)
            {
                owner.Effect = defaultEffect;
            }

            return result;
        }


        // Async
        public Task<string> ShowOpenFileDialogAsync<T>(string filter)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowOpenFileDialogAsync(filter, owner);
        }

        public Task<string> ShowOpenFileDialogAsync(string filter, object ownerWindow)
        {
            var tcs = new TaskCompletionSource<string>();

            OpenFileDialog dialog = new OpenFileDialog() { Filter = filter, Multiselect = false, Title = _defaultOpenTitle };

            Effect defaultEffect = null;

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                defaultEffect = owner.Effect;

                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var dialogResult = dialog.ShowDialog(owner);

                if (owner != null)
                {
                    owner.Effect = defaultEffect;
                }

                if (dialogResult == true)
                    tcs.SetResult(dialog.FileName);
                else
                {
                    tcs.SetResult(null);
                }
            }));

            return tcs.Task;
        }

        #endregion


        #region Open Files Dialog

        public string[] ShowOpenFilesDialog<T>(string filter)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowOpenFilesDialog(filter, owner);
        }

        public string[] ShowOpenFilesDialog(string filter, object ownerWindow)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = filter, Multiselect = true, Title = _defaultOpenTitle };

            string[] result = null;

            Effect defaultEffect = null;

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                defaultEffect = owner.Effect;

                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            if (dialog.ShowDialog(owner) == true)
                result = dialog.FileNames;

            if (owner != null)
            {
                owner.Effect = defaultEffect;
            }

            return result;
        }

        // Async
        public Task<string[]> ShowOpenFilesDialogAsync<T>(string filter)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowOpenFilesDialogAsync(filter, owner);
        }

        public Task<string[]> ShowOpenFilesDialogAsync(string filter, object ownerWindow)
        {
            var tcs = new TaskCompletionSource<string[]>();

            OpenFileDialog dialog = new OpenFileDialog() { Filter = filter, Multiselect = true, Title = _defaultOpenTitle };

            Effect defaultEffect = null;

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                defaultEffect = owner.Effect;

                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var dialogResult = dialog.ShowDialog(owner);

                if (owner != null)
                {
                    owner.Effect = defaultEffect;
                }

                if (dialogResult == true)
                    tcs.SetResult(dialog.FileNames);
                else
                {
                    tcs.SetResult(null);
                }
            }));

            return tcs.Task;
        }

        #endregion


        #region Save File Dialog

        public string ShowSaveFileDialog<T>(string filter, string fileName = null)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowSaveFileDialog(filter, owner, fileName);
        }

        public string ShowSaveFileDialog(string filter, object ownerWindow, string fileName = null)
        {
            SaveFileDialog dialog = new SaveFileDialog() { Filter = filter, FileName = fileName ?? string.Empty, Title = _defaultSaveTitle };

            string result = null;

            Effect defaultEffect = null;

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                defaultEffect = owner.Effect;

                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            if (dialog.ShowDialog(owner) == true)
                result = dialog.FileName;

            if (owner != null)
            {
                owner.Effect = defaultEffect;
            }

            return result;
        }

        // Async version
        public Task<string> ShowSaveFileDialogAsync<T>(string filter, string fileName = null)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowSaveFileDialogAsync(filter, owner, fileName);
        }

        // 1399.12.20
        // making an async saveFileDialg
        // good article: https://sriramsakthivel.wordpress.com/2015/04/19/asynchronous-showdialog/
        public Task<string> ShowSaveFileDialogAsync(string filter, object ownerWindow, string fileName = null)
        {
            var tcs = new TaskCompletionSource<string>();

            SaveFileDialog dialog = new SaveFileDialog() { Filter = filter, FileName = fileName ?? string.Empty, Title = _defaultSaveTitle };

            Effect defaultEffect = null;

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                defaultEffect = owner.Effect;

                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                var dialogResult = dialog.ShowDialog(owner);

                if (owner != null)
                {
                    owner.Effect = defaultEffect;
                }

                if (dialogResult == true)
                    tcs.SetResult(dialog.FileName);
                else
                {
                    tcs.SetResult(null);
                }
            }));

            return tcs.Task;
        }

        #endregion


        #region Yes No Dialog

        public Task<bool?> ShowYesNoDialogAsync<T>(string message, string title)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowYesNoDialogAsync(message, title, owner);
        }

        public Task<bool?> ShowYesNoDialogAsync(string message, string title, object ownerWindow)
        {
            var tcs = new TaskCompletionSource<bool?>();

            IRI.Maptor.Jab.Controls.ViewModel.Dialogs.DialogViewModel viewModel = new Jab.Controls.ViewModel.Dialogs.DialogViewModel(true)
            {
                Message = message,
                Title = title,
                IsTwoOptionsMode = true
            };

            View.Dialogs.DialogView dialog = new View.Dialogs.DialogView();

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            dialog.Owner = owner;
            //dialog.Owner = Application.Current.MainWindow;

            var ownerDefaultEffect = owner?.Effect;

            if (owner != null)
            {
                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            dialog.Closed += (sender, e) => { tcs.SetResult(viewModel.DialogResult); };

            viewModel.RequestClose = () =>
            {
                dialog.Close();

                if (owner != null)
                {
                    owner.Effect = ownerDefaultEffect;
                }

            };

            dialog.DataContext = viewModel;

            dialog.ShowDialog();

            return tcs.Task;
        }

        #endregion


        #region Show Message

        public Task ShowMessageAsync<T>(string message, string? title, string? pathMarkup)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowMessageAsync(message, title, owner, pathMarkup);
        }

        public Task ShowMessageAsync(string message, string? title, object? ownerWindow, string? pathMarkup = null)
        {
            var tcs = new TaskCompletionSource<bool?>();

            DialogViewModel viewModel = new(true)
            {
                Message = message,
                Title = title ?? _defaultShowMessageTitle,
                IsTwoOptionsMode = false,
                IconPathMarkup = pathMarkup ?? IRI.Maptor.Jab.Common.Assets.ShapeStrings.Appbar.appbarInformation
            };

            View.Dialogs.MessageBoxView dialog = new View.Dialogs.MessageBoxView();

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                dialog.Owner = owner;
            }
            //dialog.Owner = Application.Current.MainWindow;

            var ownerDefaultEffect = owner?.Effect;

            if (owner != null)
            {
                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            dialog.Closed += (sender, e) => { tcs.SetResult(viewModel.DialogResult); };

            viewModel.RequestClose = () =>
            {
                dialog.Close();

                if (owner != null)
                {
                    owner.Effect = ownerDefaultEffect;
                }

            };

            dialog.DataContext = viewModel;

            dialog.ShowDialog();

            return tcs.Task;
        }

        #endregion


        #region Show UserName SignUp Dialog

        public Task<SignUpDialogViewModel> ShowUserNameSignUpDialogAsync<T>()
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowUserNameSignUpDialogAsync(owner);
        }

        public Task<SignUpDialogViewModel> ShowUserNameSignUpDialogAsync(object ownerWindow)
        {
            var tcs = new TaskCompletionSource<SignUpDialogViewModel>();

            View.Dialogs.UserNameSignUpDialogView dialog = new View.Dialogs.UserNameSignUpDialogView();

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                dialog.Owner = owner;
            }

            var ownerDefaultEffect = owner?.Effect;

            if (owner != null)
            {
                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            Action requestClose = () =>
            {
                dialog.Close();

                if (owner != null)
                {
                    owner.Effect = ownerDefaultEffect;
                }
            };

            SignUpDialogViewModel viewModel = new SignUpDialogViewModel(requestClose);
            //{
            //    Message = message,
            //    Title = title,
            //    IsTwoOptionsMode = false,
            //    IconPathMarkup = pathMarkup
            //};

            dialog.Closed += (sender, e) =>
            {
                if (viewModel.IsOk)
                {
                    tcs.SetResult(viewModel);
                }
                else
                {
                    tcs.SetResult(null);
                }

            };

            dialog.DataContext = viewModel;

            dialog.ShowDialog();

            return tcs.Task;
        }

        #endregion


        #region Change Password Dialog


        public Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog<T>(Func<IHavePassword, Task<bool>> requestAuthenticateAsync)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowChangePasswordDialog(owner, requestAuthenticateAsync);
        }

        public Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, Func<IHavePassword, Task<bool>> requestAuthenticateAsync)
        {
            //requestClose parameter for viewModel is set in the next function
            ChangePasswordDialogViewModel viewModel = ChangePasswordDialogViewModel.Create(null, requestAuthenticateAsync);

            return ShowChangePasswordDialog(ownerWindow, viewModel);
        }

        public Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog<T>(Func<IHavePassword, bool> requestAuthenticate)
        {
            var owner = Application.Current.Windows.OfType<T>().FirstOrDefault() as Window;

            return ShowChangePasswordDialog(owner, requestAuthenticate);
        }

        public Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, Func<IHavePassword, bool> requestAuthenticate)
        {
            //requestClose parameter for viewModel is set in the next function
            ChangePasswordDialogViewModel viewModel = ChangePasswordDialogViewModel.Create(null, requestAuthenticate);

            return ShowChangePasswordDialog(ownerWindow, viewModel);
        }

        //public Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, Func<IHavePassword, bool> requestAuthenticate)
        //{
        //    TaskCompletionSource<ChangePasswordDialogViewModel> tcs = new TaskCompletionSource<ChangePasswordDialogViewModel>();

        //    View.Dialogs.ChangePasswordDialogView dialog = new View.Dialogs.ChangePasswordDialogView();

        //if (ownerWindow == null)
        //    ownerWindow = _defaultOwnerWindow;

        //    var owner = ownerWindow as Window;

        //    if (owner != null)
        //    {
        //        dialog.Owner = owner;
        //    }

        //    var ownerDefaultEffect = owner?.Effect;

        //    if (owner != null)
        //    {
        //        owner.Effect = new BlurEffect() { Radius = BlurRadius };
        //    }

        //    Action requestClose = () =>
        //    {
        //        dialog.Close();

        //        if (owner != null)
        //        {
        //            owner.Effect = ownerDefaultEffect;
        //        }
        //    };

        //    ChangePasswordDialogViewModel viewModel = ChangePasswordDialogViewModel.Create(requestClose, requestAuthenticate);
        //    //{
        //    //    Message = message,
        //    //    Title = title,
        //    //    IsTwoOptionsMode = false,
        //    //    IconPathMarkup = pathMarkup
        //    //};

        //    dialog.Closed += (sender, e) =>
        //    {
        //        if (viewModel.IsOk)
        //        {
        //            tcs.SetResult(viewModel);
        //        }
        //        else
        //        {
        //            tcs.SetResult(null);
        //        }

        //    };

        //    dialog.DataContext = viewModel;

        //    dialog.ShowDialog();

        //    return tcs.Task;
        //}

        public Task<ChangePasswordDialogViewModel> ShowChangePasswordDialog(object ownerWindow, ChangePasswordDialogViewModel viewModel)
        {
            var tcs = new TaskCompletionSource<ChangePasswordDialogViewModel>();

            View.Dialogs.ChangePasswordDialogView dialog = new View.Dialogs.ChangePasswordDialogView();

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                dialog.Owner = owner;
            }

            var ownerDefaultEffect = owner?.Effect;

            if (owner != null)
            {
                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            Action requestClose = () =>
            {
                dialog.Close();

                if (owner != null)
                {
                    owner.Effect = ownerDefaultEffect;
                }
            };

            viewModel.RequestClose = requestClose;

            dialog.Closed += (sender, e) =>
            {
                if (viewModel.IsOk)
                {
                    tcs.SetResult(viewModel);
                }
                else
                {
                    tcs.SetResult(null);
                }
            };

            dialog.DataContext = viewModel;

            dialog.ShowDialog();

            return tcs.Task;
        }

        #endregion

        public Task<bool?> ShowDialogAsync<TParent>(Window view, DialogViewModelBase viewModel)
        {
            var owner = Application.Current.Windows.OfType<TParent>().FirstOrDefault() as Window;

            return ShowDialogAsync<TParent>(owner, view, viewModel);
        }

        public Task<bool?> ShowDialogAsync<TParent>(object ownerWindow, Window view, DialogViewModelBase viewModel)
        {
            var tcs = new TaskCompletionSource<bool?>();

            if (ownerWindow == null)
                ownerWindow = _defaultOwnerWindow;

            var owner = ownerWindow as Window;

            if (owner != null)
            {
                view.Owner = owner;
            }

            var ownerDefaultEffect = owner?.Effect;

            if (owner != null)
            {
                owner.Effect = new BlurEffect() { Radius = BlurRadius };
            }

            view.Closed += (sender, e) =>
            {
                if (owner != null)
                {
                    owner.Effect = ownerDefaultEffect;
                }

                tcs.SetResult(viewModel.DialogResult);
            };

            viewModel.OnSetResult += (sender, e) =>
            {
                view.Close();
                //if (viewModel.DialogResult == true)
                //{
                //    tcs.SetResult(viewModel.DialogResult);

                //    view.Close();
                //}
                //else
                //{
                //    tcs.SetResult(null);

                //    view.Close();
                //}

            };

            view.DataContext = viewModel;

            view.ShowDialog();

            return tcs.Task;
        }


    }
}
