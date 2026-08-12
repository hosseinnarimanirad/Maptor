using System;
using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Jab.Wpf.Assets.Data;

/// <summary>
/// Contains Envelope of province geometries in Web Mercator for 1393
/// </summary>
public static class IriProvinces93WmEnvelopes
{
    public const string Alborz = "AQMAAAABAAAABQAAAKSSOnFCTVVBYP5DVtcqUEFwio6EqNpVQWD+Q1bXKlBBcIqOhKjaVUEYrke975VQQaSSOnFCTVVBGK5Hve+VUEGkkjpxQk1VQWD+Q1bXKlBB";
    public const string Ardabil = "AQMAAAABAAAABQAAAGBUUn8xFVRBgLdAkhD9UEG0N/jCb8ZUQYC3QJIQ/VBBtDf4wm/GVEF4LSGDOGZSQWBUUn8xFVRBeC0hgzhmUkFgVFJ/MRVUQYC3QJIQ/VBB";
    public const string AzarbayejaneGarbi = "AQMAAAABAAAABQAAAOiuJXh5s1JBqM/VFoxjUEEcnl6RcSBUQajP1RaMY1BBHJ5ekXEgVEEY+8vae29SQeiuJXh5s1JBGPvL2ntvUkHoriV4ebNSQajP1RaMY1BB";
    public const string AzarbayejaneShargi = "AQMAAAABAAAABQAAAOACCUowJVNBUI2XtpjMUEE4tMgGXYdUQVCNl7aYzFBBOLTIBl2HVEEYfGGS2j5SQeACCUowJVNBGHxhkto+UkHgAglKMCVTQVCNl7aYzFBB";
    public const string Booshehr = "AQMAAAABAAAABQAAAEBXW/EAR1VBeC0hv3wXSEH0jlM8uXpWQXgtIb98F0hB9I5TPLl6VkGgGi/NMQNLQUBXW/EAR1VBoBovzTEDS0FAV1vxAEdVQXgtIb98F0hB";
    public const string ChaharmahalVaBakhtiari = "AQMAAAABAAAABQAAAJhMFfBUBVVBsCXkk3/dS0FgKcugbddVQbAl5JN/3UtBYCnLoG3XVUFg7lpqr4ZNQZhMFfBUBVVBYO5aaq+GTUGYTBXwVAVVQbAl5JN/3UtB";
    public const string Isfahan = "AQMAAAABAAAABQAAALSd7weGFFVBgJVD45BrS0HIurgN0JBXQYCVQ+OQa0tByLq4DdCQV0HA7J6kdUFPQbSd7weGFFVBwOyepHVBT0G0ne8HhhRVQYCVQ+OQa0tB";
    public const string Fars = "AQMAAAABAAAABQAAADB3LT0EfVVBOGdEsVTgR0EUHclV9plXQThnRLFU4EdBFB3JVfaZV0GQh4W652FMQTB3LT0EfVVBkIeFuudhTEEwdy09BH1VQThnRLFU4EdB";
    public const string Qazvin = "AQMAAAABAAAABQAAAAxGJfH+sFRB2GgAEwkXUEEMAisztphVQdhoABMJF1BBDAIrM7aYVUFI6gSoM9ZQQQxGJfH+sFRBSOoEqDPWUEEMRiXx/rBUQdhoABMJF1BB";
    public const string Gilan = "AQMAAAABAAAABQAAADirPq8JoFRB+HXgwLizUEG4rwO3KX1VQfh14MC4s1BBuK8Dtyl9VUGgcD0u1bZRQTirPq8JoFRBoHA9LtW2UUE4qz6vCaBUQfh14MC4s1BB";
    public const string Golestan = "AQMAAAABAAAABQAAAIhjXfTw3lZBKKkTZKKqUEFY7C9T3+lXQSipE2SiqlBBWOwvU9/pV0FALNRKR4lRQYhjXfTw3lZBQCzUSkeJUUGIY1308N5WQSipE2SiqlBB";
    public const string Hamadan = "AQMAAAABAAAABQAAAOi3r2PgS1RBMNSahvO8TkFQQBPpAAJVQTDUmobzvE5BUEAT6QACVUE4GsBPA0RQQei3r2PgS1RBOBrATwNEUEHot69j4EtUQTDUmobzvE5B";
    public const string Hormozgan = "AQMAAAABAAAABQAAAKzYX778ZFZBAE2EzVJTRkGwlGVszSdZQQBNhM1SU0ZBsJRlbM0nWUEwZRnyS6NJQazYX778ZFZBMGUZ8kujSUGs2F++/GRWQQBNhM1SU0ZB";
    public const string Ilam = "AQMAAAABAAAABQAAACTb+cLQZVNBUL99tX/BTEHQkVzS5mZUQVC/fbV/wUxB0JFc0uZmVEFw8IVJusZOQSTb+cLQZVNBcPCFSbrGTkEk2/nC0GVTQVC/fbV/wUxB";
    public const string Kerman = "AQMAAAABAAAABQAAADx5WIBaE1dB6FG47rhWR0GIQWDVFUxZQehRuO64VkdBiEFg1RVMWUHgP6SnhqxMQTx5WIBaE1dB4D+kp4asTEE8eViAWhNXQehRuO64VkdB";
    public const string Kermanshah = "AQMAAAABAAAABQAAAGQQWKHRR1NB4EYDQGNqTkFwGw00eW1UQeBGA0Bjak5BcBsNNHltVEHAdJMIwwdQQWQQWKHRR1NBwHSTCMMHUEFkEFih0UdTQeBGA0Bjak5B";
    public const string KhorasanJonoobi = "AQMAAAABAAAABQAAANQJaCqLhFdBYNO8y/A8S0GkvcGvE+BZQWDTvMvwPEtBpL3BrxPgWUGArraCot1PQdQJaCqLhFdBgK62gqLdT0HUCWgqi4RXQWDTvMvwPEtB";
    public const string KhorasanRazavi = "AQMAAAABAAAABQAAACAf9BCP4FdBYJhMlZ2WTkHs4jaeegRaQWCYTJWdlk5B7OI2nnoEWkHYEvIV6E5RQSAf9BCP4FdB2BLyFehOUUEgH/QQj+BXQWCYTJWdlk5B";
    public const string KhorasanShomali = "AQMAAAABAAAABQAAAMi6uIlHvVdBmG4Sl4a1UEEIrByCWM9YQZhuEpeGtVBBCKwcgljPWEFAHOtKtp9RQci6uIlHvVdBQBzrSrafUUHIuriJR71XQZhuEpeGtVBB";
    public const string Khozestan = "AQMAAAABAAAABQAAALC/7E7ePVRBIMnlb1adSkHwp8YDiXdVQSDJ5W9WnUpB8KfGA4l3VUFQYhCIiLZNQbC/7E7ePVRBUGIQiIi2TUGwv+xO3j1UQSDJ5W9WnUpB";
    public const string KohgiluyehVaBoyerahmad = "AQMAAAABAAAABQAAANAZUZKHL1VBsJ3vT8yoSkGY3ZPbDAlWQbCd70/MqEpBmN2T2wwJVkHwJjGQrjFMQdAZUZKHL1VB8CYxkK4xTEHQGVGShy9VQbCd70/MqEpB";
    public const string Kordestan = "AQMAAAABAAAABQAAACyynZ9MWFNBQHDO6EZ/T0EEeAvUPn1UQUBwzuhGf09BBHgL1D59VEHQ3uDTJqZQQSyynZ9MWFNB0N7g0yamUEEssp2fTFhTQUBwzuhGf09B";
    public const string Lorestan = "AQMAAAABAAAABQAAAMT+skfV41NBUPwY09ldTUGoglGlnT1VQVD8GNPZXU1BqIJRpZ09VUEAgZXTAyBPQcT+skfV41NBAIGV0wMgT0HE/rJH1eNTQVD8GNPZXU1B";
    public const string Markazi = "AQMAAAABAAAABQAAANjFbdhFyVRBkOTyT6MbTkEEVg5tA65VQZDk8k+jG05BBFYObQOuVUFwtRWnaS5QQdjFbdhFyVRBcLUVp2kuUEHYxW3YRclUQZDk8k+jG05B";
    public const string Mazandaran = "AQMAAAABAAAABQAAAMAwmW69YVVBIKfo0CBIUEFY7C/nuvxWQSCn6NAgSFBBWOwv57r8VkFQ844f3+lQQcAwmW69YVVBUPOOH9/pUEHAMJluvWFVQSCn6NAgSFBB";
    public const string Qom = "AQMAAAABAAAABQAAAFxkO0fvRFVBoFWfqw/kTkGYIY5xVRFWQaBVn6sP5E5BmCGOcVURVkGwpnmHfPVPQVxkO0fvRFVBsKZ5h3z1T0FcZDtH70RVQaBVn6sP5E5B";
    public const string Semnan = "AQMAAAABAAAABQAAAEi/fc3CAlZBsPpczY37TkHgAgnuqjpYQbD6XM2N+05B4AIJ7qo6WEF4nKK71RtRQUi/fc3CAlZBeJyiu9UbUUFIv33NwgJWQbD6XM2N+05B";
    public const string SistanVaBaluchestan = "AQMAAAABAAAABQAAAODgC79f+1hBCPmg9xQARkFUliFKwONaQQj5oPcUAEZBVJYhSsDjWkGgoiPB6DBMQeDgC79f+1hBoKIjwegwTEHg4Au/X/tYQQj5oPcUAEZB";
    public const string Tehran = "AQMAAAABAAAABQAAAHASg9QLYlVB0JFcbjWgT0EYldSJ3ZJWQdCRXG41oE9BGJXUid2SVkHwQc8iCXpQQXASg9QLYlVB8EHPIgl6UEFwEoPUC2JVQdCRXG41oE9B";
    public const string Yazd = "AQMAAAABAAAABQAAAGSIY203bFZBAET6FX1VSkE07zhZFw9YQQBE+hV9VUpBNO84WRcPWEEQ4C2wPRZOQWSIY203bFZBEOAtsD0WTkFkiGNtN2xWQQBE+hV9VUpB";
    public const string Zanjan = "AQMAAAABAAAABQAAAPTb1zFFCFRB2PD09kUrUEGwcmghQP5UQdjw9PZFK1BBsHJoIUD+VEHgWBercBFRQfTb1zFFCFRB4FgXq3ARUUH029cxRQhUQdjw9PZFK1BB";

