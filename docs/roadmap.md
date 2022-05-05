---
title: Roadmap
order: 3
---
## Roadmap
These features may or may not be developed for an upcoming release.

### Partially implemented
* [#15](https://github.com/CodeCasterNL/PVBridge/issues/15): Typed two-way communication. Currently settled on gRPC over Named Pipes, but the library providing the latter doesn't support global pipes yet, so only works in dev, not when installed as service.
* CSV export: write 1-minute intervals to file.
* [#10](https://github.com/CodeCasterNL/PVBridge/issues/15) Go back further in time when syncing the backlog. Works from the command line, maybe.

### Wishlist
* Multi-inverter/channel setup
* More input providers
* More output providers
