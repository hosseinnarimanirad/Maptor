using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace IRI.Maptor.Core.Common.Helpers;

public static class SecureStringHelper
{
    public static bool SecureStringEqual(SecureString secureString1, SecureString secureString2)
    {
        if (secureString1 == null)
        {
            //throw new ArgumentNullException("s1");
            return false;
        }
        if (secureString2 == null)
        {
            //throw new ArgumentNullException("s2");
            return false;
        }

        if (secureString1.Length != secureString2.Length)
        {
            return false;
        }

        IntPtr ss_bstr1_ptr = IntPtr.Zero;
        IntPtr ss_bstr2_ptr = IntPtr.Zero;

        try
        {
            ss_bstr1_ptr = Marshal.SecureStringToBSTR(secureString1);
            ss_bstr2_ptr = Marshal.SecureStringToBSTR(secureString2);

            String str1 = Marshal.PtrToStringBSTR(ss_bstr1_ptr);
            String str2 = Marshal.PtrToStringBSTR(ss_bstr2_ptr);

            return str1.Equals(str2);
        }
        finally
        {
            if (ss_bstr1_ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeBSTR(ss_bstr1_ptr);
            }

            if (ss_bstr2_ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeBSTR(ss_bstr2_ptr);
            }
        }
    }

    public static string GetString(SecureString secureString)
    {
        return new NetworkCredential("", secureString).Password;
    }

    public static SecureString GetSecureString(string @string)
    {
        return new NetworkCredential("", @string).SecurePassword;
    }
}