    public static BoundingBox ToBoundingBox(IriProvince93 iriProvinces)
    {
        string wmBase64Envelope = string.Empty;

        switch (iriProvinces)
        {
            case IriProvince93.Alborz:
                wmBase64Envelope = Alborz;
                break;

            case IriProvince93.Ardabil:
                wmBase64Envelope = Ardabil;
                break;

            case IriProvince93.AzarbayejaneGarbi:
                wmBase64Envelope = AzarbayejaneGarbi;
                break;

            case IriProvince93.AzarbayejaneShargi:
                wmBase64Envelope = AzarbayejaneShargi;
                break;

            case IriProvince93.Booshehr:
                wmBase64Envelope = Booshehr;
                break;

            case IriProvince93.ChaharmahalVaBakhtiari:
                wmBase64Envelope = ChaharmahalVaBakhtiari;
                break;

            case IriProvince93.Isfahan:
                wmBase64Envelope = Isfahan;
                break;

            case IriProvince93.Fars:
                wmBase64Envelope = Fars;
                break;

            case IriProvince93.Qazvin:
                wmBase64Envelope = Qazvin;
                break;

            case IriProvince93.Gilan:
                wmBase64Envelope = Gilan;
                break;

            case IriProvince93.Golestan:
                wmBase64Envelope = Golestan;
                break;

            case IriProvince93.Hamadan:
                wmBase64Envelope = Hamadan;
                break;

            case IriProvince93.Hormozgan:
                wmBase64Envelope = Hormozgan;
                break;

            case IriProvince93.Ilam:
                wmBase64Envelope = Ilam;
                break;

            case IriProvince93.Kerman:
                wmBase64Envelope = Kerman;
                break;

            case IriProvince93.Kermanshah:
                wmBase64Envelope = Kermanshah;
                break;

            case IriProvince93.KhorasanJonoobi:
                wmBase64Envelope = KhorasanJonoobi;
                break;

            case IriProvince93.KhorasanRazavi:
                wmBase64Envelope = KhorasanRazavi;
                break;

            case IriProvince93.KhorasanShomali:
                wmBase64Envelope = KhorasanShomali;
                break;

            case IriProvince93.Khozestan:
                wmBase64Envelope = Khozestan;
                break;

            case IriProvince93.KohgiluyehVaBoyerahmad:
                wmBase64Envelope = KohgiluyehVaBoyerahmad;
                break;

            case IriProvince93.Kordestan:
                wmBase64Envelope = Kordestan;
                break;

            case IriProvince93.Lorestan:
                wmBase64Envelope = Lorestan;
                break;

            case IriProvince93.Markazi:
                wmBase64Envelope = Markazi;
                break;

            case IriProvince93.Mazandaran:
                wmBase64Envelope = Mazandaran;
                break;

            case IriProvince93.Qom:
                wmBase64Envelope = Qom;
                break;

            case IriProvince93.Semnan:
                wmBase64Envelope = Semnan;
                break;

            case IriProvince93.SistanVaBaluchestan:
                wmBase64Envelope = SistanVaBaluchestan;
                break;

            case IriProvince93.Tehran:
                wmBase64Envelope = Tehran;
                break;

            case IriProvince93.Yazd:
                wmBase64Envelope = Yazd;
                break;

            case IriProvince93.Zanjan:
                wmBase64Envelope = Zanjan;
                break;
        }

        byte[] envelope = Convert.FromBase64String(wmBase64Envelope);

        var geometry = Geometry<Point>.FromWkb(envelope, SridHelper.WebMercator);

        return geometry.GetBoundingBox();
    }

    public static BoundingBox ToBoundingBox(params IriProvince93[] iriProvinces)
    {
        if (iriProvinces.IsNullOrEmpty())
            return BoundingBox.NaN;

        var boundingBoxes = iriProvinces.Select(ToBoundingBox).ToList();

        return BoundingBox.GetMergedBoundingBox(boundingBoxes, true);
    }
}