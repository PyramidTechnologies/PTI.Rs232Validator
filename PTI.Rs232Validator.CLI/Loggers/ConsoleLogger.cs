using PTI.Rs232Validator.Loggers;
using System;

namespace PTI.Rs232Validator.CLI.Loggers;

/// <summary>
/// An implementation of <see cref="NamedLogger{T}"/> that logs colored messages to the console.
/// </summary>
public class ConsoleLogger<T> : NamedLogger<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleLogger{T}"/>.
    /// </summary>
    /// <param name="minLogLevel"><see cref="NamedLogger.MinLogLevel"/>.</param>
    public ConsoleLogger(LogLevel minLogLevel) : base(minLogLevel)
    {
    }
    
    protected override void Log(LogLevel logLevel, string message)
    {
        Console.ForegroundColor = logLevel switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Error => ConsoleColor.Red,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
        
        Console.WriteLine($"[{logLevel}] {message}");
        Console.ForegroundColor = ConsoleColor.White;
    }
}