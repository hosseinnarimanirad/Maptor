//The MIT License(MIT)

//Copyright(c) 2015 EMO

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

//https://github.com/emoacht/ManagedNativeWifi

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static IRI.Maptor.Infrastructure.WindowsBase.ManagedNativeWifi.Win32.NativeMethod;

namespace IRI.Maptor.Infrastructure.WindowsBase.ManagedNativeWifi
{
	/// <summary>
	/// BSS network type
	/// </summary>
	public enum BssType
	{
		/// <summary>
		/// None (invalid value)
		/// </summary>
		None = 0,

		/// <summary>
		/// Infrastructure BSS network
		/// </summary>
		Infrastructure,

		/// <summary>
		/// Independent BSS (IBSS) network (Ad hoc network)
		/// </summary>
		Independent
	}

	internal static class BssTypeConverter
	{
		public static bool TryConvert(DOT11_BSS_TYPE source, out BssType bssType)
		{
			switch (source)
			{
				case DOT11_BSS_TYPE.dot11_BSS_type_infrastructure:
					bssType = BssType.Infrastructure;
					return true;
				case DOT11_BSS_TYPE.dot11_BSS_type_independent:
					bssType = BssType.Independent;
					return true;
			}
			bssType = default(BssType);
			return false;
		}

		public static bool TryParse(string source, out BssType bssType)
		{
			if (string.Equals("ESS", source, StringComparison.OrdinalIgnoreCase))
			{
				bssType = BssType.Infrastructure;
				return true;
			}
			if (string.Equals("IBSS", source, StringComparison.OrdinalIgnoreCase))
			{
				bssType = BssType.Independent;
				return true;
			}
			bssType = default(BssType);
			return false;
		}

		public static DOT11_BSS_TYPE ConvertBack(BssType source)
		{
			switch (source)
			{
				case BssType.Infrastructure:
					return DOT11_BSS_TYPE.dot11_BSS_type_infrastructure;
				case BssType.Independent:
					return DOT11_BSS_TYPE.dot11_BSS_type_independent;
			}
			throw new ArgumentException(nameof(source));
		}
	}
}