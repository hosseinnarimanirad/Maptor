using System.Windows.Media;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.ViewModels;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Layers;
using System.Collections.ObjectModel;

namespace IRI.Maptor.Jab.IranRepo;

public static class NccLayers
{ 
    public static ObservableCollection<ILayer> GetLayers(MapViewModelBase map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        var leveling1 = NccRepository.GetLayer(
            "leveling1", 
            IRI.Maptor.Jab.Common.Properties.Resources.layer_leveling1_title,
            new VisualParameters("#88A10024", "#FFA10024", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(10),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFA10024", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (leveling1 != null)
        { 
            leveling1.Commands = GetCommands<NccPoint>(map, leveling1);
        }


        var leveling2 = NccRepository.GetLayer("leveling2",
            IRI.Maptor.Jab.Common.Properties.Resources.layer_leveling2_title,
            new VisualParameters("#88E51400", "#FFE51400", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(8),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFE51400", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (leveling2 != null)
        {
            leveling2.Commands = GetCommands<NccPoint>(map, leveling2);
        }

        var leveling3 = NccRepository.GetLayer("leveling3",
            IRI.Maptor.Jab.Common.Properties.Resources.layer_leveling3_title,
            new VisualParameters("#88FA6900", "#FFFA6900", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(6),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFFA6900", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (leveling3 != null)
        { 
            leveling3.Commands = GetCommands<NccPoint>(map, leveling3);
        }


        var geodesy1 = NccRepository.GetLayer("geodesy1", 
            IRI.Maptor.Jab.Common.Properties.Resources.layer_geodesy1_title,
            new VisualParameters("#880050EF", "#FF0050EF", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(10),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FF1CA1E2", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (geodesy1 != null)
        { 
            geodesy1.Commands = GetCommands<NccPoint>(map, geodesy1);
        }

        var geodesy2 = NccRepository.GetLayer("geodesy2", 
            IRI.Maptor.Jab.Common.Properties.Resources.layer_geodesy2_title,
            new VisualParameters("#881CA1E2", "#FF1CA1E2", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(8),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FF1CA1E2", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (geodesy2 != null)
        { 
            geodesy2.Commands = GetCommands<NccPoint>(map, geodesy2);
        }

        var gravity = NccRepository.GetLayer("gravity",
            IRI.Maptor.Jab.Common.Properties.Resources.layer_gravimetry_title,
            new VisualParameters("#88AA00FF", "#FFAA00FF", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(10),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFAA00FF", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (gravity != null)
        { 
            gravity.Commands = GetCommands<NccPoint>(map, gravity);
        }

        var geodynamic = NccRepository.GetLayer("geodynamic", 
            IRI.Maptor.Jab.Common.Properties.Resources.layer_geodynamics_title,
            new VisualParameters("#88A4C401", "#FFA4C401", 1, .9)
            {
                PointSymbol = new SimplePointSymbolizer(10),
            },
            VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFA4C401", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (geodynamic != null)
        { 
            geodynamic.Commands = GetCommands<NccPoint>(map, geodynamic/*, geodynamicLabels*/);
        }

        var result =  new List<ILayer>() { leveling1, leveling2, leveling3, geodesy1, geodesy2, gravity, geodynamic }       
                    ?.Where(l => l != null)
                    ?.ToList() ?? [];

        return new ObservableCollection<ILayer>(result);
    }

    private static List<ILegendCommand> GetCommands<T>(MapViewModelBase map, VectorLayer layer/*, LabelParameters label*/)
        where T : class, IGeometryAware<Point>
    {
        return new List<ILegendCommand>()
        {
            LegendCommand.CreateZoomToExtentCommand(map, layer),
            LegendCommand.CreateShowAttributeTable/*<T>*/(map,layer),
            LegendCommand.CreateSelectByDrawing/*<T>*/(map,layer),
            LegendCommand.CreateClearSelected(map,layer),
            LegendToggleCommand.CreateToggleLayerLabelCommand(map, layer/*, label*/)
        };
    }

}
