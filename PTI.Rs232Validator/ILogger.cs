namespace PTI.Rs232Validator
{
    /// <summary>
    ///     Generic logging interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     Log at trace level
        /// </summary>
        void Trace(string format, params object[] args);

        /// <summary>
        ///     Log at trace level
        /// </summary>
        void Debug(string format, params object[] args);

        /// <summary>
        ///     Log at trace level
        /// </summary>
        void Info(string format, params object[] args);

        /// <summary>
        ///     Log at trace level
        /// </summary>
        void Error(string format, params object[] args);
    }
}