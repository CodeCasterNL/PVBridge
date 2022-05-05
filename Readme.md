# PVBridge Solar Status Syncer
This is a tool to upload solar panel information, both in realtime and batched per day, without having to meddle with Python scripts, Task Scheduler and unreadable config files 'n such. Just a simple installer, a background service that starts automatically with your computer and a simple configuration and status UI.

It is a "bridge" between photovoltaic ("PV") systems, so to say.

At the moment it supports reading from the GoodWe API and sending this data to the PVOutput.org API. 

# [Documentation](https://codecasternl.github.io/PVBridge/)
For documentation on installing and setting up, see the [documentation site](https://codecasternl.github.io/PVBridge/).

# Status
Current status: as-is development-prerelease. This means that it might work on your computer, but if it doesn't, you can open an issue with all relevant details and help me figure it out. It also means that the behavior of the application can change at any release, just as the development pace and support level.

# Prerequisites
In order to install this program, you need:
* A GoodWe inverter that uploads its data to https://semsportal.com/.
* An API key and System ID for [PVOutput.org](https://pvoutput.org/account.jsp).
* Windows 11 (10 will work, unsupported).
* The 64-bit version of the .NET Desktop Runtime, 6.0 or higher (should come with the installer).
* Administrative permissions to install the PVBridge Windows Service.

# Installation
Download the most recent version "PVBridge.{version}.msi" under "Releases" on [the GitHub page](https://github.com/CodeCasterNL/PVBridge). For older versions, check https://github.com/CodeCasterNL/PVBridge/releases.

# Supported behavior
This is what the app should be able to do in its current state:

* Read current and historic PV data about a GoodWe inverter with a single-panel setup. 
* Write current and historic PV data to a PVOutput.org with a single-panel setup. 

We can update live data (realtime power generation, temperature and so on) up to 13 days in the past. If we miss those (the machine has been off for more than two weeks), all we can write is summaries.

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
