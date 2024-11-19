using System;

namespace PTI.Rs232Validator.Loggers;

/// <summary>
/// An implementation of <see cref="ILogger"/> that has a name and logs certain messages.
/// </summary>
public abstract class NamedLogger : ILogger
{
    /// <summary>
    /// Creates a new instance of <see cref="NamedLogger"/>.
    /// </summary>
    /// <param name="name"><see cref="Name"/>.</param>
    /// <param name="minLogLevel"><see cref="MinLogLevel"/>.</param>
    protected NamedLogger(string name, LogLevel minLogLevel)
    {
        MinLogLevel = minLogLevel;
        Name = name;
    }
    
    /// <summary>
    /// Name of this instance.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The minimum log level a message must be to be logged.
    /// </summary>
    public LogLevel MinLogLevel { get; }

    /// <inheritdoc />
    public void LogTrace(string format, params object[] args)
    {
        if (MinLogLevel > LogLevel.Trace)
        {
            return;
        }
        
        Log(LogLevel.Trace, CreateMessage(format, args));
    }

    /// <inheritdoc />
    public void LogDebug(string format, params object[] args)
    {
        if (MinLogLevel > LogLevel.Debug)
        {
            return;
        }
        
        Log(LogLevel.Debug, CreateMessage(format, args));
    }

    /// <inheritdoc />
    public void LogInfo(string format, params object[] args)
    {
        if (MinLogLevel > LogLevel.Info)
        {
            return;
        }
        
        Log(LogLevel.Info, CreateMessage(format, args));
    }

    /// <inheritdoc />
    public void LogError(string format, params object[] args)
    {
        if (MinLogLevel > LogLevel.Error)
        {
            return;
        }
        
        Log(LogLevel.Error, CreateMessage(format, args));
    }

    /// <summary>
    /// Logs a specified message at the specified log level.
    /// </summary>
    /// <param name="logLevel">The log level of the message.</param>
    /// <param name="message">The message to log.</param>
    protected abstract void Log(LogLevel logLevel, string message);
    
    private string CreateMessage(string format, params object[] args)
    {
        return $"[{Name}] {string.Format(format, args)}";
    }
}

/// <summary>
/// An implementation of <see cref="NamedLogger"/> that uses the name of the generic type as the name of the logger.
/// </summary>
/// <typeparam name="T">The class to log for.</typeparam>
public abstract class NamedLogger<T> : NamedLogger where T : class
{
    /// <summary>
    /// Initializes a new instance of <see cref="NamedLogger{T}"/>.
    /// </summary>
    /// <param name="minLogLevel"><see cref="NamedLogger.MinLogLevel"/>.</param>
    protected NamedLogger(LogLevel minLogLevel) : base(typeof(T).Name, minLogLevel)
    {
    }
}