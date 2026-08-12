using IRI.Maptor.Jab.Core.Layers;

namespace IRI.Maptor.Jab.Wpf.Models.Legend;

public interface ILegendCommand
{
    RelayCommand Command { get; set; }

    string PathMarkup { get; set; }

    bool IsEnabled { get; set; }

    //bool IsSelected { get; set; }

    string ToolTip { get; /*set;*/ }

    ILayer Layer { get; set; }

    /// <summary>
    /// e.g. commands when layer has select features
    /// </summary>
    bool IsCommandVisible { get; set; }
}
