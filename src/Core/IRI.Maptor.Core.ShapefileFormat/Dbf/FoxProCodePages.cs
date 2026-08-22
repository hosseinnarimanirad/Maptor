using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Dbf;


public static class EsriCodePages
{
    static Dictionary<string, Encoding> _codePages;

    static EsriCodePages()
    {
        _codePages = new Dictionary<string, Encoding>();

        _codePages.Add("01", Encoding.GetEncoding(437)); // U.S. MS–DOS
        _codePages.Add("02", Encoding.GetEncoding(850)); // International MS–DOS
        _codePages.Add("03", Encoding.GetEncoding(1252)); // Windows ANSI
        _codePages.Add("08", Encoding.GetEncoding(865)); // Danish OEM
        _codePages.Add("09", Encoding.GetEncoding(437)); // Dutch OEM
        _codePages.Add("0A", Encoding.GetEncoding(850)); // Dutch OEM*
        _codePages.Add("0B", Encoding.GetEncoding(437)); // Finnish OEM
        _codePages.Add("0D", Encoding.GetEncoding(437)); // French OEM
        _codePages.Add("0E", Encoding.GetEncoding(850)); // French OEM*
        _codePages.Add("0F", Encoding.GetEncoding(437)); // German OEM
        _codePages.Add("10", Encoding.GetEncoding(850)); // German OEM*
        _codePages.Add("11", Encoding.GetEncoding(437)); // Italian OEM
        _codePages.Add("12", Encoding.GetEncoding(850)); // Italian OEM*
        _codePages.Add("13", Encoding.GetEncoding(932)); // Japanese Shift-JIS
        _codePages.Add("14", Encoding.GetEncoding(850)); // Spanish OEM*
        _codePages.Add("15", Encoding.GetEncoding(437)); // Swedish OEM
        _codePages.Add("16", Encoding.GetEncoding(850)); // Swedish OEM*
        _codePages.Add("17", Encoding.GetEncoding(865)); // Norwegian OEM
        _codePages.Add("18", Encoding.GetEncoding(437)); // Spanish OEM
        _codePages.Add("19", Encoding.GetEncoding(437)); // English OEM (Britain)
        _codePages.Add("1A", Encoding.GetEncoding(850)); // English OEM (Britain)*
        _codePages.Add("1B", Encoding.GetEncoding(437)); // English OEM (U.S.)
        _codePages.Add("1C", Encoding.GetEncoding(863)); // French OEM (Canada)
        _codePages.Add("1D", Encoding.GetEncoding(850)); // French OEM*
        _codePages.Add("1F", Encoding.GetEncoding(852)); // Czech OEM
        _codePages.Add("22", Encoding.GetEncoding(852)); // Hungarian OEM
        _codePages.Add("23", Encoding.GetEncoding(852)); // Polish OEM
        _codePages.Add("24", Encoding.GetEncoding(860)); // Portuguese OEM
        _codePages.Add("25", Encoding.GetEncoding(850)); // Portuguese OEM*
        _codePages.Add("26", Encoding.GetEncoding(866)); // Russian OEM
        _codePages.Add("37", Encoding.GetEncoding(850)); // English OEM (U.S.)*
        _codePages.Add("40", Encoding.GetEncoding(852)); // Romanian OEM
        _codePages.Add("4D", Encoding.GetEncoding(936)); // Chinese GBK (PRC)
        _codePages.Add("4E", Encoding.GetEncoding(949)); // Korean (ANSI/OEM)
        _codePages.Add("4F", Encoding.GetEncoding(950)); // Chinese Big5 (Taiwan)
        _codePages.Add("50", Encoding.GetEncoding(874)); // Thai (ANSI/OEM)
        _codePages.Add("57", Encoding.GetEncoding(1252)); // ANSI
        _codePages.Add("58", Encoding.GetEncoding(1252)); // Western European ANSI
        _codePages.Add("59", Encoding.GetEncoding(1252)); // Spanish ANSI
        _codePages.Add("64", Encoding.GetEncoding(852)); // Eastern European MS–DOS
        _codePages.Add("65", Encoding.GetEncoding(866)); // Russian MS–DOS
        _codePages.Add("66", Encoding.GetEncoding(865)); // Nordic MS–DOS
        _codePages.Add("67", Encoding.GetEncoding(861)); // Icelandic MS–DOS
        _codePages.Add("6A", Encoding.GetEncoding(737)); // Greek MS–DOS (437G)
        _codePages.Add("6B", Encoding.GetEncoding(857)); // Turkish MS–DOS
        _codePages.Add("6C", Encoding.GetEncoding(863)); // French–Canadian MS–DOS
        _codePages.Add("78", Encoding.GetEncoding(950)); // Taiwan Big 5
        _codePages.Add("79", Encoding.GetEncoding(949)); // Hangul (Wansung)
        _codePages.Add("7A", Encoding.GetEncoding(936)); // PRC GBK
        _codePages.Add("7B", Encoding.GetEncoding(932)); // Japanese Shift-JIS
        _codePages.Add("7C", Encoding.GetEncoding(874)); // Thai Windows/MS–DOS
        _codePages.Add("86", Encoding.GetEncoding(737)); // Greek OEM
        _codePages.Add("87", Encoding.GetEncoding(852)); // Slovenian OEM
        _codePages.Add("88", Encoding.GetEncoding(857)); // Turkish OEM
        _codePages.Add("C8", Encoding.GetEncoding(1250)); // Eastern European Windows
        _codePages.Add("C9", Encoding.GetEncoding(1251)); // Russian Windows
        _codePages.Add("CA", Encoding.GetEncoding(1254)); // Turkish Windows
        _codePages.Add("CB", Encoding.GetEncoding(1253)); // Greek Windows
        _codePages.Add("CC", Encoding.GetEncoding(1257)); // Baltic Windows


    }

    public static Encoding LanguageIdentifierToEncoding(string identifier)
    {
        return _codePages[identifier];
    }

    /// <summary>
    /// Returns the hex representation of the language identifier
    /// </summary>
    /// <param name="displayName"></param>
    /// <returns></returns>
    public static string EncodingToLanguageIdentifier(Encoding encoding)
    {
        foreach (string item in _codePages.Keys)
        {
            if (_codePages[item].Equals(encoding))
            {
                return item;
            }
        }

        return "00";
    }
}
