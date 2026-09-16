using Phantom.Win32;

namespace Phantom.Commands;

static class Modules
{
    public static void Run(string target)
    {
        var p = ProcessResolver.Resolve(target);
        if (p == null) return;

        System.Diagnostics.ProcessModuleCollection? modules;
        try { modules = p.Modules; }
        catch { Output.Error("access denied — try running as administrator"); return; }

        Output.Header($"MODULES  {p.ProcessName} ({p.Id})");
        Console.WriteLine($"  {"BASE",-20} {"SIZE",-10} {"NAME",-35} VERSION");
        Output.Dim("  " + new string('─', 85));

        foreach (System.Diagnostics.ProcessModule m in modules)
        {
            string ver = m.FileVersionInfo.FileVersion ?? "";
            Console.WriteLine($"  0x{m.BaseAddress:X16}  {m.ModuleMemorySize / 1024,6}kb  {m.ModuleName,-35} {ver}");
        }
    }
}
