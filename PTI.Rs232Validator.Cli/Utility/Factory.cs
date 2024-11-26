using PTI.Rs232Validator.Cli.Loggers;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using System;
using System.IO;
using BillValidator = PTI.Rs232Validator.BillValidators.BillValidator;

namespace PTI.Rs232Validator.Cli.Utility;

/// <summary>
/// A factory for creating objects.
/// </summary>
public static class Factory
{
    /// <summary>
    /// Creates a new instance of <see cref="MultiLogger"/>.
    /// </summary>
    /// <typeparam name="T">The class to log for.</typeparam>
    /// <returns>A new instance of <see cref="MultiLogger"/>.</returns>
    public static MultiLogger CreateMultiLogger<T>() where T : class
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
    
    /// <summary>
    /// Creates a new instance of <see cref="BillValidator"/>.
    /// </summary>
    /// <param name="serialPortName">The name of the serial port to use.</param>
    /// <returns>A new instance of <see cref="BillValidator"/>.</returns>
    public static BillValidator CreateBillValidator(string serialPortName)
    {
        var serialPortProviderLogger = CreateMultiLogger<SerialProvider>();
        var serialPortProvider = SerialProvider.CreateUsbSerialProvider(serialPortProviderLogger, serialPortName);
        
        var billValidatorLogger = CreateMultiLogger<BillValidator>();
        var configuration = new Rs232Configuration();
        return new BillValidator(billValidatorLogger, serialPortProvider, configuration);
    }
}