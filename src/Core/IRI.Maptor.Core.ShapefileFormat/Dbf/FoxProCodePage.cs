using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Dbf;

public struct FoxProCodePage
{
    private int _codePage;

    public int CodePage
    {
        get { return _codePage; }
    }

    private String _platform;

    public String Platform
    {
        get { return _platform; }
    }

    private string _identifier;

    public string Identifier
    {
        get { return _identifier; }
    }

    public FoxProCodePage(int codePage, string platform, string identifier)
    {
        this._codePage = codePage;

        this._platform = platform;

        this._identifier = identifier;
    }

    public byte LanguageDriver
    {
        get { return byte.Parse(Identifier, System.Globalization.NumberStyles.HexNumber); }
    }

}
