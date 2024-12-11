[![NuGet](https://img.shields.io/nuget/v/PTI.Rs232Validator.svg)](https://www.nuget.org/packages/PTI.Rs232Validator/)

# PTI.Rs232Validator

This repo is an implementation of the RS-232 bill acceptor (A.K.A validator) protocol.

## Polling
Poll request messages are sent from the host (i.e. PC) to the acceptor.
Poll response messages are sent from the acceptor to the host.
Poll messages are utilized to obtain information about the acceptor and issue commands to the acceptor.

## Escrow Mode

Escrow mode allows the host to explicitly issue stack and return commands.
This may be useful if an application requires some kind of flow control between bill feeds.
Use the `StackBill()` and `ReturnBill()` methods to direct the acceptor to stack or return a bill, respectively.

## Liveness Check

The RS-232 protocol does not provide a ping mechanism.
Instead, healthy poll responses are counted in succession and if the count exceeds a threshold, the acceptor is considered 'live'.

## Extended and Telemetry Commands
Extended and telemetry commands access information stored within an acceptor, which are not part of poll messages.

### Extended Commands
* 0x01 Barcode Detected: Get the last detected barcode after a power cycle (a paper barcode must have been fed to the acceptor).

### Telemetry Commands
* 0x00 Ping: Verify that the acceptor is running and supports telemetry commands.
* 0x01 Get Serial Number: Get the serial number assigned to the acceptor.
* 0x02 Get Cashbox Metrics: Get the telemetry metrics about the cashbox.
* 0x03 Clear Cashbox Count: Clear the count of bills in the cashbox.
* 0x04 Get Unit Metrics: Get the general telemetry metrics for an acceptor.
* 0x05 Get Service Usage Counters: Get the telemetry metrics since the last time an acceptor was serviced.
* 0x06 Get Service Flags: Get the flags about what needs to be serviced.
* 0x07 Clear Service Flags: Clear 1 or more service flags.
* 0x08 Get Service Info: Get the info that was attached to the last service.
* 0x09 Get Firmware Metrics: Get the telemetry metrics that pertain to an acceptor's firmware.

## Logging

There is a simple logging interface (`ILogger`) to decouple from any external logging framework.
These are the rules for log levels:

* Error level logs report API misuse and device issues.
* Info level logs report important incidents.
* Debug level logs report info encoded in poll responses and the effects of invoking methods. 
* Trace level logs report all raw and decoded serial traffic.