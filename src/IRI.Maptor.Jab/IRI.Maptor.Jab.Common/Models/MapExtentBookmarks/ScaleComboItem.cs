using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models.MapExtentBookmarks;

public sealed class ScaleComboItem
{
    public ScaleComboItem(string group, ScaleModel model)
    {
        Group = group;
        Model = model;
    }

    public string Group { get; }

    public ScaleModel Model { get; }

    public string DisplayLabel => Model is GoogleScale g ? g.ToString() : $"1:{Model.InverseScale:N0}";
}