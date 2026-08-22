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

namespace IRI.Maptor.Infrastructure.WindowsBase.ManagedNativeWifi
{
	/// <summary>
	/// Wireless LAN information
	/// </summary>
	public class BssNetworkPack
	{
		/// <summary>
		/// Associated wireless interface information
		/// </summary>
		public InterfaceInfo Interface { get; }

		/// <summary>
		/// SSID (maximum 32 bytes)
		/// </summary>
		public NetworkIdentifier Ssid { get; }

		/// <summary>
		/// BSS network type
		/// </summary>
		public BssType BssType { get; }

		/// <summary>
		/// BSSID (6 bytes)
		/// </summary>
		public NetworkIdentifier Bssid { get; }

		/// <summary>
		/// Signal strength (RSSI)
		/// </summary>
		public int SignalStrength { get; }

		/// <summary>
		/// Link quality (0-100)
		/// </summary>
		public int LinkQuality { get; }

		/// <summary>
		/// Wireless LAN frequency (KHz)
		/// </summary>
		public int Frequency { get; }

		/// <summary>
		/// Wireless LAN channel
		/// </summary>
		public int Channel { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		public BssNetworkPack(
			InterfaceInfo interfaceInfo,
			NetworkIdentifier ssid,
			BssType bssType,
			NetworkIdentifier bssid,
			int signalStrength,
			int linkQuality,
			int frequency,
			int channel)
		{
			this.Interface = interfaceInfo;
			this.Ssid = ssid;
			this.BssType = bssType;
			this.Bssid = bssid;
			this.SignalStrength = signalStrength;
			this.LinkQuality = linkQuality;
			this.Frequency = frequency;
			this.Channel = channel;
		}
	}
}