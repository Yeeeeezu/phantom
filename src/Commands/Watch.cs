using System.Diagnostics;

namespace Phantom.Commands;

static class Watch
{
    public static void Run(string name)
    {
        Output.Header($"WATCH  {name}");
        Output.Dim("  watching for process appearance/disappearance. ctrl+c to stop.");
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
}
