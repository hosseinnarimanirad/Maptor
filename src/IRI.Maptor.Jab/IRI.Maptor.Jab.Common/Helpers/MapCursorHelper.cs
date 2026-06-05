using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace IRI.Maptor.Jab.Common.Helpers;

public static class MapCursorHelper
{
    private const string BaseUri = "/IRI.Maptor.Jab.Common;component/Assets/Cursors/MaptorCursorSet/";

    public static Cursor Load(string fileName)
    {
        var stream = Application.GetResourceStream(new Uri(BaseUri + fileName, UriKind.Relative))?.Stream
            ?? throw new InvalidOperationException($"Cursor resource not found: {fileName}");

        return new Cursor(stream, false);
    }

    public static IReadOnlyDictionary<MapAction, Cursor> CreateDefaultSet()
    {
        return new Dictionary<MapAction, Cursor>
        {
            { MapAction.Pan, Cursors.Hand },
            { MapAction.ZoomIn, Load("ZoomIn.cur") },
            { MapAction.ZoomInRectangle, Load("ZoomIn.cur") },
            { MapAction.ZoomOut, Load("ZoomOut.cur") },
            { MapAction.DrawPoint, Load("DrawPoint.cur") },
            { MapAction.DrawPolyline, Cursors.Cross },
            { MapAction.DrawPolygon, Cursors.Cross },
            { MapAction.DrawRectangle, Cursors.Cross },
            { MapAction.Identify, Load("information.cur") },
            { MapAction.None, Cursors.Arrow },
        };
    }
}
