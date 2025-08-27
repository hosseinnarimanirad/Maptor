using System.Windows.Media;

using IRI.Maptor.Jab.Common;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.Cartography.Symbologies;
using IRI.Maptor.Jab.Common.Presenters;

namespace IRI.Maptor.Jab.IranRepo;

public static class NccLayers
{ 
    public static List<ILayer> GetLayers(MapPresenter map)
    {
        var fontFamily = new FontFamily("Times New Roman");

        var leveling1 = NccRepository.GetLayer("leveling1", "ترازیابی درجه ۱", new VisualParameters("#88A10024", "#FFA10024", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(10),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFA10024", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (leveling1 != null)
        {
            //var leveling1Labels = ;
            leveling1.Commands = GetCommands<NccPoint>(map, leveling1/*, leveling1Labels*/);
        }


        var leveling2 = NccRepository.GetLayer("leveling2", "ترازیابی درجه ۲", new VisualParameters("#88E51400", "#FFE51400", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(8),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFE51400", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (leveling2 != null)
        {
            //var leveling2Labels = ;
            leveling2.Commands = GetCommands<NccPoint>(map, leveling2/*, leveling2Labels*/);
        }

        var leveling3 = NccRepository.GetLayer("leveling3", "ترازیابی درجه ۳", new VisualParameters("#88FA6900", "#FFFA6900", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(6),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFFA6900", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (leveling3 != null)
        {
            //var leveling3Labels = ;
            leveling3.Commands = GetCommands<NccPoint>(map, leveling3/*, leveling3Labels*/);
        }


        var geodesy1 = NccRepository.GetLayer("geodesy1", "ژئودزی ۱", new VisualParameters("#880050EF", "#FF0050EF", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(10),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FF1CA1E2", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (geodesy1 != null)
        {
            //var gedesy1Labels = ;
            geodesy1.Commands = GetCommands<NccPoint>(map, geodesy1/*, gedesy1Labels*/);
        }

        var geodesy2 = NccRepository.GetLayer("geodesy2", "ژئودزی ۲", new VisualParameters("#881CA1E2", "#FF1CA1E2", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(8),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FF1CA1E2", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (geodesy2 != null)
        {
            //var geodesy2Labels = ;
            geodesy2.Commands = GetCommands<NccPoint>(map, geodesy2/*, geodesy2Labels*/);
        }

        var gravity = NccRepository.GetLayer("gravity", "ثقل سنجی", new VisualParameters("#88AA00FF", "#FFAA00FF", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(10),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFAA00FF", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (gravity != null)
        {
            //var gravityLabels = ;
            gravity.Commands = GetCommands<NccPoint>(map, gravity/*, gravityLabels*/);
        }

        var geodynamic = NccRepository.GetLayer("geodynamic", "ژئودینامیک", new VisualParameters("#88A4C401", "#FFA4C401", 1, .9)
        {
            PointSymbol = new SimplePointSymbolizer(10),
        },
        VisualParameters.CreateLabel(ScaleInterval.Create(10), 11, BrushHelper.CreateBrush("#FFA4C401", 1), fontFamily, i => i.AsPoint(), isRtl: false));

        if (geodynamic != null)
        {
            //var geodynamicLabels = ;
            geodynamic.Commands = GetCommands<NccPoint>(map, geodynamic/*, geodynamicLabels*/);
        }

        return new List<ILayer>() { leveling1, leveling2, leveling3, geodesy1, geodesy2, gravity, geodynamic }
                    ?.Where(l => l != null)
                    ?.ToList() ?? [];
    }

    private static List<ILegendCommand> GetCommands<T>(MapPresenter map, VectorLayer layer/*, LabelParameters label*/)
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
