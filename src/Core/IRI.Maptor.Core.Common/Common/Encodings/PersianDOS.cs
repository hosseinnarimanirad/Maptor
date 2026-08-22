using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Encodings;

public class PersianDOS : Encoding
{
    //static Dictionary<byte, string> customEncoding = new Dictionary<byte, string>()
    //{
    //    {175, "ط"},{162, "د"},{163, "ذ"},{170, "ش"},
    //    {164, "ر"},{144, "ا"},{172, "ص"},{249, "ه "},
    //    {165, "ز"},{145, "ا"},{238, "ک"},{147, "ب"},
    //    {158, "ح "},{246, "ن "},{250, "ه"},{150, "ت "},
    //    {159, "ح"},{247, "ن"},{155, "ج"},{151, "ت"},
    //    {254, "ی"},{243, "ل"},{161, "خ"},{149,"پ"},
    //    {255, "ی "},{241, "ل "},{240, "گ"},{236,"ق"},
    //    {253, "ی "},{248, "و"},{234, "ف"},{152,"ث "},
    //    {245, "م"},{232, "غ"},{174, "ض"},{226,"ع "},
    //    {244, "م "},{231, "غ"},{168, "س"},{237, "ک "},{252,"ی "},
    //    {128, "0"},{129, "1"},{130, "2"},{131, "3"},{132, "4"},{133, "5"},{134, "6"},{135, "7"},{136, "8"},{137, "9"},


    //    {228,"ع"},{146,"ب "}, {160,"خ "}, {169, "ش "}, {251,"ه"}, {148, "پ "},{235,"ق "}, {157, "چ"}, {156,"چ "} ,{227, "ع"}, {171, "ص "}, {173, "ض "}, {233, "ف "}, {224, "ظ"},
    //    {154, "ج "},{167,"س "},  {153, "ث"},   {229, "غ "}, {230, "غ "}, {225, "ع "}, {239, "گ "},{166, "ژ"},{242,"لا"},{143,"ء"},

    //    {142, "ئ"}, {141, "آ"}
    //};

    static char[] charList = new char[256];

    static ReadOnlyCollection<int> iranSystem = new ReadOnlyCollection<int>(new int[]{
            0x06F0, 0x06F1, 0x06F2, 0x06F3, 0x06F4, 0x06F5, 0x06F6, 0x06F7, 0x06F8, 0x06F9, 0x060C, 0x0640, 0x061F, 0xFE81, 0xFE8B, 0x0621,
            0xFE8D, 0xFE8E, 0xFE8F, 0xFE91, 0xFB56, 0xFB58, 0xFE95, 0xFE97, 0xFE99, 0xFE9B, 0xFE9D, 0xFE9F, 0xFB7C, 0xFB7C, 0xFEA1, 0xFEA3,
            0xFEA5, 0xFEA7, 0x062F, 0x0630, 0x0631, 0x0632, 0x0698, 0xFEB1, 0xFEB3, 0xFEB5, 0xFEB7, 0xFEB9, 0xFEBB, 0xFEBD, 0xFEBF, 0x0637,
            0x2591, 0x2592, 0x2593, 0x2502, 0x2524, 0x2561, 0x2562, 0x2556, 0x2555, 0x2563, 0x2551, 0x2557, 0x255D, 0x255C, 0x255B, 0x2510,
            0x2514, 0x2534, 0x252C, 0x251C, 0x2500, 0x253C, 0x255E, 0x255F, 0x255A, 0x2554, 0x2569, 0x2566, 0x2560, 0x2550, 0x256C, 0x2567,
            0x2568, 0x2564, 0x2565, 0x2559, 0x2558, 0x2552, 0x2553, 0x256B, 0x256A, 0x2518, 0x250C, 0x2588, 0x2584, 0x258C, 0x2590, 0x2580,
            0x0638, 0xFEC9, 0xFECA, 0xFECC, 0xFECB, 0xFECD, 0xFECE, 0xFED0, 0xFECF, 0xFED1, 0xFED3, 0xFED5, 0xFED7, 0xFB8E, 0xFB90, 0xFB92,
            0xFB94, 0xFEDD, 0xFEFB, 0xFEDF, 0xFEE1, 0xFEE3, 0xFEE5, 0xFEE7, 0x0648, 0xFEE9, 0xFEEC, 0xFEEB, 0xFBFD, 0xFBFC, 0xFBFE, 0x00A0 });

    public static ReadOnlyCollection<int> IranSystem
    {
        get { return iranSystem; }
    }

    static PersianDOS()
    {

        for (int i = 0; i < 128; i++) charList[i] = (char)i;

        for (int i = 128; i < 256; i++) charList[i] = (char)IranSystem[i - 128];
    }


    public override string GetString(byte[] bytes)
    {
        var stringBuilder = new StringBuilder();

        foreach (var byteValue in bytes)
        {
            stringBuilder.Append(charList[byteValue]);
        }

        return EncodingConverter.PersianDosToPersianUnicode(stringBuilder.ToString());
    }

    public override int GetByteCount(char[] chars, int index, int count)
    {
        throw new NotImplementedException();
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        throw new NotImplementedException();
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        throw new NotImplementedException();
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        throw new NotImplementedException();
    }

    public override int GetMaxByteCount(int charCount)
    {
        throw new NotImplementedException();
    }

    public override int GetMaxCharCount(int byteCount)
    {
        throw new NotImplementedException();
    }

    //public override byte[] GetBytes(string s)
    //{
    //    var unicodes = s.SelectMany(i => (int)i).ToArray();

    //    var result = unicodes.SelectMany(i => BitConverter.GetBytes(i));

    //}
}
