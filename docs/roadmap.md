---
title: Roadmap
order: 40
---
## Roadmap
These features may or may not be developed for an upcoming release.

### Partially implemented
* [#15](https://github.com/CodeCasterNL/PVBridge/issues/15): Typed two-way communication between service and UI. Currently settled on gRPC over Named Pipes, but the library providing the latter doesn't support global pipes yet, so only works in dev, not when installed as service.
* CSV export: write 1-minute intervals to file.
* [#10](https://github.com/CodeCasterNL/PVBridge/issues/10): Go back further in time when syncing the backlog. Works from the command line, maybe.

### Wishlist
* Multi-inverter/channel setup
* More input providers (Growatt, ...)
* [#19](https://github.com/CodeCasterNL/PVBridge/issues/19): More output providers (Domoticz, ...)
