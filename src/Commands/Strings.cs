using System.Text;
using Phantom.Win32;

namespace Phantom.Commands;

static class Strings
{
    public static void Run(string target, int minLen)
    {
        var p = ProcessResolver.Resolve(target);
        if (p == null) return;

        Output.Header($"STRINGS  {p.ProcessName} ({p.Id})  min={minLen}");
        Output.Dim("  scanning readable regions... (ctrl+c to stop)");
        Console.WriteLine();

        var regions = Memory.GetReadableRegions(p.Handle);
        int found = 0, scanned = 0;

        foreach (var (addr, size) in regions)
        {
            scanned++;
            byte[] buf = new byte[size];
            if (!Memory.Read(p.Handle, addr, buf)) continue;

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
                        if (found >= 2000) { Output.Dim("\n  (stopped at 2000 results)"); return; }
                    }
                    start = -1;
                }
            }
        }

        Console.WriteLine();
        Output.Dim($"  {found} strings found across {scanned} regions");
    }
}
