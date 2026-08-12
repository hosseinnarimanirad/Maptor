using System;
using System.Linq;
using System.Collections.Generic;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Jab.Wpf.ViewModels;

public class PagedResultViewModel<T> : Notifier
{
    public PagedResultViewModel(IEnumerable<T> dataSource, int pageSize)
    {
        PageSize = pageSize;

        DataSource = dataSource;
    }

    public void Refresh(IEnumerable<T> dataSource)
    {
        DataSource = dataSource;
    }

    public PagedResultViewModel(int pageSize)
    {
        DataSource = new List<T>();

        PageSize = pageSize;
    }

    private int _currentPage;

    public int CurrentPage
    {
        get { return _currentPage; }
        set
        {
            _currentPage = value;
            CurrentPageItems = DataSource.Skip(PageSize * (value - 1)).Take(PageSize);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsNextPageAvailable));
            RaisePropertyChanged(nameof(IsPreviousPageAvailable));
            RaisePropertyChanged(nameof(Title));

            if (OnPageChanged != null)
            {
                OnPageChanged(this, null);
            }
        }
    }

    private IEnumerable<T> _dataSource;

    public IEnumerable<T> DataSource
    {
        get { return _dataSource; }
        set
        {
            _dataSource = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(Count));

            CurrentPage = 1;
        }
    }

    private IEnumerable<T> _currentPageItems;

    public IEnumerable<T> CurrentPageItems
    {
        get { return _currentPageItems; }
        set
        {
            _currentPageItems = value;
            RaisePropertyChanged();
        }
    }

    private int _pageSize;

    public int PageSize
    {
        get { return _pageSize; }
        set
        {
            _pageSize = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(IsNextPageAvailable));
            RaisePropertyChanged(nameof(IsPreviousPageAvailable));

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            RaisePropertyChanged(nameof(CurrentPage));
        }
    }

    public int TotalPages
    {
        get
        {
            if (DataSource == null)
            {
                return 0;
            }

            return (int)Math.Ceiling(DataSource.Count() / (double)PageSize);
        }
    }


    public int Count
    {
        get { return DataSource.Count(); }
    }

    public string Title
    {
        get { return string.Format("صفحۀ {0} از {1}", CurrentPage, TotalPages); }
    }


    public bool IsNextPageAvailable
    {
        get { return CurrentPage < TotalPages; }
    }

    public bool IsPreviousPageAvailable
    {
        get { return CurrentPage > 1; }
    }

    private IEnumerable<T> GetItems(int pageNumber)
    {
        return DataSource.Skip(PageSize * pageNumber).Take(PageSize);
    }

    public void GoToNextPage()
    {
        if (!IsNextPageAvailable)
            return;

        CurrentPage++;
    }

    public void GoToPreviousPage()
    {
        if (!IsPreviousPageAvailable)
            return;

        CurrentPage--;
    }

    public void GoToFirstPage()
    {
        CurrentPage = 1;
    }

    public void GoToLastPage()
    {
        CurrentPage = TotalPages;
    }

    public event EventHandler OnPageChanged;

    #region Commands

    private RelayCommand _nextPageCommand;

    public RelayCommand NextPageCommand
    {
        get
        {
            if (_nextPageCommand == null)
            {
                _nextPageCommand = new RelayCommand(param => GoToNextPage(), param => IsNextPageAvailable);
            }

            return _nextPageCommand;
        }
    }


    private RelayCommand _previousPageCommand;

    public RelayCommand PreviousPageCommand
    {
        get
        {
            if (_previousPageCommand == null)
            {
                _previousPageCommand = new RelayCommand(param => GoToPreviousPage(), param => IsPreviousPageAvailable);
            }

            return _previousPageCommand;
        }
    }


    private RelayCommand _lastPageCommand;

    public RelayCommand LastPageCommand
    {
        get
        {
            if (_lastPageCommand == null)
            {
                _lastPageCommand = new RelayCommand(param => GoToLastPage(), param => IsNextPageAvailable);
            }

            return _lastPageCommand;
        }
    }


    private RelayCommand _firstPageCommand;

    public RelayCommand FirstPageCommand
    {
        get
        {
            if (_firstPageCommand == null)
            {
                _firstPageCommand = new RelayCommand(param => GoToFirstPage(), param => IsPreviousPageAvailable);
            }

            return _firstPageCommand;
        }
    }


    #endregion
}
