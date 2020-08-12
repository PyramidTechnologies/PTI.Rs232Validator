namespace PTI.Rs232Validator.CLI
{
    using System;
    using System.IO;

    public class FileLogger : BaseLogger, IDisposable
    {
        private readonly Stream _stream;
        private readonly StreamWriter _logWriter;

        /// <summary>
        ///     Create a new file logger that write to this log path.
        ///     If the file does not exist, it will be created. If the
        ///     file exists, it will be appended to.
        /// </summary>
        /// <param name="logPath">File to write to</param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public FileLogger(string logPath)
        {
            _stream = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _logWriter = new StreamWriter(_stream)
            {
                AutoFlush = true
            };
        }

        /// <inheritdoc />
        public override void Trace(string format, params object[] args)
        {
            if (Level < 4)
            {
                return;
            }

            Log("TRACE", format, args);
        }


        /// <inheritdoc />
        public override void Debug(string format, params object[] args)
        {
            if (Level < 3)
            {
                return;
            }

            Log("DEBUG", format, args);
        }

        /// <inheritdoc />
        public override void Info(string format, params object[] args)
        {
            if (Level < 2)
            {
                return;
            }

            Log("INFOR", format, args);
        }

        /// <inheritdoc />
        public override void Error(string format, params object[] args)
        {
            if (Level < 1)
            {
                return;
            }

            Log("ERROR", format, args);
        }

        private void Log(string level, string format, params object[] args)
        {
            var line = $"[{level}] {DateTime.Now - Epoch}::{string.Format(format, args)}";
            _logWriter.WriteLine(line);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _logWriter?.Flush();
            _logWriter?.Dispose();
            _stream?.Dispose();
        }
    }
}