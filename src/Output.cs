namespace Phantom;

static class Output
{
    public static void Header(string text) => Console.WriteLine($"\n\x1b[1;37m  {text}\x1b[0m\n");
    public static void Field(string key, string val) => Console.WriteLine($"  \x1b[2m{key,-16}\x1b[0m {val}");
    public static void Dim(string text) => Console.WriteLine($"\x1b[2m{text}\x1b[0m");
    public static void Error(string msg) => Console.Error.WriteLine($"\x1b[31m  error: {msg}\x1b[0m");

    public static void Help() => Console.WriteLine("""

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
