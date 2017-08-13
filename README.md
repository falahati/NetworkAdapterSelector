# Network Adapter Selector
A solution containing an Injector to hook WinSock methods and bind the new connections to the specific network adapter along with a Shell Extension to simplifies the process of injecting the code into other programs.
![Screenshot](/screenshot.jpg?raw=true "Screenshot")

## Download
Download the latest version of the program from the [releases](https://github.com/falahati/NetworkAdapterSelector/releases/latest) page.

## Command Line Parameters
If you are not interested in using the provided shell extension, you can always use the following command line parameters directly to manipulate running applications or start a new process.

- `-d `--debug`: Debug mode creates a log file in temp directory logging all activities of the injected code. [true, false]
- `-n` `--network`: Identification string of the network adapter to bind.
- `-a` `--attach`: Process identification number of the process to attach. **OR**
- `-e` `--execute`: Address of the executable file to start.
- `-c` `--args`: Arguments to be sent to the executable file.
- `-t` `--delay`: Delay in milliseconds before trying to inject the code.

#### Network Identification String
Network identification string is the network GUID in `{00000000-0000-0000-0000-000000000000}` format, all uppercase. To get those you can run the following commands in the CMD to start the "Wired AutoConfig" and "WLAN AutoConfig" services and list network adapters:
```
net start "Wired AutoConfig"
net start "WLAN AutoConfig"
netsh lan show interfaces
netsh wlan show interfaces
```

Or use the following code in a PowerShell window that uses .Net libraries to produce a similar result:
```
[System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces()
```

Also, you can use the registry and read them from the following path directly:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsNT\CurrentVersion\NetworkCards
```

#### Examples
Attaching to an already running process: (PID `1234`)
```
NetworkAdapterSelector.Hook.exe --network "{ABCDEFGH-0000-0000-0000-000000000000}" --attach 1234
```

Starting a new instance of `notepad.exe` and attaching to it after one second:
```
NetworkAdapterSelector.Hook.exe --network "{ABCDEFGH-0000-0000-0000-000000000000}" --execute "C:\Windows\System32\notepad.exe" --delay 1000
```

## Technology
Both parts of the solution are in pure C# using EasyHook library and SharpShell framework. It was a little experiment to see how stable is EasyHook and how SharpShell can perform in terms of performance.
