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
	/// 802.11 PHY and media type
	/// </summary>
	public enum PhyType
	{
		/// <summary>
		/// Unknown or uninitialized PHY type
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Any PHY type
		/// </summary>
		Any,

		/// <summary>
		/// Frequency-hopping spread-spectrum (FHSS) PHY type
		/// </summary>
		Fhss,

		/// <summary>
		/// Direct sequence spread spectrum (DSSS) PHY type
		/// </summary>
		Dsss,

		/// <summary>
		/// Infrared (IR) baseband PHY type
		/// </summary>
		IrBaseband,

		/// <summary>
		/// Orthogonal frequency division multiplexing (OFDM) PHY type
		/// </summary>
		Ofdm,

		/// <summary>
		/// High-rate DSSS (HRDSSS) PHY type
		/// </summary>
		HrDsss,

		/// <summary>
		/// Extended rate PHY (ERP) type
		/// </summary>
		Erp,

		/// <summary>
		/// 802.11n PHY type
		/// </summary>
		Ht,

		/// <summary>
		/// 802.11ac PHY type
		/// </summary>
		Vht,

		/// <summary>
		/// The start of the range that is used to define PHY types that are developed by
		/// an independent hardware vendor (IHV)
		/// </summary>
		IhvStart,

		/// <summary>
		/// The start of the range that is used to define PHY types that are developed by
		/// an independent hardware vendor (IHV)
		/// </summary>
		IhvEnd
	}

	internal static class PhyTypeConverter
	{
		public static PhyType Convert(DOT11_PHY_TYPE source)
		{
			switch (source)
			{
				case DOT11_PHY_TYPE.dot11_phy_type_any:
					return PhyType.Any;
				case DOT11_PHY_TYPE.dot11_phy_type_fhss:
					return PhyType.Fhss;
				case DOT11_PHY_TYPE.dot11_phy_type_dsss:
					return PhyType.Dsss;
				case DOT11_PHY_TYPE.dot11_phy_type_irbaseband:
					return PhyType.IrBaseband;
				case DOT11_PHY_TYPE.dot11_phy_type_ofdm:
					return PhyType.Ofdm;
				case DOT11_PHY_TYPE.dot11_phy_type_hrdsss:
					return PhyType.HrDsss;
				case DOT11_PHY_TYPE.dot11_phy_type_erp:
					return PhyType.Erp;
				case DOT11_PHY_TYPE.dot11_phy_type_ht:
					return PhyType.Ht;
				case DOT11_PHY_TYPE.dot11_phy_type_vht:
					return PhyType.Vht;
				case DOT11_PHY_TYPE.dot11_phy_type_IHV_start:
					return PhyType.IhvStart;
				case DOT11_PHY_TYPE.dot11_phy_type_IHV_end:
					return PhyType.IhvEnd;
				default:
					return PhyType.Unknown;
			}
		}

		public static DOT11_PHY_TYPE ConvertBack(PhyType source)
		{
			switch (source)
			{
				case PhyType.Any:
					return DOT11_PHY_TYPE.dot11_phy_type_any;
				case PhyType.Fhss:
					return DOT11_PHY_TYPE.dot11_phy_type_fhss;
				case PhyType.Dsss:
					return DOT11_PHY_TYPE.dot11_phy_type_dsss;
				case PhyType.IrBaseband:
					return DOT11_PHY_TYPE.dot11_phy_type_irbaseband;
				case PhyType.Ofdm:
					return DOT11_PHY_TYPE.dot11_phy_type_ofdm;
				case PhyType.HrDsss:
					return DOT11_PHY_TYPE.dot11_phy_type_hrdsss;
				case PhyType.Erp:
					return DOT11_PHY_TYPE.dot11_phy_type_erp;
				case PhyType.Ht:
					return DOT11_PHY_TYPE.dot11_phy_type_ht;
				case PhyType.Vht:
					return DOT11_PHY_TYPE.dot11_phy_type_vht;
				case PhyType.IhvStart:
					return DOT11_PHY_TYPE.dot11_phy_type_IHV_start;
				case PhyType.IhvEnd:
					return DOT11_PHY_TYPE.dot11_phy_type_IHV_end;
				default:
					return DOT11_PHY_TYPE.dot11_phy_type_unknown;
			}
		}
	}
}