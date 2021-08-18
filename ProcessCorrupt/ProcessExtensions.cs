namespace RTCV.ProcessCorrupt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Win32.SafeHandles;

    //Lifted from Bizhawk https://github.com/TASVideos/BizHawk
    public static class ProcessExtensions
    {
        public enum MemType
        {
            MEMORY_COMMIT = 0x00001000,
            MEMORY_RESERVE = 0x00002000,
            MEMORY_RESET = 0x00008000,
            MEMORY_PHYSICAL = 0x00400000,
            MEMORY_TOP_DOWN = 0x00100000,
            MEMORY_RESET_UNDO = 0x1000000,
            MEMORY_LARGE_PAGES = 0x20000000,

        }


        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicInformation
        {
            public readonly IntPtr BaseAddress;

            public readonly IntPtr AllocationBase;
            public readonly uint AllocationProtect;
            public readonly uint __alignment1;

            public readonly IntPtr RegionSize;

            public readonly uint State;

            public readonly MemProtection Protect;

            public readonly uint Type;
            public readonly uint __alignment2;
        }


        [Flags]
        public enum MemProtection
        {
            Memory_Empty = -1,
            Memory_ZeroAccess = 0,
            Memory_NoAccess = 1,
            Memory_ReadOnly = 2,
            Memory_ReadWrite = 4,
            Memory_WriteCopy = 8,
            Memory_Execute = 16, // 0x00000010
            Memory_ExecuteRead = 32, // 0x00000020
            Memory_ExecuteReadWrite = 64, // 0x00000040
            Memory_ExecuteWriteCopy = 128, // 0x00000080
            Memory_Guard = 256, // 0x00000100
            Memory_ReadWriteGuard = Memory_Guard | Memory_ReadWrite, // 0x00000104
            Memory_NoCache = 512, // 0x00000200
        }
        [Flags]
        public enum MemoryThreadAccess : int
        {
            Thread_TERMINATE = (0x0001),
            Thread_SUSPEND_RESUME = (0x0002),
            Thread_GET_CONTEXT = (0x0008),
            Thread_SET_CONTEXT = (0x0010),
            Thread_SET_INFORMATION = (0x0020),
            Thread_QUERY_INFORMATION = (0x0040),
            Thread_SET_THREAD_TOKEN = (0x0080),
            Thread_IMPERSONATE = (0x0100),
            Thread_DIRECT_IMPERSONATION = (0x0200)
        }
        [Flags]
        public enum MemoryAllocationType
        {
            Alloc_Commit = 0x1000,
            Alloc_Reserve = 0x2000,
            Alloc_Decommit = 0x4000,
            Alloc_Release = 0x8000,
            Alloc_Reset = 0x80000,
            Alloc_Physical = 0x400000,
            Alloc_TopDown = 0x100000,
            Alloc_WriteWatch = 0x200000,
            Alloc_LargePages = 0x20000000
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;  // minimum address
            public IntPtr maximumApplicationAddress;  // maximum address
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }




        #region DllImports

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(MemoryThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualQueryEx(SafeProcessHandle processHandle, IntPtr baseAddress, out MemoryBasicInformation memoryInformation, int length);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr processHandle, IntPtr baseAddress, IntPtr protectionSize, MemProtection protectionType, out MemProtection oldProtectionType);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, MemoryAllocationType flAllocationType, MemProtection flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress, byte[] lpBuffer, IntPtr bytesToRead, IntPtr numberOfBytesReadBuffer);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr processHandle, IntPtr baseAddress, IntPtr bytesReadBuffer, IntPtr bytesToWrite, IntPtr numberOfBytesReadBuffer);
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetMappedFileNameW(IntPtr ProcessHandle, IntPtr Address, StringBuilder Buffer, int Size);
        [DllImport("kernel32.dll")]
        public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(
            [In] Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid hProcess,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
        );
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr processHandle,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("Kernel32.dll")]
        private static extern uint QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);
        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);


        #endregion














        public static bool VirtualQueryEx(Process p, IntPtr baseAddress, out MemoryBasicInformation memoryBasicInformation)
        {
            SafeProcessHandle handle = new SafeProcessHandle(p.Handle, false);
            if (!VirtualQueryEx(handle, baseAddress, out memoryBasicInformation, Marshal.SizeOf<MemoryBasicInformation>()))
            {
                Console.WriteLine("Failed to query a region of virtual memory in the remote process");
                return false;
            }

            return true;
        }

        public static byte[] ReadVirtualMemory(Process p, IntPtr baseAddress, int bytesToRead)
        {
            var bytesBuffer = new byte[bytesToRead];
            if (!ReadProcessMemory(p.Handle, baseAddress, bytesBuffer, new IntPtr(bytesToRead), IntPtr.Zero))
            {
                //throw new Exception($"Failed to read from a region of virtual memory in the remote process. Error code: {Marshal.GetLastWin32Error()}");
            }


            return bytesBuffer;
        }

        public static void WriteVirtualMemory(Process p, IntPtr baseAddress, byte[] bytesToWrite)
        {
            var bytesToWriteBufferHandle = GCHandle.Alloc(bytesToWrite, GCHandleType.Pinned);
            try
            {
                if (!WriteProcessMemory(p.Handle, baseAddress, bytesToWriteBufferHandle.AddrOfPinnedObject(), new IntPtr(bytesToWrite.Length), IntPtr.Zero))
                {
                    //throw new Exception($"Failed to read write a region  of virtual memory in the remote process. Error code: {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                bytesToWriteBufferHandle.Free();
            }
        }

        public static bool VirtualProtectEx(Process p, IntPtr baseAddress, IntPtr dwSize, MemProtection protType, out MemProtection oldProtType)
        {
            if (!VirtualProtectEx(p.Handle, baseAddress, dwSize, protType, out oldProtType))
            {
                //  Console.WriteLine($"Failed to protect a region of virtual memory {baseAddress.ToString("X")} + {dwSize.ToString("X")} in the remote process");
                return false;
            }

            return true;
        }


        private static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            IntPtr h = OpenProcess(0x001F0FFF, false, process.Id);
            try
            {
                if (h == (IntPtr)0)
                    return null;
                return QueryFullProcessImageName(h, 0, fileNameBuilder, ref bufferLength) != 0 ? fileNameBuilder.ToString() : null;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        public static Icon GetIcon(this Process process)
        {
            try
            {
                string mainModuleFileName = process.GetMainModuleFileName();
                return Icon.ExtractAssociatedIcon(mainModuleFileName);
            }
            catch
            {
                // Probably no access
                return null;
            }
        }

        public static string GetMappedFileNameW(IntPtr hProcess, IntPtr hModule)
        {
            StringBuilder fileName = new StringBuilder(255);
            GetMappedFileNameW(hProcess, hModule, fileName, fileName.Capacity);
            return fileName.ToString();
        }


        public static Process GetProcessSafe(Process process)
        {
            try
            {
                if (process?.HasExited ?? true)
                    return null;
                return process;
            }
            catch (Exception e)
            {
                Console.WriteLine("GetProcessSafe FAILED!" + e.Message);
                return null;
            }
        }
        public static bool Suspend(this Process process)
        {
            bool success = true;
            foreach (ProcessThread thread in process.Threads)
            {
                try
                {
                    var pOpenThread = OpenThread(MemoryThreadAccess.Thread_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        continue;
                    }

                    SuspendThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to suspend thread {thread.Id}");
                    success = false;
                }
            }
            return success;
        }
        public static bool Resume(this Process process)
        {
            bool success = true;
            foreach (ProcessThread thread in process.Threads)
            {
                try
                {
                    var pOpenThread = OpenThread(MemoryThreadAccess.Thread_SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        continue;
                    }

                    ResumeThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to resume thread {thread.Id}");
                    success = false;
                }
            }
            return success;
        }


    }
}
