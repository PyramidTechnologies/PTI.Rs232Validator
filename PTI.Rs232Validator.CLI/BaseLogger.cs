namespace PTI.Rs232Validator.CLI
{
    using System;

    public abstract class BaseLogger : ILogger
    {
        /// <summary>
        ///     Relative timestamp
        /// </summary>
        protected readonly DateTime Epoch = DateTime.Now;

        /// <summary>
        ///     Logging level
        ///     0: None
        ///     1: Error                     (Red)
        ///     2: Error, Info               (White)
        ///     3: Error, Info, Debug        (Gray)
        ///     4: Error, Info, Debug, Trace (DarkGray)
        /// </summary>
        public int Level { get; set; }
        
        /// <inheritdoc />
        public abstract void Trace(string format, params object[] args);
        
        /// <inheritdoc />
        public abstract void Debug(string format, params object[] args);
        
        /// <inheritdoc />
        public abstract void Info(string format, params object[] args);

        /// <inheritdoc />
        public abstract void Error(string format, params object[] args);
    }
}