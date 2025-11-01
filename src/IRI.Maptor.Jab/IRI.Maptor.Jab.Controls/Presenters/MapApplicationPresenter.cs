using System; 
using System.Threading.Tasks;

using IRI.Maptor.Jab.Common.Presenters;
using IRI.Maptor.Jab.Common.Assets.Commands;

namespace IRI.Maptor.Jab.Controls.Presenters
{
    public class MapApplicationPresenter : MapPresenter
    {
        public MapApplicationPresenter()
        {

        }

        //private AccountPresenter<TUser> _account;

        //public AccountPresenter<TUser> Account
        //{
        //    get { return _account; }
        //    set
        //    {
        //        _account = value;
        //        RaisePropertyChanged();
        //    }
        //}
        private AccountDialogViewModel _account;

        public AccountDialogViewModel Account
        {
            get { return _account; }
            set
            {
                _account = value;
                RaisePropertyChanged();
            }
        }

        public virtual void Initialize(System.Windows.Window ownerWindow)
        {
            this.DialogService = new IRI.Maptor.Jab.Controls.Services.Dialog.DefaultDialogService(ownerWindow);

            this.RequestShowGoToView = IRI.Maptor.Jab.Controls.Common.Defaults.DefaultActions.GetDefaultGoToAction(ownerWindow, this);

            this.RequestShowSymbologyView = layer => Common.Defaults.DefaultActions.GetDefaultShowSymbologyView(ownerWindow, layer, this);

            this.RequestClearAll = this.ClearAll;

            this.MapSettings.BaseMapCacheDirectory = Environment.CurrentDirectory + "\\Data";

            this.MapSettings.MaxGoogleZoomLevel = 18;
            this.MapSettings.MinGoogleZoomLevel = 2;

            this.SetMapCursorSet1();

            this.RegisterMapOptions();

            this.IsPanMode = true;

            ownerWindow.DataContext = this;
        }
         

        private void ShowAboutMe()
        {
            this.OnRequestShowAboutMe?.Invoke();
        }


        private RelayCommand _showAboutMeCommand;

        public RelayCommand ShowAboutMeCommand
        {
            get
            {
                if (_showAboutMeCommand == null)
                {
                    _showAboutMeCommand = new RelayCommand(param => this.ShowAboutMe());
                }
                return _showAboutMeCommand;
            }
        }

    }
}
