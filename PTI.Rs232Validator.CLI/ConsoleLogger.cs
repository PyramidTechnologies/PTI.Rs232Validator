namespace PTI.Rs232Validator.CLI
{
    using System;

    /// <summary>
    ///     Color console logger
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        /// <summary>
        ///     Relative timestamp
        /// </summary>
        private readonly DateTime _epoch = DateTime.Now;

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
        public void Trace(string format, params object[] args)
        {
            if (Level < 4)
            {
                return;
            }

            Log("TRACE", ConsoleColor.DarkGray, format, args);
        }

        /// <inheritdoc />
        public void Debug(string format, params object[] args)
        {
            if (Level < 3)
            {
                return;
            }

            Log("DEBUG", ConsoleColor.Gray, format, args);
        }

        /// <inheritdoc />
        public void Info(string format, params object[] args)
        {
            if (Level < 2)
            {
                return;
            }

            Log("INFOR", ConsoleColor.White, format, args);
        }

        /// <inheritdoc />
        public void Error(string format, params object[] args)
        {
            if (Level < 1)
            {
                return;
            }

            Log("ERROR", ConsoleColor.Red, format, args);
        }

        private void Log(string level, ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.Write($"[{level}] {DateTime.Now - _epoch}::");
            Console.WriteLine(format, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}