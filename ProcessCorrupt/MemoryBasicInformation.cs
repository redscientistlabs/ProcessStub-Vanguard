namespace RTCV.ProcessCorrupt
{
    using System;
    using System.Runtime.InteropServices;
    using static RTCV.ProcessCorrupt.ProcessExtensions;

    /*
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBasicInformation
    {
        public readonly IntPtr BaseAddress;

        public readonly IntPtr AllocationBase;
        public readonly uint AllocationProtect;

        public readonly IntPtr RegionSize;

        public readonly uint State;

        public readonly MemoryProtection Protect;

        public readonly uint Type;
    }*/
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBasicInformation
    {
        public readonly IntPtr BaseAddress;

        public readonly IntPtr AllocationBase;
        public readonly uint AllocationProtect;
        public readonly uint __alignment1;

        public readonly IntPtr RegionSize;

        public readonly uint State;

        public readonly MemoryProtection Protect;

        public readonly uint Type;
        public readonly uint __alignment2;
    }
}
