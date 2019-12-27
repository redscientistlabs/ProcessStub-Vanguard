using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using static RTCV.ProcessCorrupt.NativeTypes;

namespace RTCV.ProcessCorrupt
{

    //Lifted from Bizhawk https://github.com/TASVideos/BizHawk
    public static class ProcessExtensions
    {
        public enum MemoryType
        {
            MEM_COMMIT = 0x00001000,
            MEM_RESERVE = 0x00002000,
            MEM_RESET = 0x00008000,
            MEM_RESET_UNDO = 0x1000000,
            MEM_LARGE_PAGES = 0x20000000,
            MEM_PHYSICAL = 0x00400000,
            MEM_TOP_DOWN = 0x00100000,
        }
        public enum MEMORY_REGION_TYPE
        {
            UnknownRegion,
            CustomRegion,
            UnusableRegion,
            MappedFileRegion,
            UserSharedDataRegion,
            PebRegion,
            Peb32Region,
            TebRegion,
            Teb32Region, // Not used
            StackRegion,
            Stack32Region,
            HeapRegion,
            Heap32Region,
            HeapSegmentRegion,
            HeapSegment32Region,
            CfgBitmapRegion,
            CfgBitmap32Region,
            ApiSetMapRegion,
            HypervisorSharedDataRegion,
        }

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
        }
        /// <summary>
        /// Defines the protection to be applied to a region of virtual memory
        /// </summary>
        [Flags]
        public enum MemoryProtection
        {
            /// <summary>Disables all access to the region of virtual memory</summary>
            ZeroAccess = 0,
            /// <summary>Disables all access to the region of virtual memory</summary>
            NoAccess = 1,
            /// <summary>Marks the region of virtual memory as readable</summary>
            ReadOnly = 2,
            /// <summary>
            /// Marks the region of virtual memory as readable and/or writable
            /// </summary>
            ReadWrite = 4,
            /// <summary>
            /// Marks the region of virtual memory as readable and/or copy on write
            /// </summary>
            WriteCopy = 8,
            /// <summary>Marks the region of virtual memory as executable</summary>
            Execute = 16, // 0x00000010
            /// <summary>
            /// Marks the region of virtual memory as readable and/or executable
            /// </summary>
            ExecuteRead = 32, // 0x00000020
            /// <summary>
            /// Marks the region of virtual memory as readable, writable and/or executable
            /// </summary>
            ExecuteReadWrite = 64, // 0x00000040
            /// <summary>
            /// Marks the region of virtual memory as readable, copy on write and/or executable
            /// </summary>
            ExecuteWriteCopy = 128, // 0x00000080
            /// <summary>Marks the region of virtual memory as guarded</summary>
            Guard = 256, // 0x00000100
            /// <summary>
            /// Marks the region of virtual memory as readable, writable and guarded
            /// </summary>
            ReadWriteGuard = Guard | ReadWrite, // 0x00000104
            /// <summary>Marks the region of virtual memory as non-cacheable</summary>
            NoCache = 512, // 0x00000200
        }
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualQueryEx(SafeProcessHandle processHandle, IntPtr baseAddress, out MemoryBasicInformation memoryInformation, int length);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(SafeProcessHandle processHandle, IntPtr baseAddress, IntPtr protectionSize, MemoryProtection protectionType, out MemoryProtection oldProtectionType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(SafeProcessHandle processHandle, IntPtr baseAddress, byte[] lpBuffer, IntPtr bytesToRead, IntPtr numberOfBytesReadBuffer);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(SafeProcessHandle processHandle, IntPtr baseAddress, IntPtr bytesReadBuffer, IntPtr bytesToWrite, IntPtr numberOfBytesReadBuffer);
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetMappedFileNameW(IntPtr ProcessHandle, IntPtr Address, StringBuilder Buffer, int Size);
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
        private static extern NtStatus QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);[DllImport("NTDLL.DLL", SetLastError = true)]
        private static extern NtStatus NtQueryInformationProcess(IntPtr hProcess, PROCESS_INFORMATION_CLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, out int pSize);
        /// <summary>Retrieves the specified system information.</summary>
        /// <param name="InfoClass">indicate the kind of system information to be retrieved</param>
        /// <param name="Info">a buffer that receives the requested information</param>
        /// <param name="Size">The allocation size of the buffer pointed to by Info</param>
        /// <param name="Length">If null, ignored.  Otherwise tells you the size of the information returned by the kernel.</param>
        /// <returns>Status Information</returns>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724509%28v=vs.85%29.aspx
        [DllImport("ntdll.dll")]
        public static extern NtStatus NtQuerySystemInformation([In]SYSTEM_INFORMATION_CLASS SystemInformationClass, [Out]IntPtr SystemInformation, [In]uint SystemInformationLength, [Out]out IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        private static extern NtStatus NtQuerySystemInformationEx([In]SYSTEM_INFORMATION_CLASS SystemInformationClass, [In]IntPtr InputBuffer, [In]uint InputBufferLength, [Out]IntPtr SystemInformation, [In]uint SystemInformationLength, [Out]out IntPtr ReturnLength);

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
            SafeProcessHandle handle = new SafeProcessHandle(p.Handle, false);
            var bytesBuffer = new byte[bytesToRead];

            if (!ReadProcessMemory(handle, baseAddress, bytesBuffer, new IntPtr(bytesToRead), IntPtr.Zero))
            {
                throw new Exception($"Failed to read from a region of virtual memory in the remote process. Error code: {Marshal.GetLastWin32Error()}");
            }

            //var bytesRead = new byte[bytesToRead];
            //Marshal.Copy(bytesBuffer, bytesRead, 0, bytesToRead);
            //Marshal.FreeHGlobal(bytesBuffer);

            return bytesBuffer;
        }

