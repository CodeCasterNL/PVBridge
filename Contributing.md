﻿# Developing PVBridge Solar Status Syncer
Pull requests welcome, see [issues](https://github.com/CodeCasterNL/PVBridge/issues) or [Docs: Roadmap](https://codecasternl.github.io/PVBridge/roadmap.html).

## System Requirements
* Windows 11
* Visual Studio 2022
* [Microsoft Visual Studio Installer Projects 2022](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2022InstallerProjects) to build the installer project into an MSI.

## Debugging classes
Configure these:
* GoodWeFileReader input (for debugging)
* CSV output (for debugging, broken, might finish later for reporting to documents folder? For taxes and stuff)

## Console
The console, using (beta) `System.CommandLine` can do the following things, for testing/debugging/force-syncing without having to run the whole service loop:

    PVBridge.exe sync 2020-12-22
    PVBridge.exe sync # Live status
    PVBridge.exe sync 2021-06-01[T15:15] 2021-06-09

See [`CodeCaster.PVBridge.Service.CommandLine`](https://github.com/CodeCasterNL/PVBridge/tree/develop/src/CodeCaster.PVBridge.Service/CommandLine).

## Architecture
These are the main parts of the application.

### InputToOutputLoop
[src/CodeCaster.PVBridge.Logic/InputToOutputLoop.cs](https://github.com/CodeCasterNL/PVBridge/blob/develop/src/CodeCaster.PVBridge.Logic/InputToOutputLoop.cs)

Main loop of the logic that determines the current status of the service (a shutdown is actually kindof a hibernate on Windows 10 and up), continues where it left off to sync the possible backlog of data.

### InputToOutputLoopStatus
[src/CodeCaster.PVBridge.Logic/InputToOutputLoopStatus.cs](https://github.com/CodeCasterNL/PVBridge/blob/develop/src/CodeCaster.PVBridge.Logic/InputToOutputLoopStatus.cs)

Bookkeeping for where we are.

### InputToOutputWriter
[src/CodeCaster.PVBridge.Logic/InputToOutputWriter.cs](https://github.com/CodeCasterNL/PVBridge/blob/develop/src/CodeCaster.PVBridge.Logic/InputToOutputWriter.cs)

Reads from inputs, writes to outputs.

## Terminology
A "Snapshot" contains realtime system info, such as power, temperature and cumulative generation (Wh) at the moment of taking the snapshot. 

An "DaySummary" is a the summary of data of a given day, like total power generated.

## Windows Service
Set as Startup Projcet: CodeCaster.PVBridge.Service.

Then hit F5 to run it as a service (as long as it says `"commandLineArgs": "service run"` in `Properties/launchSettings.json`).

Make sure you don't have another version of the service configured with the same accounts while debugging/testing, or you'll be racing another instance.

## Initial configuration
* Run the Configuration UI, or
* Have a configuration file in "C:\ProgramData\PVBridge\PVBridge.AccountConfig.json".

## Service Uninstallation
0. Close the Services MSC.
1. From an elevated command prompt, execute: `sc delete PVBridge`.

## GoodWe
Peculiarity: when the inverter loses its internet connection, it aggregates its data internally for each five minute block and uploads it when connectivity is restored. This is however not rounded on 5 minutes, but can be :01, :06, :11 and so on.

## Releasing
1. Build the solution in release.
1. Rebuild the Installer project.
  * When rebuilding a new MSI with the same version, uninstall the old one first or it won't install.
1. Navigate to `src\CodeCaster.PVBridge.Installer\Release`.
1. Rename `CodeCaster.PVBridge.Installer.msi` to `PVBridge.1.2.3.msi`.
1. Test upgrade, uninstall, reinstall.
1. Push tag, create release.
1. Upload msi to release.
1. Wait.
1. Publish release.
1. TODO: automate.

## Docs build
Push to develop and hope for the best. [Something something jekyll](https://docs.github.com/en/pages/setting-up-a-github-pages-site-with-jekyll/testing-your-github-pages-site-locally-with-jekyll).
