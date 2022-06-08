using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Artanim
{
	public class WindowUtils
	{
		[DllImport("kernel32.dll")]
		static extern uint GetCurrentThreadId();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);

		private const string UnityWindowClassName = "UnityWndClass";
		static IntPtr _hUnityWnd;

		public static IntPtr GetUnityWindowHandle()
		{
			if (_hUnityWnd == IntPtr.Zero)
			{
				uint threadId = GetCurrentThreadId();
				EnumThreadWindows(threadId, EnumThreadWindowsCallback, IntPtr.Zero);
			}
			return _hUnityWnd;
		}

		static bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam)
		{
			var classText = new StringBuilder(UnityWindowClassName.Length + 1);
			GetClassName(hWnd, classText, classText.Capacity);
			if (classText.ToString() == UnityWindowClassName)
			{
				_hUnityWnd = hWnd;
				return false;
			}
			return true;
		}
	}

#if OTHER_WAY_OF_GETTING_HWND
	public class WindowUtils
	{
		delegate bool EnumThreadWndProc(IntPtr hWnd, long lParam);

		[DllImport("user32.dll")]
		static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWndProc lpfn, IntPtr lParam);

		[DllImport("Kernel32.dll")]
		static extern int GetCurrentThreadId();

		static IntPtr _hUnityWnd;

		public static IntPtr GetUnityWindowHandle()
		{
			if (_hUnityWnd == IntPtr.Zero)
			{
				var threadId = GetCurrentThreadId();
				EnumThreadWindows(threadId, EnumThreadWindowsCallback, IntPtr.Zero);
			}
			return _hUnityWnd;
		}

		static bool EnumThreadWindowsCallback(IntPtr hWnd, long lParam)
		{
			if (_hUnityWnd == IntPtr.Zero) _hUnityWnd = hWnd;
			return true;
		}
	}
#endif
}