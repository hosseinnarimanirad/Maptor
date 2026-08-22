namespace IRI.Maptor.Core.Spatial.IO.PmTiles;

internal static class PmTilesConstants
{
    public const string Magic = "PMTiles";
    public const int MagicLength = 7;
    public const byte CurrentVersion = 3;
    public const int HeaderLength = 127;

    public const int RootDirectoryHeaderOffset = MagicLength + 1; // magic + version
}

