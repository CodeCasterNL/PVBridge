---
title: Downloads
order: 50
---
## Latest release
Download the latest release on the [GitHub Releases page](https://github.com/CodeCasterNL/PVBridge/releases).

You'll need the latest PVBridge-{version}.msi ([currently 0.7.2](https://github.com/CodeCasterNL/PVBridge/releases/tag/v0.7.2)), ignore SmartScreen (click "More info" and "Run anyway").

See [Configuring the service](./configuration.md) for configuring the service using the UI.

## Updating / uninstalling
Unfortunately, the setup doesn't properly delete the service on uninstall. This means the service will remain installed and active after uninstallation, unless you **manually delete PVBridge**.

You can do so by starting an administrative command prompt, and:

```bash
> cd "C:\Program Files\PVBridge"

> PVBridge.exe service uninstall
```

Now you can uninstall the earlier version from the Windows "Add or remove programs" control panel, i.e. "Apps & Features". If it doesn't appear there, it's now safe to delete `C:\Program Files\PVBridge` or wherever you installed it.

Uninstall also doesn't clean up log and configuration data on uninstall. You can delete `C:\ProgramData\PVBridge` whenever you want.
