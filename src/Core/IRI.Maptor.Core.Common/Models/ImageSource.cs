using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Model;

public class ImageSource
{
    private Func<int, int, int, string> MakeFileName { get; set; }
     
    public int ZoomLevel { get; set; }
     
    public ImageSource(int zoomLevel, Func<int, int, int, string> makeFileName)
    {
        this.ZoomLevel = zoomLevel;

        this.MakeFileName = makeFileName;
    }

    public string GetFileName(int row, int column)
    {
        return this.MakeFileName(row, column, this.ZoomLevel);
    }
}
