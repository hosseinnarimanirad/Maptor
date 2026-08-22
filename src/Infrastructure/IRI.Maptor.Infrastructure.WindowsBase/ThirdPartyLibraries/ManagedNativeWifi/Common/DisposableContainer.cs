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

namespace IRI.Maptor.Infrastructure.WindowsBase.ManagedNativeWifi.Common
{
	/// <summary>
	/// Container of disposable object
	/// </summary>
	/// <typeparam name="T">Disposable object type</typeparam>
	/// <remarks>
	/// If a disposable object is given as content when this container is instantiated,
	/// the content object will not be disposed when this container is disposed.
	/// In contrast, if no disposable object is given (if it is default, in the case of class, null)
	/// as content when this container is instantiated, a new disposable object is instantiated
	/// instead and the content object will be disposed when this container is disposed.
	///	</remarks>
	internal class DisposableContainer<T> : IDisposable where T : IDisposable, new()
	{
		private readonly bool _isDefault;
		public T Content { get; }

		public DisposableContainer(T content)
		{
			_isDefault = EqualityComparer<T>.Default.Equals(content, default(T));
			this.Content = _isDefault ? new T() : content;
		}

		#region Dispose

		bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				if (_isDefault)
					Content.Dispose();
			}

			_disposed = true;
		}

		#endregion
	}
}