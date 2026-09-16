using Phantom;
using Phantom.Commands;

if (args.Length == 0) { Output.Help(); return; }

switch (args[0].ToLowerInvariant())
{
    case "list" or "ls":
        List.Run(args.Length > 1 ? args[1] : null);
        break;
    case "info":
        if (args.Length < 2) { Output.Error("usage: phantom info <pid|name>"); return; }
        Info.Run(args[1]);
        break;
    case "modules" or "mods":
        if (args.Length < 2) { Output.Error("usage: phantom modules <pid|name>"); return; }
        Modules.Run(args[1]);
        break;
    case "strings":
        if (args.Length < 2) { Output.Error("usage: phantom strings <pid|name> [minlen]"); return; }
        int minLen = args.Length > 2 && int.TryParse(args[2], out int ml) ? ml : 6;
        Strings.Run(args[1], minLen);
        break;
    case "watch":
        if (args.Length < 2) { Output.Error("usage: phantom watch <name>"); return; }
        Watch.Run(args[1]);
        break;
    default:
        Output.Error($"unknown command '{args[0]}'");
        Output.Help();
        break;
}
