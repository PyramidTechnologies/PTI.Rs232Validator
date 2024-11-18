using PTI.Rs232Validator;
using PTI.Rs232Validator.CLI.Loggers;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Validators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

if (args.Length == 0)
{
    Console.Error.WriteLine("A port name was not provided.");
    Console.WriteLine("Usage: rs232validator.cli.exe portName");
    return;
}

var portName = args.FirstOrDefault();
var cts = new CancellationTokenSource();

var currentDirectory = Environment.CurrentDirectory;
var traceLogFilePath = Path.Combine(currentDirectory, "trace.log");
var debugLogFilePath = Path.Combine(currentDirectory, "debug.log");
var infoLogFilePath = Path.Combine(currentDirectory, "info.log");
var errorLogFilePath = Path.Combine(currentDirectory, "error.log");
var logger = new MultiLogger([
    new FileLogger<UsbSerialProvider>(traceLogFilePath, LogLevel.Trace),
    new FileLogger<UsbSerialProvider>(debugLogFilePath, LogLevel.Debug),
    new FileLogger<UsbSerialProvider>(infoLogFilePath, LogLevel.Info),
    new FileLogger<UsbSerialProvider>(errorLogFilePath, LogLevel.Error),
    
    new FileLogger<BillValidator>(traceLogFilePath, LogLevel.Trace),
    new FileLogger<BillValidator>(debugLogFilePath, LogLevel.Debug),
    new FileLogger<BillValidator>(infoLogFilePath, LogLevel.Info),
    new FileLogger<BillValidator>(errorLogFilePath, LogLevel.Error),
    
    new ConsoleLogger<UsbSerialProvider>(LogLevel.Info),
    new ConsoleLogger<BillValidator>(LogLevel.Info)
]);

var serialPortPrivder = new UsbSerialProvider(portName, logger);

var configuration = new Rs232Configuration();
var billValidator = new BillValidator(configuration, logger);

validator.OnLostConnection += (sender, eventArgs) => { config.Logger?.Error($"[APP] Lost connection to acceptor"); };

validator.OnBillInEscrow += (sender, i) =>
{
    // For USA this index represent $20. This example will always return a $20
    // Alternatively you could set the Rs232Config mask to 0x5F to disable a 20.
    if (i == 5)
    {
        config.Logger.Info($"[APP] Issuing a return command for this {BillValues[i]}");

        validator.Return();
    }
    else
    {
        config.Logger.Info($"[APP] Issuing stack command for this {BillValues[i]}");

        validator.Stack();
    }
};

validator.OnCreditIndexReported += (sender, i) => { config.Logger.Info($"[APP] Credit issued: {BillValues[i]}"); };

validator.OnStateChanged += (sender, state) =>
{
    config.Logger.Info($"[APP] State changed from {state.OldState} to {state.NewState}");
};

validator.OnEventReported += (sender, evt) => { config.Logger.Info($"[APP] Event(s) reported: {evt}"); };

validator.OnCashBoxRemoved += (sender, eventArgs) => { config.Logger.Info("[APP] Cash box removed"); };

validator.OnCashBoxAttached += (sender, eventArgs) => { config.Logger.Info("[APP] Cash box attached"); };

if (!validator.StartPollingLoop())
{
    config.Logger.Error("[APP] Failed to start RS232 main loop");
    return;
}

config.Logger.Info("[APP] Validator is now running. CTRL+C to Exit");
while (true)
{
    Thread.Sleep(TimeSpan.FromMilliseconds(100));

    if (!validator.IsUnresponsive)
    {
        continue;
    }

    config.Logger?.Error("[APP] validator failed to start. Quitting now");

    validator.StopPollingLoop();

    break;
}

Console.WriteLine("Hello World!");
return;