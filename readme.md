# PTI.Rs232Validator

A re-implementation of the classic RS-232 bill validator protocol

## Another API?

Yes! This API is designed to incorporate all the latest and greatest dotnet core has to offer. The old .NET framework 
version will remain available if you are happy with it. 

Our new API is designed to be faster, more memory-efficient, and provide better logging utilities.

## Logging 

We use a simple logging interface to decouple from any external logging framework. Just implement our ILogger and 
you're all set. In general, we follow these rules for each log level.  

* Error level logs notify you of API misuse and device issues 
* Info level logs report initialization and state/event changed 
* Debug level logs report more detail than info but at a similar rate 
* Trace level logs report all raw and decoded serial traffic