---
title: Changelog
order: 40
---
## Changelog

### [v0.7.0] (upcoming)
Branched from 0.6.2, so no inverter selection yet, fixed:

* Command-line sync further back than 14 days
* Loop logic rewritten, should now wait until the next interval and not fall back to backlog sync.
* Log JSON before deserializing, not after

### v0.6.4 (removed)
Fix GoodWe inverter selection, or so we hope.

### v0.6.3 (removed)
Save GoodWe plant installation date, looks like the moment the first inverter was turned on.  
Then use that date to not sync before then, instead of always 14 days back.

### [v0.6.2](https://github.com/CodeCasterNL/PVBridge/releases/tag/v0.6.2)
* Parse API dates according to user settings.
* Fix event log logging.

### [v0.5.0](https://github.com/CodeCasterNL/PVBridge/releases/tag/v0.5.0)
First release.
