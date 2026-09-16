using System.Diagnostics;
using Phantom.Win32;

namespace Phantom.Commands;

static class Info
{
    public static void Run(string target)
    {
        var p = ProcessResolver.Resolve(target);
        if (p == null) return;

        Output.Header($"INFO  {p.ProcessName} ({p.Id})");

        Output.Field("pid",         p.Id.ToString());
        Output.Field("name",        p.ProcessName);
        try { Output.Field("path", p.MainModule?.FileName ?? "?"); } catch { Output.Field("path", "(access denied)"); }
        Output.Field("threads",     p.Threads.Count.ToString());
        Output.Field("handles",     p.HandleCount.ToString());
        try { Output.Field("started", p.StartTime.ToString("yyyy-MM-dd HH:mm:ss")); } catch { }
        Output.Field("working set", $"{p.WorkingSet64 / 1024 / 1024} MB");
        Output.Field("peak ws",     $"{p.PeakWorkingSet64 / 1024 / 1024} MB");
        Output.Field("virtual mem", $"{p.VirtualMemorySize64 / 1024 / 1024} MB");
        Output.Field("private mem", $"{p.PrivateMemorySize64 / 1024 / 1024} MB");
        try { Output.Field("window title", string.IsNullOrEmpty(p.MainWindowTitle) ? "(none)" : p.MainWindowTitle); } catch { }

        Console.WriteLine();
        Output.Dim("  threads:");

        var threads = p.Threads.Cast<ProcessThread>().ToList();
        const int showMax = 20;
        foreach (var t in threads.Take(showMax))
        {
            string state = t.ThreadState.ToString();
            string wait  = t.ThreadState == System.Diagnostics.ThreadState.Wait ? $" ({t.WaitReason})" : "";
            Console.WriteLine($"    {t.Id,-8} {state}{wait}");
        }
        if (threads.Count > showMax)
            Output.Dim($"    ... and {threads.Count - showMax} more");
    }
}
