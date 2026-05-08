using System;

namespace IRI.Maptor.Extensions;

public static class StringExtensions
{
    const char arabicYa01 = '\u0649';
    const char arabicYa02 = '\u064A';
    const char arabicKaf = '\u0643';

    const char farsiYa = '\u06CC';
    const char farsiKaf = '\u06A9';

    public static string ArabicToFarsi(this string arabicString)
    {
        if (string.IsNullOrWhiteSpace(arabicString))
            return arabicString;

        return arabicString
            .Replace(arabicYa01, farsiYa)
            .Replace(arabicYa02, farsiYa)
            .Replace(arabicKaf, farsiKaf);
    }

    public static string FixPersianChars(this string str)
    {
        return str.Replace("ﮎ", "ک")
            .Replace("ﮏ", "ک")
            .Replace("ﮐ", "ک")
            .Replace("ﮑ", "ک")
            .Replace("ك", "ک")
            .Replace("ي", "ی")
            .Replace(" ", " ")
            .Replace("‌", " ")
            .Replace("ھ", "ه");//.Replace("ئ", "ی");


        //// better approach (performance) but more code
        //char[] persianDigits = { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        //char[] result = str.ToCharArray();
        //for (int i = 0; i < result.Length; i++)
        //{
        //    if (char.IsDigit(result[i]))
        //        result[i] = persianDigits[result[i] - '0'];
        //}
        //return new string(result);
    }

    public static string LatinNumbersToFarsiNumbers(this string value)
    {
        return value.Replace('1', '۱')
                .Replace('2', '۲')
                .Replace('3', '۳')
                .Replace('4', '۴')
                .Replace('5', '۵')
                .Replace('6', '۶')
                .Replace('7', '۷')
                .Replace('8', '۸')
                .Replace('9', '۹')
                .Replace('0', '۰')
                .Replace('.', '\u066B');
    }

    public static string FarsiNumbersToLatinNumbers(this string value)
    {
        return value.Replace('۱', '1')
                    .Replace('۲', '2')
                    .Replace('۳', '3')
                    .Replace('۴', '4')
                    .Replace('۵', '5')
                    .Replace('۶', '6')
                    .Replace('۷', '7')
                    .Replace('۸', '8')
                    .Replace('۹', '9')
                    .Replace('۰', '0')
                    .Replace('\u066B', '.')
                    //iphone numeric
                    .Replace("٠", "0")
                    .Replace("١", "1")
                    .Replace("٢", "2")
                    .Replace("٣", "3")
                    .Replace("٤", "4")
                    .Replace("٥", "5")
                    .Replace("٦", "6")
                    .Replace("٧", "7")
                    .Replace("٨", "8")
                    .Replace("٩", "9");
    }

    public static bool ContainsIgnoreCase(this string value, string term)
    {
        return value.IndexOf(term, StringComparison.InvariantCultureIgnoreCase) >= 0;
    }

    public static bool EqualsIgnoreCase(this string theString, string value)
    {
        return theString.Equals(value, StringComparison.InvariantCultureIgnoreCase);
    }

    public static string AsBase64String(this string value)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    }

    public static string Base64ToNormalString(this string base64String)
    {
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }

    public static string[] SplitIgnoreEmptyEntries(this string value, char separator)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return value.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);
    }
}
