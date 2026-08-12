using System.Windows;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Wpf.Models.GoTo
{
    public class HamburgerGoToMenuItem : Notifier
    {
        private string _icon;

        public string Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                RaisePropertyChanged();
            }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SubTitle));
            }
        }

        private string _subTitle;

        public string SubTitle
        {
            get { return _subTitle; }
            set
            {
                _subTitle = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSubTitle
        {
            get => !string.IsNullOrWhiteSpace(SubTitle);
        }

        private string _tooltip;

        public string Tooltip
        {
            get { return _tooltip; }
            set
            {
                _tooltip = value;
                RaisePropertyChanged();
            }
        }

        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                RaisePropertyChanged();
            }
        }

        private SpatialReferenceType _menuType;

        public SpatialReferenceType MenuType
        {
            get { return _menuType; }
            set
            {
                _menuType = value;
                RaisePropertyChanged();
            }
        }


        private FrameworkElement _content;

        public FrameworkElement Content
        {
            get { return _content; }
            set
            {
                _content = value;
                RaisePropertyChanged();
            }
        }

        public HamburgerGoToMenuItem(FrameworkElement content, SpatialReferenceType menuType)
        {
            this.Content = content;

            this.MenuType = menuType;
            //this.ContentPresenter = presenter;

            //this.Content.DataContext = presenter;
        }

    }
}
