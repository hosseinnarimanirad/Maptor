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
	/// Wireless profile information
	/// </summary>
	public class ProfilePack
	{
		/// <summary>
		/// Profile name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Associated wireless interface information
		/// </summary>
		public InterfaceInfo Interface { get; }

		/// <summary>
		/// Profile type
		/// </summary>
		public ProfileType ProfileType { get; }

		/// <summary>
		/// Profile XML document
		/// </summary>
		public ProfileDocument Document { get; }

		/// <summary>
		/// Profile XML string
		/// </summary>
		[Obsolete("Use Document.ToString method instead.")]
		public string ProfileXml => Document.ToString();

		/// <summary>
		/// SSID of associated wireless LAN
		/// </summary>
		[Obsolete("Use Document.Ssid property instead.")]
		public NetworkIdentifier Ssid => Document.Ssid;

		/// <summary>
		/// BSS network type of associated wireless LAN
		/// </summary>
		[Obsolete("Use Document.BssType property instead.")]
		public BssType BssType => Document.BssType;

		/// <summary>
		/// Authentication of associated wireless LAN
		/// </summary>
		[Obsolete("Use Document.Authentication property instead.")]
		public string Authentication => Document.AuthenticationString;

		/// <summary>
		/// Encryption of associated wireless LAN
		/// </summary>
		[Obsolete("Use Document.Encryption property instead.")]
		public string Encryption => Document.EncryptionString;

		/// <summary>
		/// Whether this profile is set to be automatically connected
		/// </summary>
		[Obsolete("Use Document.IsAutoConnectEnabled property instead.")]
		public bool IsAutomatic => Document.IsAutoConnectEnabled;

		/// <summary>
		/// Position in preference order of associated wireless interface
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Signal quality of associated wireless LAN
		/// </summary>
		public int SignalQuality { get; }

		/// <summary>
		/// Whether this profile is currently connected
		/// </summary>
		public bool IsConnected { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProfilePack(
			string name,
			InterfaceInfo interfaceInfo,
			ProfileType profileType,
			string profileXml,
			int position,
			int signalQuality,
			bool isConnected)
		{
			this.Name = name;
			this.Interface = interfaceInfo;
			this.ProfileType = profileType;
			Document = new ProfileDocument(profileXml);
			this.Position = position;
			this.SignalQuality = signalQuality;
			this.IsConnected = isConnected;
		}
	}
}