using System;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.UnManaged
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        public IntPtr ProcessHandle;
        public IntPtr ThreadHandle;
        public int ProcessId;
        public int ThreadId;
    }
}
