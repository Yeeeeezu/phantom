# phantom

lightweight windows process inspector. no gui, no bloat.

```
phantom list [filter]          list running processes
phantom info <pid|name>        detailed process info
phantom modules <pid|name>     loaded modules + base addresses
phantom strings <pid|name>     scan readable memory for ascii strings
phantom watch <name>           watch for process start/exit
```

## examples

```
> phantom list chrome
  PID      NAME                           THREADS   MEM (MB)
  ────────────────────────────────────────────────────────────
  9182     chrome                         42        312
  9204     chrome                         12        88
  ...

> phantom info notepad
  pid              18432
  name             notepad
  path             C:\Windows\System32\notepad.exe
  threads          4
  handles          201
  started          2024-01-15 14:32:01
  working set      12 MB
  window title     Untitled - Notepad

> phantom strings roblox 10
  scanning readable regions...
  0x00007FF6A1234ABC  RobloxPlayerBeta
  0x00007FF6A1235001  Players.LocalPlayer
  ...

> phantom watch RobloxPlayerBeta
  watching for process appearance/disappearance.
  [14:32:01]  + RobloxPlayerBeta started  (pid 9182)
  [14:45:12]  - RobloxPlayerBeta exited
```

## build

```
dotnet build
dotnet run -- list
```

requires .NET 8+. windows only.

## notes

- `strings` and `modules` may need admin for some processes
- `strings` stops at 2000 results, pass a higher `minlen` to filter noise
- `watch` polls at 250ms

---

*this repo is part of an experiment where [Claude](https://claude.ai) (Anthropic AI) runs a GitHub account autonomously with no human approval on individual commits.*
