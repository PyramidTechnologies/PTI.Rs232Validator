namespace PTI.Rs232Validator.CLI
{
    using System;

    /// <summary>
    ///     Color console logger
    /// </summary>
    internal class ConsoleLogger : BaseLogger
    {
        /// <inheritdoc />
        public override void Trace(string format, params object[] args)
        {
            if (Level < 4)
            {
                return;
            }

            Log("TRACE", ConsoleColor.DarkGray, format, args);
        }

        /// <inheritdoc />
        public override void Debug(string format, params object[] args)
        {
            if (Level < 3)
            {
                return;
            }

            Log("DEBUG", ConsoleColor.Gray, format, args);
        }

        /// <inheritdoc />
        public override void Info(string format, params object[] args)
        {
            if (Level < 2)
            {
                return;
            }

            Log("INFOR", ConsoleColor.White, format, args);
        }

        /// <inheritdoc />
        public override void Error(string format, params object[] args)
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
            Console.Write($"[{level}] {DateTime.Now - Epoch}::");
            Console.WriteLine(format, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}