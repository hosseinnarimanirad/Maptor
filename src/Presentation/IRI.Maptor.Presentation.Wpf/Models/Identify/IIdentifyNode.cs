using System;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>
/// A node of the identify results tree (a layer or a feature). WPF's TreeView exposes
/// selection through <c>TreeViewItem.IsSelected</c>, so each node carries its own flag and
/// the view model listens for it instead of binding a read-only <c>SelectedItem</c>.
/// </summary>
public interface IIdentifyNode
{
    bool IsSelected { get; set; }

    /// <summary>Bound two-way by the tree's container style; a leaf simply stores the value.</summary>
    bool IsExpanded { get; set; }

    event EventHandler? IsSelectedChanged;
}