        public static void WriteVirtualMemory(Process p, IntPtr baseAddress, byte[] bytesToWrite)
        {
            var bytesToWriteBufferHandle = GCHandle.Alloc(bytesToWrite, GCHandleType.Pinned);
            try
            {
                SafeProcessHandle handle = new SafeProcessHandle(p.Handle, false);

                if (!WriteProcessMemory(handle, baseAddress, bytesToWriteBufferHandle.AddrOfPinnedObject(), new IntPtr(bytesToWrite.Length), IntPtr.Zero))
                {
                    throw new Exception($"Failed to read write a region  of virtual memory in the remote process. Error code: {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                bytesToWriteBufferHandle.Free();
            }

        }

        public static bool VirtualProtectEx(Process p, IntPtr baseAddress, IntPtr dwSize, MemoryProtection protType, out MemoryProtection oldProtType)
        {
            SafeProcessHandle handle = new SafeProcessHandle(p.Handle, false);
            if (!VirtualProtectEx(handle, baseAddress, dwSize, protType, out oldProtType))
            {
                Console.WriteLine($"Failed to protect a region of virtual memory {baseAddress} + {dwSize} in the remote process");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Much faster than Process.MainModule.Filename
        /// </summary>
        /// <param name="process"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Returns the process with additional checking on whether or not it exists
        /// ALWAYS USE TRY CATCH AS THE PROCESS COULD DIE AT ANY POINT
        /// </summary>
        /// <returns></returns>
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
                    var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                    }

                    SuspendThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
                catch (Exception e)
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
                    var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        break;
                    }

                    ResumeThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to resume thread {thread.Id}");
                    success = false;
                }
            }
            return success;

        }


        /*
        public static  NtStatus EnumProcessEx(out IntPtr processes, SYSTEM_INFORMATION_CLASS sic)
        {
            SYSTEM_PROCESS_INFORMATION pi;
            NtStatus status;

            uint bufferSize = sizeof(SYSTEM_INFORMATION_CLASS);
            var infoPtr = Marshal.AllocHGlobal((int)sic);

            while (true)
            {
                status = (NtStatus)NtQuerySystemInformation(sic, infoPtr, bufferSize, out IntPtr returnSize);
                if (status == NtStatus.BufferTooSmall || status == NtStatus.InfoLengthMismatch)
                {
                    Marshal.FreeHGlobal(infoPtr);
                    infoPtr = Marshal.AllocHGlobal(returnSize);
                }
                else
                    break;
            }

            if (status != NtStatus.Success)
            {
                Marshal.FreeHGlobal(infoPtr);
                Console.WriteLine($"Whoops {status}");
                processes = (IntPtr)0;
                return status;
            }
            processes = infoPtr;

            return status;
        }
        public static SYSTEM_PROCESS_INFORMATION FindProcessInformation(IntPtr Processes, int ProcessId)
        {
            SYSTEM_PROCESS_INFORMATION process = (SYSTEM_PROCESS_INFORMATION)Marshal.PtrToStructure(Processes, typeof(SYSTEM_PROCESS_INFORMATION));
            return process;
        }

        public static bool EnumProcessss(Process p, out NativeTypes.SYSTEM_EXTENDED_PROCESS_INFORMATION epi)
        {
            var handle = p.Handle;
            p.Suspend();

            SYSTEM_INFORMATION_CLASS sic = new SYSTEM_INFORMATION_CLASS();

            if (EnumProcessEx(out IntPtr process, sic) == NtStatus.Success)
            {
                var spi = FindProcessInformation(process, p.Id);

                PROCESS_BASIC_INFORMATION basicInfo = new PROCESS_BASIC_INFORMATION();
                uint numberOfHeaps;
                IntPtr processHeapsPtr;
                IntPtr processHeaps;
                IntPtr apiSetMap;
                uint i;
                IntPtr peb32;
                uint processHeapsPtr32;
                IntPtr processHeaps32;
                uint apiSetMap32;

                if (NtQueryInformationProcess((IntPtr) p.Id, PROCESS_INFORMATION_CLASS.ProcessBasicInformation, ref basicInfo, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)), out _) == NtStatus.Success)
                {

                    if (ReadVirtualMemory(p, (basicInfo.PebBaseAddress + Marshal.OffsetOf(TYPE, FIELD_NAME)))
                }


                foreach (ProcessThread thread in p.Threads)
                {
                    //result = (uint)NtQuerySystemInformationEx(sic, infoPtr, result, out IntPtr returnSize);

                }

                if (NtQueryInformationProcess(handle,))
            }
        }*/
    }
}
