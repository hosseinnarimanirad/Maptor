using PdfSharpCore.Fonts;

namespace IRI.Maptor.Sta.Pdf;

/// <summary>
/// Resolves the decoration-label font from caller-supplied TTF bytes, delegating every
/// other family to PdfSharpCore's system-font resolver. GlobalFontSettings.FontResolver
/// is process-global, so registration happens once and later font bytes are ignored.
/// </summary>
internal sealed class EmbeddedFontResolver : IFontResolver
{
    public const string FamilyName = "MaptorPdfLabelFont";

    private const string FaceName = "MaptorPdfLabelFont#Regular";

    private static readonly object _registrationLock = new();

    private static volatile bool _registered;

    private static byte[]? _fontBytes;

    private readonly IFontResolver _fallback = new PdfSharpCore.Utils.FontResolver();

    public string DefaultFontName => _fontBytes != null ? FamilyName : _fallback.DefaultFontName;

    /// <summary>
    /// Registers the resolver globally (first call wins) and returns the family name to
    /// use for label fonts: <see cref="FamilyName"/> when the bytes were accepted,
    /// otherwise <paramref name="fallbackFamily"/>.
    /// </summary>
    public static string Register(byte[]? fontBytes, string fallbackFamily)
    {
        if (fontBytes == null || fontBytes.Length == 0)
            return fallbackFamily;

        lock (_registrationLock)
        {
            if (!_registered)
            {
                try
                {
                    _fontBytes = fontBytes;
                    GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
                    _registered = true;
                }
                catch
                {
                    // Another resolver was already locked in by PdfSharpCore; keep system fonts.
                    _fontBytes = null;
                }
            }
        }

        return _registered ? FamilyName : fallbackFamily;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (_fontBytes != null && string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase))
            return new FontResolverInfo(FaceName);

        return _fallback.ResolveTypeface(familyName, isBold, isItalic);
    }

    public byte[] GetFont(string faceName)
    {
        if (_fontBytes != null && string.Equals(faceName, FaceName, StringComparison.Ordinal))
            return _fontBytes;

        return _fallback.GetFont(faceName);
    }
}