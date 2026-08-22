using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Encodings;

public static class EncodingConverter
{

    static string[] iranSystemEquivalent = new string[] {
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "،" , "-", "؟", "آ", "ئ", "ء",
            "ا", "ا", "ب ", "ب", "پ ", "پ", "ت ", "ت", "ث ", "ث", "ج ", "ج", "چ ", "چ", "ح ", "ح",
            "خ ", "خ", "د", "ذ", "ر", "ز", "ژ", "س ", "س", "ش ", "ش", "ص ", "ص", "ض ", "ض", "ط",
            "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*",
            "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*",
            "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*", "*",
            "ظ", "ع ", "ع ", "ع", "ع", "غ ", "غ ", "غ", "غ", "ف ", "ف", "ق ", "ق", "ک ", "ک", "گ ",
            "گ", "ل ", "لا", "ل", "م ", "م", "ن ", "ن", "و", "ه ", "ه", "ه", "ی ", "ی ", "ی"};//, 0x00A0 };

    static PersianDOS persianDosEncoding = new PersianDOS();

    static List<int> iranSystem = new List<int> {
            0x06F0, 0x06F1, 0x06F2, 0x06F3, 0x06F4, 0x06F5, 0x06F6, 0x06F7, 0x06F8, 0x06F9, 0x060C, 0x0640, 0x061F, 0xFE81, 0xFE8B, 0x0621,
            0xFE8D, 0xFE8E, 0xFE8F, 0xFE91, 0xFB56, 0xFB58, 0xFE95, 0xFE97, 0xFE99, 0xFE9B, 0xFE9D, 0xFE9F, 0xFB7C, 0xFB7C, 0xFEA1, 0xFEA3,
            0xFEA5, 0xFEA7, 0x062F, 0x0630, 0x0631, 0x0632, 0x0698, 0xFEB1, 0xFEB3, 0xFEB5, 0xFEB7, 0xFEB9, 0xFEBB, 0xFEBD, 0xFEBF, 0x0637,
            0x2591, 0x2592, 0x2593, 0x2502, 0x2524, 0x2561, 0x2562, 0x2556, 0x2555, 0x2563, 0x2551, 0x2557, 0x255D, 0x255C, 0x255B, 0x2510,
            0x2514, 0x2534, 0x252C, 0x251C, 0x2500, 0x253C, 0x255E, 0x255F, 0x255A, 0x2554, 0x2569, 0x2566, 0x2560, 0x2550, 0x256C, 0x2567,
            0x2568, 0x2564, 0x2565, 0x2559, 0x2558, 0x2552, 0x2553, 0x256B, 0x256A, 0x2518, 0x250C, 0x2588, 0x2584, 0x258C, 0x2590, 0x2580,
            0x0638, 0xFEC9, 0xFECA, 0xFECC, 0xFECB, 0xFECD, 0xFECE, 0xFED0, 0xFECF, 0xFED1, 0xFED3, 0xFED5, 0xFED7, 0xFB8E, 0xFB90, 0xFB92,
            0xFB94, 0xFEDD, 0xFEFB, 0xFEDF, 0xFEE1, 0xFEE3, 0xFEE5, 0xFEE7, 0x0648, 0xFEE9, 0xFEEC, 0xFEEB, 0xFBFD, 0xFBFC, 0xFBFE, 0x00A0 };

    public static string PersianDosToPersianUnicode(string persianDosString)
    {
        var unicodeValues = persianDosString.ToCharArray().Select(i => (int)i).ToArray();

        var convertedValues = unicodeValues.Select(i => iranSystem.Contains(i) ? iranSystemEquivalent[iranSystem.IndexOf(i)] : "*").ToArray();

        return string.Join(string.Empty, convertedValues);
    }


}
