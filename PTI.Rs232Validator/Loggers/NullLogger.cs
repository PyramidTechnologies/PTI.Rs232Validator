namespace PTI.Rs232Validator.Loggers;

/// <summary>
/// An implementation of <see cref="ILogger"/> that does nothing.
/// </summary>
internal class NullLogger : ILogger
{
    /// <inheritdoc />
    public void LogTrace(string format, params object[] args)
    {
    }

    /// <inheritdoc />
    public void LogDebug(string format, params object[] args)
    {
    }

    /// <inheritdoc />
    public void LogInfo(string format, params object[] args)
    {
    }

    /// <inheritdoc />
    public void LogError(string format, params object[] args)
    {
    }
}