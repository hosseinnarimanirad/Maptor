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
	/// Wireless interface state
	/// </summary>
	/// <remarks>Equivalent to WLAN_INTERFACE_STATE</remarks>
	public enum InterfaceState
	{
		/// <summary>
		/// The interface is not ready to operate.
		/// </summary>
		NotReady = 0,

		/// <summary>
		/// The interface is connected to a network.
		/// </summary>
		Connected,

		/// <summary>
		/// The interface is the first node in an ad hoc network. No peer has connected.
		/// </summary>
		AdHocNetworkFormed,

		/// <summary>
		/// The interface is disconnecting from the current network.
		/// </summary>
		Disconnecting,

		/// <summary>
		/// The interface is not connected to any network.
		/// </summary>
		Disconnected,

		/// <summary>
		/// The interface is attempting to associate with a network.
		/// </summary>
		Associating,

		/// <summary>
		/// Auto configuration is discovering the settings for the network.
		/// </summary>
		Discovering,

		/// <summary>
		/// The interface is in the process of authenticating.
		/// </summary>
		Authenticating
	}

	internal static class InterfaceStateConverter
	{
		public static InterfaceState Convert(WLAN_INTERFACE_STATE source) =>
			(InterfaceState)source;

		public static WLAN_INTERFACE_STATE ConvertBack(InterfaceState source) =>
			(WLAN_INTERFACE_STATE)source;
	}
}