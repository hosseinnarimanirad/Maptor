namespace IRI.Maptor.Core.Satellite.Elements;

/// <summary>
/// NORAD catalog numbers, including the Alpha-5 scheme: a five-character field whose first character
/// is a letter for numbers 100000–339999 (A = 10 … Z = 33; I and O are not used).
/// </summary>
public static class CatalogNumbers
{
    private const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static int Decode(string catalogNumber)
    {
        if (catalogNumber is null)
            throw new ArgumentNullException(nameof(catalogNumber));

        var text = catalogNumber.Trim();

        if (text.Length == 0)
            throw new FormatException("The catalog number is empty.");

        if (char.IsLetter(text[0]))
        {
            var index = Letters.IndexOf(char.ToUpperInvariant(text[0]));

            if (index < 0 || text.Length != 5 || !int.TryParse(text.Substring(1), out var rest))
                throw new FormatException($"'{catalogNumber}' is not an Alpha-5 catalog number.");

            return (index + 10) * 10000 + rest;
        }

        if (!int.TryParse(text, out var number) || number < 0)
            throw new FormatException($"'{catalogNumber}' is not a catalog number.");

        return number;
    }

    /// <summary>The five-character TLE form: zero-padded digits up to 99999, Alpha-5 above.</summary>
    public static string Encode(int number)
    {
        if (number < 0 || number > 339999)
            throw new ArgumentOutOfRangeException(nameof(number), "A TLE catalog number is in [0, 339999].");

        if (number < 100000)
            return number.ToString("00000");

        return Letters[number / 10000 - 10] + (number % 10000).ToString("0000");
    }
}
