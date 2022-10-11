> ⚠️ 2022-10-11: I'm trying to find time to make the project more robust, but the GoodWe APIs are _really_ unstable lately. They often don't return any data for days on end.
>
> I'll try to release a newer approach in Q4 2022, but due to personal reasons I might not be able to. See [#23](https://github.com/CodeCasterNL/PVBridge/issues/23).

# PVBridge Solar Status Syncer
PVBridge is a tool to upload solar panel information, both in realtime and batched per day, without having to meddle with Python scripts, Task Scheduler and unreadable config files 'n such. Just a simple installer, a background service that starts automatically with your computer and a simple configuration and status UI.

It is a "bridge" between photovoltaic ("PV") systems, so to say.

# What it does
It automatically synchronizes your realtime and historic GoodWe data to PVOutput.org, as soon as you start your Windows machine and as far back as 14 days. Support for longer back is underway. 

It does so in smart batching and respecting the limits of APIs on either side, by gradually backing off and syncing missing periods with as little traffic as possible. Every five minutes or so, the service sends the realtime solar panel output every five minutes or so, higher resolution support incoming.

Both are tracked in [#10](https://github.com/CodeCasterNL/PVBridge/issues/10).

PVBridge runs as a Windows Service, meaning you won't have to start it yourself, and has a configuration UI. The credentials you enter are saved encrypted using AES, the key only readable by an administrator or the service (runs as NetworkService).

We can update live data (realtime power generation, temperature and so on) up to 13 days in the past. If we miss those (the machine has been off for more than two weeks), all we can write is summaries, but we don't yet.

Other inputs and outputs (other inverter APIs, CSV), aggregations ([#26](https://github.com/CodeCasterNL/PVBridge/issues/26)), aggregations to CSV, Excel to CSV and [other features and improvements](https://github.com/CodeCasterNL/PVBridge/issues) are (far) future plans, help is more than welcome! See [Contributing.md](Contributing.md).

# Installation
Download the most recent version "PVBridge-{version}.msi" under "Releases" on [the GitHub page](https://github.com/CodeCasterNL/PVBridge). For older versions, check https://github.com/CodeCasterNL/PVBridge/releases.

# Uninstallation / updating
Unfortunately, the uninstaller is broken. Will fix before v1.0.

For now, manually uninstall before update: see the [downloads page on the documentation site](https://codecasternl.github.io/PVBridge/downloads.html).

# [Documentation](https://codecasternl.github.io/PVBridge/)
For documentation on installing and setting up, see the [documentation site](https://codecasternl.github.io/PVBridge/).

# Status
Current status: as-is development-prerelease. This means that it might work on your computer, but if it doesn't, please [open an issue](https://github.com/CodeCasterNL/PVBridge/issues/new) with as much relevant details as possible and help me figure it out.

It also means that the behavior of the application can change at any release, just as the development pace and support level.

Once the basic functionality is stabilized, a 1.0 version will be released.

# Prerequisites
In order to install this program, you need:
* Windows 11 (10 will work, unsupported).
* A GoodWe inverter that uploads its data to https://semsportal.com/.
* An API key and System ID for [PVOutput.org](https://pvoutput.org/account.jsp).
* Administrative permissions to install the PVBridge Windows Service.
* The 64-bit version of the .NET Desktop Runtime, 6.0 or higher (should come with the installer).

# Supported behavior
This is what the app should be able to do in its current state:

* Read current and historic PV data about a GoodWe inverter with a single-panel setup. 
* Write current and historic PV data to a PVOutput.org with a single-panel setup. 

# Unsupported
These features exist in code and might even be callable or configurable, but are not supported. Try at your own risk.

## Configuration
* GoodWeFileReader input
* CSV output

## Console
The console can do the following things, for testing/debugging/force-syncing without having to run the whole service loop:

    PVBridge.exe sync 2020-12-22
    PVBridge.exe sync # Live status
    PVBridge.exe sync 2021-06-01[T15:15] 2021-06-09

These are subject to change.

## Badges
[![CI build/test develop](https://github.com/CodeCasterNL/PVBridge/actions/workflows/DotNetCore6.0-CI.yml/badge.svg)](https://github.com/CodeCasterNL/PVBridge/actions/workflows/DotNetCore6.0-CI.yml)

Thanks to [pyrocumulus/pvoutput.net](https://github.com/pyrocumulus/pvoutput.net) for maintaining the PVOutput.org API client for .NET.
