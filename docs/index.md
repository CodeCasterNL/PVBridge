---
title: Documentation Home
order: 1
---
## PVBridge Solar Status Syncer
This program reads the output of internet-connected, data-uploading GoodWe inverters through the GoodWe API, and uploads it to PVOutput.org. 

It does so every ~5 minutes, and syncs as back far as 14 days including today. The application exists of a Windows Service (running in the background whether someone is logged on or not), and a configuration/status UI. 

* A GoodWe inverter that uploads its data to [semsportal.com/](https://semsportal.com/).
* An API key and System ID for [PVOutput.org](https://pvoutput.org/account.jsp).
* Windows 11 (10 will work, unsupported).
* The 64-bit version of the .NET Desktop Runtime, 6.0 or higher (should come with the installer).
* Administrative permissions to install the PVBridge Windows Service.
