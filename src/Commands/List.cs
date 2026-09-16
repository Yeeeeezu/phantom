using System.Diagnostics;

namespace Phantom.Commands;

static class List
{
    public static void Run(string? filter)
    {
        var procs = Process.GetProcesses()
            .Where(p => filter == null || p.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.ProcessName);

        Output.Header("PROCESSES");
        Console.WriteLine($"  {"PID",-8} {"NAME",-30} {"THREADS",-9} {"MEM (MB)",-10}");
        Output.Dim("  " + new string('─', 60));

        foreach (var p in procs)
        {
            long mem = -1;
            try { mem = p.WorkingSet64 / (1024 * 1024); } catch { }
            string memStr = mem == -1 ? "?" : mem.ToString();
            Console.WriteLine($"  {p.Id,-8} {p.ProcessName,-30} {p.Threads.Count,-9} {memStr,-10}");
        }
    }
}
