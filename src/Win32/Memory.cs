using System.Runtime.InteropServices;

namespace Phantom.Win32;

static class Memory
{
    [DllImport("kernel32")] public static extern bool ReadProcessMemory(nint hProcess, ulong lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);
    [DllImport("kernel32")] public static extern int VirtualQueryEx(nint hProcess, ulong lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public ulong BaseAddress, AllocationBase;
        public uint AllocationProtect, PartitionId;
        public ulong RegionSize;
        public uint State, Protect, Type;
    }

    public static List<(ulong addr, int size)> GetReadableRegions(nint hProcess)
    {
        var list = new List<(ulong, int)>();
        ulong addr = 0;

        while (VirtualQueryEx(hProcess, addr, out var mbi, Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()) != 0)
        {
            bool committed = mbi.State == 0x1000;
            bool readable  = (mbi.Protect & 0xCC) != 0;
            bool notGuard  = (mbi.Protect & 0x100) == 0;

            if (committed && readable && notGuard && mbi.RegionSize > 0 && mbi.RegionSize <= 64 * 1024 * 1024)
                list.Add((mbi.BaseAddress, (int)mbi.RegionSize));

            if (addr + mbi.RegionSize <= addr) break;
            addr += mbi.RegionSize;
        }

        return list;
    }

    public static bool Read(nint hProcess, ulong addr, byte[] buf)
    {
        try { return ReadProcessMemory(hProcess, addr, buf, buf.Length, out _); }
        catch { return false; }
    }
}
