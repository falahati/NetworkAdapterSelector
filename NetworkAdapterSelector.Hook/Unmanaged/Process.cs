using System;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.Unmanaged
{
    internal static class Process
    {
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CreateProcessW(
            IntPtr lpApplicationName,
            IntPtr lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory,
            IntPtr lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CreateProcessA(
            IntPtr lpApplicationName,
            IntPtr lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory,
            IntPtr lpStartupInfo,
            out ProcessInformation lpProcessInformation);
    }
}