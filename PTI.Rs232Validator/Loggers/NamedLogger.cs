using System;

namespace PTI.Rs232Validator.Loggers;

/// <summary>
/// An implementation of <see cref="ILogger"/> that has a name and includes timestamps in log messages.
/// </summary>
public abstract class NamedLogger : ILogger
{
    /// <summary>
    /// Creates a new instance of <see cref="NamedLogger"/>.
    /// </summary>
    /// <param name="name"><see cref="Name"/>.</param>
    protected NamedLogger(string name)
    {
        Name = name;
    }
    
    /// <summary>
    /// Name of this instance.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public abstract void Trace(string format, params object[] args);

    /// <inheritdoc />
    public abstract void Debug(string format, params object[] args);

    /// <inheritdoc />
    public abstract void Info(string format, params object[] args);

    /// <inheritdoc />
    public abstract void Error(string format, params object[] args);
    
    /// <summary>
    /// Creates a log message with <see cref="Name"/> and the current timestamp.
    /// </summary>
    protected string CreateMessage(string format, params object[] args)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        return $"[{timestamp}] [{Name}] {string.Format(format, args)}";
    }
}