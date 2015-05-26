TEST
Cloud Foundry Windows Extensions
===============================================

This repository includes the Droplet Execution Agent (DEA) written in C# designed to allow Windows Server to run in a Cloud Foundry 2.0 environment.

What are the Cloud Foundry Windows Extensions?
----------------------------------------------

This project is an effort to extend Cloud Foundry so it runs .Net web applications on a Windows environment.

Cloud Foundry was developed on Linux and lacks support for Microsoft Windows Server environments. The Cloud Foundry Windows Extensions are built entirely on Windows and .NET.
We have ported the Cloud Foundry NATS client and DEA (Droplet Execution Engine) to .NET and Windows Server.
The Cloud Foundry Windows Extensions allow Windows Servers to be full-fledged Cloud Foundry 2.0 citizens.
Windows developers can now benefit from the same Cloud Foundry application deployment advances that Ruby developers already enjoy.

The .NET Extensions also make it possible for the open source developer community to add new Cloud Foundry enabled buildpacks to Windows Servers.
