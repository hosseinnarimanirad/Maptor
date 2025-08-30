using IRI.Maptor.Jab.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models;

public class PathMarkupLabelPair : Notifier
{
    private string _pathMarkup;

    public string PathMarkup
    {
        get { return _pathMarkup; }
        set
        {
            _pathMarkup = value;
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
        }
    }

    public PathMarkupLabelPair(string title, string pathMarkup)
    {
        this.Title = title;

        this.PathMarkup = pathMarkup;
    }

}
