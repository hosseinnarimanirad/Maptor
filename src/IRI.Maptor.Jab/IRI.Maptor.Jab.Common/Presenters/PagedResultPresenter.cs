using System;
using System.Linq; 
using System.Collections.Generic;

using IRI.Maptor.Jab.Common.Assets.Commands;

namespace IRI.Maptor.Jab.Common.Presenters;

public class PagedResultPresenter<T> : Notifier
{
    public PagedResultPresenter(IEnumerable<T> dataSource, int pageSize)
    {
        this.PageSize = pageSize;

        this.DataSource = dataSource;
    }

    public void Referesh(IEnumerable<T> dataSource)
    {
        this.DataSource = dataSource;
    }

    public PagedResultPresenter(int pageSize)
    {
        this.DataSource = new List<T>();

        this.PageSize = pageSize;
    }

    private int _currentPage;

    public int CurrentPage
    {
        get { return _currentPage; }
        set
        {
            _currentPage = value;
            this.CurrentPageItems = this.DataSource.Skip(PageSize * (value - 1)).Take(PageSize);
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

            this.CurrentPage = 1;
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

            if (this.CurrentPage > this.TotalPages)
            {
                this.CurrentPage = this.TotalPages;
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
        get { return this.DataSource.Count(); }
    }

    public string Title
    {
        get { return string.Format("صفحۀ {0} از {1}", this.CurrentPage, this.TotalPages); }
    }


    public bool IsNextPageAvailable
    {
        get { return this.CurrentPage < this.TotalPages; }
    }

    public bool IsPreviousPageAvailable
    {
        get { return this.CurrentPage > 1; }
    }

    private IEnumerable<T> GetItems(int pageNumber)
    {
        return DataSource.Skip(PageSize * pageNumber).Take(PageSize);
    }

    public void GoToNextPage()
    {
        if (!IsNextPageAvailable)
            return;

        this.CurrentPage++;
    }

    public void GoToPreviousPage()
    {
        if (!IsPreviousPageAvailable)
            return;

        this.CurrentPage--;
    }

    public void GoToFirstPage()
    {
        this.CurrentPage = 1;
    }

    public void GoToLastPage()
    {
        this.CurrentPage = this.TotalPages;
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
                _nextPageCommand = new RelayCommand(param => this.GoToNextPage(), param => this.IsNextPageAvailable);
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
                _previousPageCommand = new RelayCommand(param => this.GoToPreviousPage(), param => this.IsPreviousPageAvailable);
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
                _lastPageCommand = new RelayCommand(param => this.GoToLastPage(), param => this.IsNextPageAvailable);
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
                _firstPageCommand = new RelayCommand(param => this.GoToFirstPage(), param => this.IsPreviousPageAvailable);
            }

            return _firstPageCommand;
        }
    }


    #endregion
}
