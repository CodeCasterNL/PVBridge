---
menuTitle: Command line
title: Commandline Sync
order: 20
---
## Syncing from the command line
Run an administrative command prompt, then:

```bash
> cd "C:\Program Files\PVBridge"
```

From this directory, you can run the executable on the command line, passing arguments/options to determine which period to sync. The first is "sync" and requires a date:

```bash
> PVBridge.exe sync 2022-08-22
```
Or whatever day you want to sync , in `yyyy-MM-dd` format. 

You can also provide a period to sync by passing a second date:

```bash
> PVBridge.exe sync 2022-08-20 2022-08-22
```
This will sync days 20, 21 and 22 of August 2022.

When a date is further back than 14 days, only the summary will be synced by default. If you donate to PVOutput, you can pass the additional argument `-d|--snapshot-days 90` to expand to the 90 days of PVOutput:

```bash
> PVBridge.exe sync 2022-08-20 2022-08-22 -d 90
```

These commands will sync from and to the providers as configured in the UI.
