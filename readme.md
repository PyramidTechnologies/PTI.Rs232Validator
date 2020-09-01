[![NuGet](https://img.shields.io/nuget/v/PTI.Rs232Validator.svg)](https://www.nuget.org/packages/PTI.Rs232Validator/)

# PTI.Rs232Validator

A re-implementation of the classic RS-232 bill validator protocol

## Another API?

Yes! This API is designed to incorporate all the latest and greatest dotnet core has to offer. The old .NET framework 
version will remain available if you are happy with it. 

Our new API is designed to be faster, more memory-efficient, and provide better logging utilities.

## Non-Windows Support 

We are dependent on a preview version of System.IO.Ports when targeting dotnetcore and non-Windows platforms. We do 
not recommend using a preview package in production systems. Regular .NET Framework platforms use the built-in 
System.IO.Ports package.

## Logging 

We use a simple logging interface to decouple from any external logging framework. Just implement our ILogger and 
you're all set. In general, we follow these rules for each log level.  

* Error level logs notify you of API misuse and device issues 
* Info level logs report initialization and state/event changed 
* Debug level logs report more detail than info but at a similar rate 
* Trace level logs report all raw and decoded serial traffic

## Strict Mode 

In strict mode, the acceptor will be expected to follow the specification without any exception. Certain older 
models may have slight protocol violations that do not affect the typical user. Use this mode if you are are 
experiencing unusual behavior with your acceptor.

## Escrow Mode 

Escrow mode allows the host to explicitly issue stack and return commands. This may be useful if your application 
requires some kind of flow control between bill feeds. Use the Stack() and Return() commands to direct the acceptor 
to perform a stack or return, respectively.

## Liveness Check

The RS-232 protocol does not provide a "ping" mechansim. Instead, we have a period of time in which we count a number
of healhy poll responses. Once this is staisfied, it is assumed that the attached device is operating properly.
