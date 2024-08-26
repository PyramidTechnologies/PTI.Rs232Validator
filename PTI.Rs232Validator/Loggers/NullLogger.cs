namespace PTI.Rs232Validator.Loggers;

/// <summary>
///     A nop logger
/// </summary>
internal class NullLogger : ILogger
{
    /// <inheritdoc />
    public void Trace(string format, params object[] args)
    {
    }

    /// <inheritdoc />
    public void Debug(string format, params object[] args)
    {
    }

    /// <inheritdoc />
    public void Info(string format, params object[] args)
    {
    }

    /// <inheritdoc />
    public void Error(string format, params object[] args)
    {
    }
}