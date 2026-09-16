using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Phantom;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0) { PrintHelp(); return; }

        switch (args[0].ToLowerInvariant())
        {
            case "list" or "ls":
                ListProcesses(args.Length > 1 ? args[1] : null);
                break;
            case "info":
                if (args.Length < 2) { Error("usage: phantom info <pid|name>"); return; }
                ShowInfo(args[1]);
                break;
            case "modules" or "mods":
                if (args.Length < 2) { Error("usage: phantom modules <pid|name>"); return; }
                ShowModules(args[1]);
                break;
            case "strings":
                if (args.Length < 2) { Error("usage: phantom strings <pid|name> [minlen]"); return; }
                int minLen = args.Length > 2 && int.TryParse(args[2], out int ml) ? ml : 6;
                ScanStrings(args[1], minLen);
                break;
            case "watch":
                if (args.Length < 2) { Error("usage: phantom watch <name>"); return; }
                WatchProcess(args[1]);
                break;
            default:
                Error($"unknown command '{args[0]}'");
                PrintHelp();
                break;
        }
    }

    static void ListProcesses(string? filter)
    {
        var procs = Process.GetProcesses()
            .Where(p => filter == null || p.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.ProcessName);

        Header("PROCESSES");
        Console.WriteLine($"  {"PID",-8} {"NAME",-30} {"THREADS",-9} {"MEM (MB)",-10}");
        Dim("  " + new string('─', 60));

        foreach (var p in procs)
        {
            long mem = -1;
            try { mem = p.WorkingSet64 / (1024 * 1024); } catch { }
            string memStr = mem == -1 ? "?" : mem.ToString();
            Console.WriteLine($"  {p.Id,-8} {p.ProcessName,-30} {p.Threads.Count,-9} {memStr,-10}");
        }
    }

    static void ShowInfo(string target)
    {
        var p = Resolve(target);
        if (p == null) return;

        Header($"INFO  {p.ProcessName} ({p.Id})");

        Field("pid", p.Id.ToString());
        Field("name", p.ProcessName);
        try { Field("path", p.MainModule?.FileName ?? "?"); } catch { Field("path", "(access denied)"); }
        Field("threads", p.Threads.Count.ToString());
        Field("handles", p.HandleCount.ToString());
        try { Field("started", p.StartTime.ToString("yyyy-MM-dd HH:mm:ss")); } catch { }
        Field("working set", $"{p.WorkingSet64 / 1024 / 1024} MB");
        Field("peak ws", $"{p.PeakWorkingSet64 / 1024 / 1024} MB");
        Field("virtual mem", $"{p.VirtualMemorySize64 / 1024 / 1024} MB");
        Field("private mem", $"{p.PrivateMemorySize64 / 1024 / 1024} MB");

        try
        {
            Field("window title", string.IsNullOrEmpty(p.MainWindowTitle) ? "(none)" : p.MainWindowTitle);
        }
        catch { }

        Console.WriteLine();
        Dim("  threads:");
        foreach (ProcessThread t in p.Threads)
        {
            string state = t.ThreadState.ToString();
            string wait = t.ThreadState == System.Diagnostics.ThreadState.Wait ? $" ({t.WaitReason})" : "";
            Console.WriteLine($"    {t.Id,-8} {state}{wait}");
        }
    }

    static void ShowModules(string target)
    {
        var p = Resolve(target);
        if (p == null) return;

        ProcessModuleCollection? modules;
        try { modules = p.Modules; }
        catch { Error("access denied — try running as administrator"); return; }

        Header($"MODULES  {p.ProcessName} ({p.Id})");
        Console.WriteLine($"  {"BASE",-20} {"SIZE",-10} {"NAME",-35} VERSION");
        Dim("  " + new string('─', 85));

        foreach (ProcessModule m in modules)
        {
            string ver = m.FileVersionInfo.FileVersion ?? "";
            Console.WriteLine($"  0x{m.BaseAddress:X16}  {m.ModuleMemorySize / 1024,6}kb  {m.ModuleName,-35} {ver}");
        }
    }

    static void ScanStrings(string target, int minLen)
    {
        var p = Resolve(target);
        if (p == null) return;

        Header($"STRINGS  {p.ProcessName} ({p.Id})  min={minLen}");
        Dim("  scanning readable regions... (ctrl+c to stop)");
        Console.WriteLine();

        var regions = GetReadableRegions(p.Id);
        int found = 0;
        int scanned = 0;

        foreach (var (addr, size) in regions)
        {
            scanned++;
            byte[] buf = new byte[size];
            if (!ReadMem(p.Handle, addr, buf)) continue;

            // scan for printable ascii runs
            int start = -1;
            for (int i = 0; i <= buf.Length; i++)
            {
                bool printable = i < buf.Length && buf[i] >= 0x20 && buf[i] < 0x7F;
                if (printable)
                {
                    if (start == -1) start = i;
                }
                else if (start != -1)
                {
                    int len = i - start;
                    if (len >= minLen)
                    {
                        string s = Encoding.ASCII.GetString(buf, start, len);
                        Console.WriteLine($"  0x{addr + (ulong)start:X16}  {s}");
                        found++;
                        if (found >= 2000) { Dim($"\n  (stopped at 2000 results)"); return; }
                    }
                    start = -1;
                }
            }
        }

        Console.WriteLine();
        Dim($"  {found} strings found across {scanned} regions");
    }

    static void WatchProcess(string name)
    {
        Header($"WATCH  {name}");
        Dim("  watching for process appearance/disappearance. ctrl+c to stop.");
        Console.WriteLine();

        bool wasRunning = false;
        while (true)
        {
            bool running = Process.GetProcessesByName(name).Length > 0;
            if (running != wasRunning)
            {
                string ts = DateTime.Now.ToString("HH:mm:ss");
                if (running)
                    Console.WriteLine($"  [{ts}]  \x1b[32m+ {name} started\x1b[0m  (pid {Process.GetProcessesByName(name)[0].Id})");
                else
                    Console.WriteLine($"  [{ts}]  \x1b[31m- {name} exited\x1b[0m");
                wasRunning = running;
            }
            Thread.Sleep(250);
        }
    }

    // ---- win32 memory reading ----

    [DllImport("kernel32")] static extern bool ReadProcessMemory(nint hProcess, ulong lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);
    [DllImport("kernel32")] static extern int VirtualQueryEx(nint hProcess, ulong lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORY_BASIC_INFORMATION
    {
        public ulong BaseAddress, AllocationBase;
        public uint AllocationProtect, PartitionId;
        public ulong RegionSize;
        public uint State, Protect, Type;
    }

    static List<(ulong addr, int size)> GetReadableRegions(int pid)
    {
        var list = new List<(ulong, int)>();
        nint h;
        try { h = Process.GetProcessById(pid).Handle; } catch { return list; }

        ulong addr = 0;
        while (VirtualQueryEx(h, addr, out var mbi, Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()) != 0)
        {
            // MEM_COMMIT = 0x1000, readable protect flags
            bool committed = mbi.State == 0x1000;
            bool readable = (mbi.Protect & 0xCC) != 0; // PAGE_READONLY + PAGE_READWRITE + PAGE_EXECUTE_READ + PAGE_EXECUTE_READWRITE
            bool notGuard = (mbi.Protect & 0x100) == 0;

            if (committed && readable && notGuard && mbi.RegionSize > 0 && mbi.RegionSize <= 64 * 1024 * 1024)
                list.Add((mbi.BaseAddress, (int)mbi.RegionSize));

            if (addr + mbi.RegionSize <= addr) break;
            addr += mbi.RegionSize;
        }
        return list;
    }

    static bool ReadMem(nint hProcess, ulong addr, byte[] buf)
    {
        try { return ReadProcessMemory(hProcess, addr, buf, buf.Length, out _); }
        catch { return false; }
    }

    // ---- resolve pid or name ----

    static Process? Resolve(string target)
    {
        if (int.TryParse(target, out int pid))
        {
            try { return Process.GetProcessById(pid); }
            catch { Error($"no process with pid {pid}"); return null; }
        }

        var matches = Process.GetProcessesByName(target);
        if (matches.Length == 0)
        {
            // try partial
            matches = Process.GetProcesses()
                .Where(p => p.ProcessName.Contains(target, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (matches.Length == 0) { Error($"no process matching '{target}'"); return null; }
        if (matches.Length > 1)
        {
            Dim($"  multiple matches — picking first (pid {matches[0].Id})");
        }
        return matches[0];
    }

    // ---- output helpers ----

    static void Header(string text) => Console.WriteLine($"\n\x1b[1;37m  {text}\x1b[0m\n");
    static void Field(string key, string val) => Console.WriteLine($"  \x1b[2m{key,-16}\x1b[0m {val}");
    static void Dim(string text) => Console.WriteLine($"\x1b[2m{text}\x1b[0m");
    static void Error(string msg) => Console.Error.WriteLine($"\x1b[31m  error: {msg}\x1b[0m");

    static void PrintHelp()
    {
        Console.WriteLine("""

  phantom — windows process inspector

  usage:
    phantom list [filter]          list running processes
    phantom info <pid|name>        detailed process info
    phantom modules <pid|name>     loaded modules + base addresses
    phantom strings <pid|name>     scan readable memory for strings
    phantom watch <name>           watch for process start/exit

  examples:
    phantom list chrome
    phantom info 1234
    phantom modules notepad
    phantom strings roblox 8
    phantom watch RobloxPlayerBeta

""");
    }
}
