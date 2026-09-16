using System.Diagnostics;

namespace Phantom.Win32;

static class ProcessResolver
{
    public static Process? Resolve(string target)
    {
        if (int.TryParse(target, out int pid))
        {
            try { return Process.GetProcessById(pid); }
            catch { Output.Error($"no process with pid {pid}"); return null; }
        }

        var matches = Process.GetProcessesByName(target);
        if (matches.Length == 0)
        {
            matches = Process.GetProcesses()
                .Where(p => p.ProcessName.Contains(target, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (matches.Length == 0) { Output.Error($"no process matching '{target}'"); return null; }
        if (matches.Length > 1)
            Output.Dim($"  multiple matches — picking first (pid {matches[0].Id})");

        return matches[0];
    }
}
