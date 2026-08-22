using IRI.Maptor.Presentation.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Presentation.Wpf.Models.MapExtentBookmarks;

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

    public override bool Equals(object? obj)
    {
        if (obj is not ScaleComboItem other)
            return false;

        return Group == other.Group &&
               Model?.InverseScale == other.Model?.InverseScale;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Group, Model?.InverseScale);
    }
}