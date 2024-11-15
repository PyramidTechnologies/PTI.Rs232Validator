namespace PTI.Rs232Validator.Loggers;

/// <summary>
/// A generic logging interface.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a message at the trace level.
    /// </summary>
    void Trace(string format, params object[] args);

    /// <summary>
    /// Logs a message at the debug level.
    /// </summary>
    void Debug(string format, params object[] args);

    /// <summary>
    /// Logs a message at the info level.
    /// </summary>
    void Info(string format, params object[] args);

    /// <summary>
    /// Logs a message at the error level.
    /// </summary>
    void Error(string format, params object[] args);
}