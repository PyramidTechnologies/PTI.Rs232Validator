# SoftBill.NET

A .NET Windows Forms application that serves as a fork of the Python SoftBill project, designed to emulate Bill Acceptors using the Mars Protocol.

## Overview

SoftBill.NET provides a Windows-based interface for emulating bill acceptor devices that communicate via the Mars Protocol. This application allows for testing and development of systems that interact with bill acceptor hardware without requiring physical devices.

## Features

- Bill acceptor emulation using Mars Protocol
- Windows Forms GUI for easy interaction
- Serial port communication support
- Real-time monitoring and control

## Requirements

- .NET 9.0 or later
- Windows operating system
- Serial port access for device communication

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

Or build and run the executable from the `bin` directory after building.

## Project Structure

- `Acceptor.cs` - Core bill acceptor functionality
- `BillAcceptor.cs` - Bill acceptor implementation
- `AutoPilot.cs` - Automated operation features
- `Monitor.cs` - Device monitoring capabilities
- `MainForm.cs` - Main application GUI
- `Program.cs` - Application entry point

## License

This project is a .NET port of the original Python SoftBill project.