using PTI.Rs232Validator.Loggers;
using System.Collections.Generic;

namespace PTI.Rs232Validator.CLI.Loggers;

/// <summary>
/// An implementation of <see cref="ILogger"/> that logs messages to multiple loggers.
/// </summary>
public class MultiLogger : ILogger
{
    private readonly IEnumerable<ILogger> _loggers;

    /// <summary>
    /// Initializes a new instance of <see cref="MultiLogger"/>.
    /// </summary>
    /// <param name="loggers">The loggers to log messages to.</param>
    public MultiLogger(IEnumerable<ILogger> loggers)
    {
        _loggers = loggers;
    }

    public void LogTrace(string format, params object[] args)
    {
        foreach (var logger in _loggers)
        {
            logger.LogTrace(format, args);
        }
    }

    public void LogDebug(string format, params object[] args)
    {
        foreach (var logger in _loggers)
        {
            logger.LogDebug(format, args);
        }
    }

    public void LogInfo(string format, params object[] args)
    {
        foreach (var logger in _loggers)
        {
            logger.LogInfo(format, args);
        }
    }

    public void LogError(string format, params object[] args)
    {
        foreach (var logger in _loggers)
        {
            logger.LogError(format, args);
        }
    }
}