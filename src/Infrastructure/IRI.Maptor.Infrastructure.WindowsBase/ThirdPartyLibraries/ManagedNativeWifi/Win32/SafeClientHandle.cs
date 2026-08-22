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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static IRI.Maptor.Infrastructure.WindowsBase.ManagedNativeWifi.Win32.NativeMethod;

namespace IRI.Maptor.Infrastructure.WindowsBase.ManagedNativeWifi.Win32
{
	/// <summary>
	/// Wrapper class only for handle taken by WlanOpenHandle function in Native Wifi API
	/// </summary>
	/// <remarks>
	/// This implementation is based on:
	/// http://referencesource.microsoft.com/#mscorlib/system/runtime/interopservices/safehandle.cs
	/// </remarks>
	internal class SafeClientHandle : SafeHandle
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <remarks>This constructor is for P/Invoke.</remarks>
		private SafeClientHandle() : base(IntPtr.Zero, true)
		{ }

		public override bool IsInvalid => (handle == IntPtr.Zero);

		protected override bool ReleaseHandle()
		{
			var result = WlanCloseHandle(handle, IntPtr.Zero);
			return (result == ERROR_SUCCESS);
		}
	}
}