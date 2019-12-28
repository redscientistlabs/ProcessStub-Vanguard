using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using RTCV.CorruptCore;

namespace RTCV.ProcessCorrupt
{
    [Serializable()]
    public class ProcessMemoryDomain : IMemoryDomain
    {
        public string Name { get; }
        public bool BigEndian => false;
        public long Size { get; }
        private IntPtr baseAddr { get; }
        public int WordSize => 4;
        private ProcessExtensions.MemoryProtection mp = ProcessExtensions.MemoryProtection.Empty;
        private Process p;
    
        private int errCount = 0;
        private int maxErrs = 5;
    
        public override string ToString()
        {
            return Name;
        }
    
        public ProcessMemoryDomain(Process _p, IntPtr baseAddress, long size)
        {
            try
            {
                if (_p == null || _p.HasExited)
                {
                    throw new Exception("Process doesn't exist or has exited");
                }
    
                p = _p;
                Size = size;
                baseAddr = baseAddress;
    
                var path = ProcessExtensions.GetMappedFileNameW(_p.Handle, baseAddress);
                if (!String.IsNullOrWhiteSpace(path))
                    path = Path.GetFileName(path);
                else
                    path = "UNKNOWN";
                Name = $"{baseAddr.ToString("X8")} : {size.ToString("X8")} {path}";
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to create ProcessInterface!\nMessage: {e.Message}");
            }
        }
    
    
        public void PokeByte(long address, byte data)
        {
            if (p == null || errCount > maxErrs)
                return;
            try
            {
                ProcessExtensions.WriteVirtualMemory(p, (IntPtr)((long)baseAddr + address), new[] { data });
            }
            catch (Exception e)
            {
                Console.WriteLine($"ProcessInterface PokeByte failed!\n{e.Message}\n{e.StackTrace}");
                errCount++;
            }
        }
    
        public byte PeekByte(long address)
        {
            if (p == null || errCount > maxErrs)
                return 0;
            try
            {
                return ProcessExtensions.ReadVirtualMemory(p, (IntPtr)((long)baseAddr + address), 1)[0];
            }
            catch (Exception e)
            {
                Console.WriteLine($"ProcessInterface PeekByte failed!\n{e.Message}\n{e.StackTrace}");
                errCount++;
            }
            return 0;
        }
        public byte[] PeekBytes(long address, int length)
        {
            if (p == null || errCount > maxErrs)
                return null;
            try
            {
                return ProcessExtensions.ReadVirtualMemory(p, (IntPtr)((long)baseAddr + address), length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ProcessInterface PeekBytes failed!\n{e.Message}");
                errCount++;
            }
            return null;
        }
        public byte[] GetDump()
        {
            throw new NotImplementedException();
        }
    
        public bool SetMemoryProtection(ProcessExtensions.MemoryProtection memoryProtection)
        {
            var result = ProcessExtensions.VirtualProtectEx(p, baseAddr, (IntPtr)Size, memoryProtection, out var _mp);
            if (result)
                mp = _mp;
            return result;
        }
        public bool ResetMemoryProtection()
        {
            if(mp != ProcessExtensions.MemoryProtection.Empty)
                return ProcessExtensions.VirtualProtectEx(p, baseAddr, (IntPtr)Size, mp, out _);
            return false;
        }

        public void FlushInstructionCache()
        {
            ProcessExtensions.FlushInstructionCache(p.Handle, baseAddr, (UIntPtr) Size);
        }
    }
}
