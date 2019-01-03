using System;
using System.Runtime.InteropServices;
using NetworkAdapterSelector.Hook.UnManaged.Structures;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    internal static class NativeProcess
    {
        [DllImport("kernel32", SetLastError = true)]
        // ReSharper disable once TooManyArguments
        public static extern bool CreateProcessW(
            IntPtr applicationName,
            IntPtr commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            IntPtr currentDirectory,
            IntPtr startupInfo,
            out ProcessInformation processInformation);

        [DllImport("kernel32", SetLastError = true)]
        // ReSharper disable once TooManyArguments
        public static extern bool CreateProcessA(
            IntPtr applicationName,
            IntPtr commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            IntPtr currentDirectory,
            IntPtr startupInfo,
            out ProcessInformation processInformation);
    }
}