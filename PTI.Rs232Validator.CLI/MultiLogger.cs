namespace PTI.Rs232Validator.CLI
{
    using System.Collections.Generic;

    /// <summary>
    ///     Logs to multiple ILoggers at once
    /// </summary>
    public class MultiLogger : ILogger
    {
        private readonly IEnumerable<ILogger> _loggers;

        public MultiLogger(IEnumerable<ILogger> loggers)
        {
            _loggers = loggers;
        }

        /// <inheritdoc />
        public void Trace(string format, params object[] args)
        {
            foreach (var logger in _loggers)
            {
                logger.Trace(format, args);
            }
        }

        /// <inheritdoc />
        public void Debug(string format, params object[] args)
        {
            foreach (var logger in _loggers)
            {
                logger.Debug(format, args);
            }
        }

        /// <inheritdoc />
        public void Info(string format, params object[] args)
        {
            foreach (var logger in _loggers)
            {
                logger.Info(format, args);
            }
        }

        /// <inheritdoc />
        public void Error(string format, params object[] args)
        {
            foreach (var logger in _loggers)
            {
                logger.Error(format, args);
            }
        }
    }
}