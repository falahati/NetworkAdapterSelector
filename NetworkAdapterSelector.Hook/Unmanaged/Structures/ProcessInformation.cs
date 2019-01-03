using System;
using System.Runtime.InteropServices;

namespace NetworkAdapterSelector.Hook.UnManaged.Structures
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
