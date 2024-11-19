using PTI.Rs232Validator;
using PTI.Rs232Validator.CLI.Loggers;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Validators;
using System;
using System.IO;
using System.Threading;

const string programUsage = "Usage: rs232validator.cli.exe [portName] [billTypeToReturn]";
if (args.Length == 0)
{
    Console.Error.WriteLine("portName was not provided.");
    Console.WriteLine(programUsage);
    return;
}

if (args.Length == 1)
{
    Console.Error.WriteLine("billTypeToReturn was not provided.");
    Console.WriteLine(programUsage);
    return;
}

var portName = args[0];
if (!int.TryParse(args[1], out var billTypeToReturn))
{
    Console.Error.WriteLine("billTypeToReturn was not a valid integer.");
    Console.WriteLine(programUsage);
    return;
}

Console.CancelKeyPress += (_, _) => Environment.Exit(0);

var serialPortProviderLogger = CreateMultiLogger<UsbSerialProvider>();
var serialPortProvider = new UsbSerialProvider(portName, serialPortProviderLogger);

var billValidatorLogger = CreateMultiLogger<BillValidator>();
var configuration = new Rs232Configuration();
var billValidator = new BillValidator(billValidatorLogger, serialPortProvider, configuration);

var programLogger = CreateMultiLogger<Program>();

billValidator.OnStateChanged += (_, state) => { programLogger.LogInfo($"The state has changed from {state.OldState} to {state.NewState}"); };

billValidator.OnEventReported += (_, evt) => { programLogger.LogInfo($"Received the following event(s): {evt}"); };

billValidator.OnCashboxRemoved += (sender, eventArgs) => { programLogger.LogInfo("The cashbox was removed."); };

billValidator.OnCashboxAttached += (sender, eventArgs) => { programLogger.LogInfo("The cashbox was attached."); };

billValidator.OnBillStacked += (sender, billType) => { programLogger.LogInfo($"A bill of type {billType} was stacked."); };

billValidator.OnBillEscrowed += (_, billType) =>
{
    if (billType == billTypeToReturn)
    {
        programLogger.LogInfo($"Sent a request to return of a bill of type {billType}.");
        billValidator.ReturnBill();
        return;
    }
    
    programLogger.LogInfo($"Sent a request to stack a bill of type {billType}.");
    billValidator.StackBill();
};

billValidator.OnConnectionLost += (sender, eventArgs) => { programLogger.LogError("Lost connection to the acceptor."); };

if (!billValidator.StartMessageLoop())
{
    programLogger.LogError("Failed to start the message loop.");
    return;
}

programLogger.LogInfo("Now polling the acceptor. Press CTRL+C to exit.");
while (true)
{
    if (billValidator.IsConnectionPresent)
    {
        Thread.Sleep(configuration.PollingPeriod);
        continue;
    }
    
    programLogger.LogError("The acceptor is no longer connected. Exiting now.");
    billValidator.StopMessageLoop();
    break;
}

return;

MultiLogger CreateMultiLogger<T>() where T : class
{
    var currentDirectory = Environment.CurrentDirectory;
    var traceLogFilePath = Path.Combine(currentDirectory, "trace.log");
    var debugLogFilePath = Path.Combine(currentDirectory, "debug.log");
    var infoLogFilePath = Path.Combine(currentDirectory, "info.log");
    var errorLogFilePath = Path.Combine(currentDirectory, "error.log");

    return new MultiLogger([
        new FileLogger<T>(traceLogFilePath, LogLevel.Trace),
        new FileLogger<T>(debugLogFilePath, LogLevel.Debug),
        new FileLogger<T>(infoLogFilePath, LogLevel.Info),
        new FileLogger<T>(errorLogFilePath, LogLevel.Error),
        
        new ConsoleLogger<T>(LogLevel.Info)
    ]);
}