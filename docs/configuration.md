---
title: Configuration
order: 30
---
## Configuring the service
When the setup has installed the service, or when you run the "PVBridge Configuration UI" from the start menu, you can configure between which accounts the service synchronizes data.

Enter your credentials for GoodWe and PVOutput, click "Read device info", select an inverter and enter a system ID respectively, and save the configuration.

The service will pick up the configuration change and restart its logic on the new credentials.

The credentials you enter are saved encrypted using AES, the key only readable by an administrator or the service (runs as NetworkService).
