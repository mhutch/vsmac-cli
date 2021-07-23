// 
// Derived from monodevelop/main/src/addins/MacPlatform/MacInterop/CoreFoundation.cs 
//
// Copyright (c) Microsoft Corp.
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VSMacLocator
{
	static class MacInterop
	{
		const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";
		const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/Versions/A/ApplicationServices";
		const int kCFStringEncodingUTF8 = 0x08000100;

		static MacInterop()
		{
			if (IntPtr.Size != 8)
			{
				throw new NotSupportedException("Only 64bit is supported");
			}
		}

		static CFStringRef CreateString(string value) => CFStringCreateWithCString(IntPtr.Zero, value, kCFStringEncodingUTF8);

		static string GetString(CFStringRef handle)
		{
			if (handle == IntPtr.Zero)
			{
				return null;
			}

			string value;

			int length = CFStringGetLength(handle);
			IntPtr stringPtr = CFStringGetCharactersPtr(handle);

			var buffer = IntPtr.Zero;

			if (stringPtr == IntPtr.Zero)
			{
				CFRange r = new CFRange(0, length);
				buffer = Marshal.AllocCoTaskMem(length * 2);
				CFStringGetCharacters(handle, r, buffer);
				stringPtr = buffer;
			}
			unsafe
			{
				value = new string((char*)stringPtr, 0, length);
			}

			if (buffer != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(buffer);
			}

			return value;
		}

		public static string[] GetApplicationUrls(string bundleIdentifier, out long errorCode)
		{
			errorCode = 0;

			var bundleId = CreateString(bundleIdentifier);

			CFArrayRef urlArr = LSCopyApplicationURLsForBundleIdentifier(bundleId, out CFErrorRef errorRef);

			if (errorRef != IntPtr.Zero)
			{
				errorCode = CFErrorGetCode(errorRef).ToInt64();
				CFRelease(errorRef);
				return null;
			}


			var count = CFArrayGetCount(urlArr);
			var urls = new string[count];
			for (long i = 0; i < count; i++)
			{
				var urlRef = (CFUrlRef)CFArrayGetValueAtIndex(urlArr, i);
				var pathStr = CFURLCopyFileSystemPath(urlRef, CFUrlPathStyle.Posix);
				urls[i] = GetString(pathStr);
				CFRelease(pathStr);
			}

			CFRelease(urlArr);

			return urls;
		}

		[DllImport(CoreFoundation)]
		static extern CFStringRef CFStringCreateWithCString(IntPtr alloc, string str, int encoding);

		[DllImport(CoreFoundation)]
		static extern void CFRelease(IntPtr handle);

		[DllImport(CoreFoundation)]
		extern static int CFStringGetLength(CFStringRef handle);

		[DllImport(CoreFoundation)]
		extern static IntPtr CFStringGetCharactersPtr(CFStringRef handle);

		[DllImport(CoreFoundation, CharSet = CharSet.Unicode)]
		extern static IntPtr CFStringGetCharacters(CFStringRef handle, CFRange range, IntPtr buffer);

		[DllImport(CoreFoundation)]
		extern static CFStringRef CFURLCopyFileSystemPath(CFUrlRef anUrl, CFUrlPathStyle pathStyle);

		[DllImport(CoreFoundation)]
		extern static CFIndex CFArrayGetCount(CFArrayRef theArray);

		[DllImport(CoreFoundation)]
		extern static IntPtr CFArrayGetValueAtIndex(CFArrayRef theArray, CFIndex idx);

		[DllImport(ApplicationServices)]
		extern static CFArrayRef LSCopyApplicationURLsForBundleIdentifier(CFStringRef inBundleIdentifierRef, out CFErrorRef errorRef);

		[DllImport(CoreFoundation)]
		extern static IntPtr CFErrorGetCode(CFErrorRef errorRef);

		[DllImport(CoreFoundation)]
		extern static CFReadStreamRef CFReadStreamCreateWithFile(IntPtr alloc, CFUrlRef url);

		[DllImport(CoreFoundation)]
		extern static bool CFReadStreamOpen(CFReadStreamRef stream);

		[DllImport(CoreFoundation)]
		extern static void CFReadStreamClose(CFReadStreamRef stream);

		[DllImport(CoreFoundation)]
		extern static CFPropertyListRef CFPropertyListCreateWithStream(
			IntPtr alloc, CFReadStreamRef stream, CFIndex streamLength, CFPropertyListMutabilityOptions options,
			out CFPropertyListFormat format, out CFErrorRef error);

		[DllImport(CoreFoundation)]
		extern static IntPtr CFDictionaryGetValue(CFDictionaryRef theDict, IntPtr key);

		[DllImport(CoreFoundation)]
		extern static CFUrlRef CFURLCreateWithFileSystemPath(IntPtr allocator, CFStringRef filePath, CFUrlPathStyle pathStyle, bool isDirectory);

		struct CFUrlRef
		{
			CFUrlRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static implicit operator IntPtr(CFUrlRef r) => r.Ptr;
			public static explicit operator CFUrlRef(IntPtr i) => new CFUrlRef(i);
		}

		struct CFStringRef
		{
			CFStringRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static implicit operator IntPtr(CFStringRef r) => r.Ptr;
			public static explicit operator CFStringRef(IntPtr i) => new CFStringRef(i);
		}

		struct CFArrayRef
		{
			CFArrayRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static implicit operator IntPtr(CFArrayRef r) => r.Ptr;
			public static explicit operator CFArrayRef(IntPtr i) => new CFArrayRef(i);
		}

		struct CFErrorRef
		{
			CFErrorRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static implicit operator IntPtr(CFErrorRef r) => r.Ptr;
			public static explicit operator CFErrorRef(IntPtr i) => new CFErrorRef(i);
		}

		struct CFReadStreamRef
		{
			CFReadStreamRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static implicit operator IntPtr(CFReadStreamRef r) => r.Ptr;
			public static explicit operator CFReadStreamRef(IntPtr i) => new CFReadStreamRef(i);
		}

		struct CFDictionaryRef
		{
			CFDictionaryRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static implicit operator IntPtr(CFDictionaryRef r) => r.Ptr;
			public static explicit operator CFDictionaryRef(IntPtr i) => new CFDictionaryRef(i);
		}

		readonly struct CFPropertyListRef
		{
			CFPropertyListRef(IntPtr ptr) => Ptr = ptr;
			readonly IntPtr Ptr;
			public static explicit operator CFDictionaryRef(CFPropertyListRef r) => (CFDictionaryRef)r.Ptr;
			public static implicit operator IntPtr(CFPropertyListRef r) => r.Ptr;
			public static explicit operator CFPropertyListRef(IntPtr i) => new CFPropertyListRef(i);
		}

		// NOTE this would be 4bytes on 32bit
		struct CFIndex
		{
			public CFIndex(long value) => Value = value;
			public long Value;
			public static implicit operator CFIndex(int c) => new(c);
			public static implicit operator CFIndex(long l) => new(l);
			public static implicit operator long(CFIndex i) => i.Value;
		}

		struct CFRange
		{
			public CFIndex Index, Length;
			public CFRange(CFIndex index, CFIndex length)
			{
				Index = index;
				Length = length;
			}
		}

		enum CFPropertyListFormat : long
		{
			OpenStep = 1,
			Xml_v1_0 = 100,
			Binary = 200
		}

		[Flags]
		enum CFPropertyListMutabilityOptions : ulong
		{
			Immutable = 0,
			MutableContainers = 1,
			MutableContainersAndLeaves = 2
		}

		enum CFUrlPathStyle
		{
			Posix = 0,
			Hfs = 1,
			Windows = 2
		};


		public static Dictionary<string, string> GetStringValuesFromPlist(string plistPath, params string[] keys)
		{
			var results = new Dictionary<string, string>();

			//could also use CFUrlCreateFromFileSystemRepresentation but isn't obvious how that handles encoding
			var pathStr = CreateString(plistPath);
			var url = CFURLCreateWithFileSystemPath(IntPtr.Zero, pathStr, CFUrlPathStyle.Posix, false);
			CFRelease(pathStr);

			var stream = CFReadStreamCreateWithFile(IntPtr.Zero, url);
			CFReadStreamOpen(stream);

			var dict = (CFDictionaryRef)CFPropertyListCreateWithStream(
				IntPtr.Zero, stream, 0, CFPropertyListMutabilityOptions.Immutable,
				out CFPropertyListFormat format, out CFErrorRef error);

			foreach (var key in keys)
			{
				var keyStr = CreateString(key);
				var val = (CFStringRef)CFDictionaryGetValue(dict, keyStr);
				CFRelease(keyStr);

				if (val != IntPtr.Zero)
				{
					var valStr = GetString(val);
					results[key] = valStr;
				}
			}

			CFRelease(dict);
			CFReadStreamClose(stream);
			CFRelease(stream);

			return results;
		}
	}
}