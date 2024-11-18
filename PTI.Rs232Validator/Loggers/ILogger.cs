namespace PTI.Rs232Validator.Loggers;

/// <summary>
/// The levels of log messages.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// A trace message.
    /// </summary>
    Trace,
    
    /// <summary>
    /// A debug message.
    /// </summary>
    Debug,
    
    /// <summary>
    /// An info message.
    /// </summary>
    Info,
    
    /// <summary>
    /// An error message.
    /// </summary>
    Error
}

/// <summary>
/// A generic logging interface.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a message at the trace level.
    /// </summary>
    public void LogTrace(string format, params object[] args);

    /// <summary>
    /// Logs a message at the debug level.
    /// </summary>
    public void LogDebug(string format, params object[] args);

    /// <summary>
    /// Logs a message at the info level.
    /// </summary>
    public void LogInfo(string format, params object[] args);

    /// <summary>
    /// Logs a message at the error level.
    /// </summary>
    public void LogError(string format, params object[] args);
}