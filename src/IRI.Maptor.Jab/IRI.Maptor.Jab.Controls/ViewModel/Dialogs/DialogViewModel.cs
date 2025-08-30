using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Controls.ViewModel.Dialogs
{
    public class DialogViewModel : DialogViewModelBase
    {
        public Action _requestFirstAction, _requestSecondAction;

        public Action RequestClose;

        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }


        private string _message;

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChanged();
            }
        }

        private string _iconPathMarkup;

        public string IconPathMarkup
        {
            get { return _iconPathMarkup; }
            set
            {
                _iconPathMarkup = value;
                RaisePropertyChanged();
            }
        }


        private bool _isTwoOptionsMode;

        public bool IsTwoOptionsMode
        {
            get { return _isTwoOptionsMode; }
            set
            {
                _isTwoOptionsMode = value;
                RaisePropertyChanged();
            }
        }





        //OK Button
        private string _firstOptionText;

        public string FirstOptionText
        {
            get { return _firstOptionText; }
            set
            {
                _firstOptionText = value;
                RaisePropertyChanged();
            }
        }


        private string _firstOptionPathMarkup;

        public string FirstOptionPathMarkup
        {
            get { return _firstOptionPathMarkup; }
            set
            {
                _firstOptionPathMarkup = value;
                RaisePropertyChanged();
            }
        }

        private Brush _firstOptionColor;

        public Brush FirstOptionColor
        {
            get { return _firstOptionColor; }
            set
            {
                _firstOptionColor = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand _firstOptionCommand;

        public RelayCommand FirstOptionCommand
        {
            get
            {
                if (_firstOptionCommand == null)
                {
                    _firstOptionCommand = new RelayCommand(param =>
                    {
                        _requestFirstAction?.Invoke();

                        this.DialogResult = true;

                        RequestClose?.Invoke();
                    });
                }

                return _firstOptionCommand;
            }
        }


        //Cancel Button
        private string _secondOptionText;

        public string SecondOptionText
        {
            get { return _secondOptionText; }
            set
            {
                _secondOptionText = value;
                RaisePropertyChanged();
            }
        }

        private string _secondOptionPathMarkup;

        public string SecondOptionPathMarkup
        {
            get { return _secondOptionPathMarkup; }
            set
            {
                _secondOptionPathMarkup = value;
                RaisePropertyChanged();
            }
        }

        private Brush _secondOptionColor;

        public Brush SecondOptionColor
        {
            get { return _secondOptionColor; }
            set
            {
                _secondOptionColor = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand _secondOptionCommand;

        public RelayCommand SecondOptionCommand
        {
            get
            {
                if (_secondOptionCommand == null)
                {
                    _secondOptionCommand = new RelayCommand(param =>
                    {
                        _requestSecondAction?.Invoke();
                        
                        DialogResult = false;

                        RequestClose?.Invoke();
                    });
                }

                return _secondOptionCommand;
            }
        }

        public DialogViewModel()
        {
            IsTwoOptionsMode = true;
        }

        public DialogViewModel(bool isTwoOptionsMode = true)
        {
            IsTwoOptionsMode = isTwoOptionsMode;
            //Title = "تایید مجدد عملیات";
            //Message = "مطمئنی؟";
            ////Message = "alsdkfha;lskdhf;alksdgh;laksdhglaksdhg;lkahsdglahdglka;j ;aekl;ajdsgl; kajdg;lakdg a \na;lsdfj;lafdj";
            //FirstOptionText = "بله";
            //SecondOptionText = "خیر";
            //IsTwoOptionsMode = true;
        }
    }
}
