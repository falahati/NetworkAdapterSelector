using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    internal static class NativeWindow
    {
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr windowHandle, StringBuilder text, int length);

        [DllImport("user32", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr windowHandle);

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr windowHandle);

        [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr windowHandle, string text);
    }
}