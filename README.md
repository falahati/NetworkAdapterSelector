# Network Adapter Selector
[![](https://img.shields.io/github/license/falahati/NetworkAdapterSelector.svg?style=flat-square)](https://github.com/falahati/NetworkAdapterSelector/blob/master/LICENSE)
[![](https://img.shields.io/github/commit-activity/y/falahati/NetworkAdapterSelector.svg?style=flat-square)](https://github.com/falahati/NetworkAdapterSelector/commits/master)
[![](https://img.shields.io/github/issues/falahati/NetworkAdapterSelector.svg?style=flat-square)](https://github.com/falahati/NetworkAdapterSelector/issues)

A solution containing an Injector to hook WinSock methods and bind the new connections to the specific network adapter along with a Shell Extension to simplifies the process of injecting the code into other programs.
![Screenshot](/screenshot.jpg?raw=true "Screenshot")

## How to get
[![](https://img.shields.io/github/downloads/falahati/NetworkAdapterSelector/total.svg?style=flat-square)](https://github.com/falahati/NetworkAdapterSelector/releases)
[![](https://img.shields.io/github/tag-date/falahati/NetworkAdapterSelector.svg?label=version&style=flat-square)](https://github.com/falahati/NetworkAdapterSelector/releases)

Download the latest version of the program from the [releases](https://github.com/falahati/NetworkAdapterSelector/releases/latest) page.

## Help me fund my own Death Star

[![](https://img.shields.io/badge/crypto-CoinPayments-8a00a3.svg?style=flat-square)](https://www.coinpayments.net/index.php?cmd=_donate&reset=1&merchant=820707aded07845511b841f9c4c335cd&item_name=Donate&currency=USD&amountf=20.00000000&allow_amount=1&want_shipping=0&allow_extra=1)
[![](https://img.shields.io/badge/shetab-ZarinPal-8a00a3.svg?style=flat-square)](https://zarinp.al/@falahati)
[![](https://img.shields.io/badge/usd-Paypal-8a00a3.svg?style=flat-square)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=ramin.graphix@gmail.com&lc=US&item_name=Donate&no_note=0&cn=&curency_code=USD&bn=PP-DonationsBF:btn_donateCC_LG.gif:NonHosted)

**--OR--**

You can always donate your time by contributing to the project or by introducing it to others.

## Command line parameters
If you are not interested in using the provided shell extension, you can always use the following command line parameters directly to manipulate running applications or start a new process.

- `-d` `--debug`: Debug mode creates a log file in temp directory logging all activities of the injected code. [true, false]
- `-n` `--network`: Identification string of the network adapter to bind.
- `-a` `--attach`: Process identification number of the process to attach. **OR**
- `-e` `--execute`: Address of the executable file to start.
- `-c` `--args`: Arguments to be sent to the executable file.
- `-t` `--delay`: Delay in milliseconds before trying to inject the code.

#### Network Identification String
Network identification string is the network GUID in `{00000000-0000-0000-0000-000000000000}` format, all uppercase. To get those you can run the following commands in the CMD to start the "Wired AutoConfig" and "WLAN AutoConfig" services and list network adapters:
```Shell
net start "Wired AutoConfig"
net start "WLAN AutoConfig"
netsh lan show interfaces
netsh wlan show interfaces
```

Or use the following code in a PowerShell window that uses .Net libraries to produce a similar result:
```Shell
[System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces()
```

Also, you can use the registry and read them from the following path directly:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsNT\CurrentVersion\NetworkCards
```

#### Examples
Attaching to an already running process: (PID `1234`)
```Shell
NetworkAdapterSelector.Hook.exe --network "{ABCDEFGH-0000-0000-0000-000000000000}" --attach 1234
```

Starting a new instance of `notepad.exe` and attaching to it after one second:
```Shell
NetworkAdapterSelector.Hook.exe --network "{ABCDEFGH-0000-0000-0000-000000000000}" --execute "C:\Windows\System32\notepad.exe" --delay 1000
```

## Technology
Both parts of the solution are in pure C# using EasyHook library and SharpShell framework. It was a little experiment to see how stable is EasyHook and how SharpShell can perform in terms of performance.

## License
Copyright (C) 2017-2020 Soroush Falahati

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.