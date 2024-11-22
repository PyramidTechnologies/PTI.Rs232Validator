using PTI.Rs232Validator.Cli.Loggers;
using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Validators;
using System;
using System.IO;

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
    /// <returns>If successful, a new instance of <see cref="BillValidator"/>; otherwise, null.</returns>
    public static BillValidator? CreateBillValidator(string serialPortName)
    {
        var serialPortProviderLogger = CreateMultiLogger<SerialPortProvider>();
        var serialPortProvider = SerialPortProvider.CreateUsbSerialProvider(serialPortProviderLogger, serialPortName);
        if (serialPortProvider is null)
        {
            return null;
        }

        var billValidatorLogger = CreateMultiLogger<BillValidator>();
        var configuration = new Rs232Configuration();
        return new BillValidator(billValidatorLogger, serialPortProvider, configuration);
    }
}