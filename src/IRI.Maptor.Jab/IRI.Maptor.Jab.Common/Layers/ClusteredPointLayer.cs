using System;
using System.Collections.Generic;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Ket.GdiPersistence;
using IRI.Maptor.Sta.Common.Primitives;


namespace IRI.Maptor.Jab.Common.Layers;

public class ClusteredPointLayer : BaseLayer
{
    public event EventHandler? OnMouseDown;

    public override BoundingBox Extent
    {
        get => _source?.WebMercatorExtent ?? BoundingBox.NaN;
        protected set => throw new NotImplementedException();
    }

    public override LayerType Type => LayerType.Complex;

    ClusteredGeoTaggedImageSource? _source;

    Func<string, System.Windows.FrameworkElement>? _viewMaker;

    private ClusteredPointLayer()
    {

    }

    public SpecialPointLayer? GetLayer(double scale)
    {
        if (_source is null || _viewMaker is null)
            return null;

        var images = _source.Get(scale);

        var locatables = new List<Locateable>();

        foreach (var imageGroup in images)
        {
            var locatable = new Locateable();

            locatable.AncherFunction = AncherFunctionHandlers.CenterCenter;

            locatable.X = imageGroup.Center.WebMercatorLocation.X;

            locatable.Y = imageGroup.Center.WebMercatorLocation.Y;

            locatable.Element = _viewMaker(imageGroup.Frequency.ToString());

            locatable.OnRequestHandleMouseDown += (sender, e) => OnMouseDown?.Invoke(imageGroup, e: EventArgs.Empty);

            locatables.Add(locatable);
        }

        return new SpecialPointLayer(LayerName, locatables);
    }


    public static ClusteredPointLayer Create(string imageDirectory, Func<string, System.Windows.FrameworkElement> viewMaker)
    {
        ClusteredPointLayer result = new ClusteredPointLayer();

        result.LayerId = Guid.NewGuid();

        //result.imageSymbol = symbol;
        result._viewMaker = viewMaker;

        //result.VisualParameters = new VisualParameters(null, null, 0, 1);

        result._source = ClusteredGeoTaggedImageSource.Create(imageDirectory);

        return result;
    }

}
