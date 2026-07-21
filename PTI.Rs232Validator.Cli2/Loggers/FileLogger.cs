using PTI.Rs232Validator.Loggers;
using System;
using System.Collections.Generic;
using System.IO;

namespace PTI.Rs232Validator.Cli.Loggers;

internal static class FileLogger
{
    public static readonly Dictionary<string, Tuple<byte, StreamWriter>> LogFilePathMap = new();
}

/// <summary>
/// An implementation of <see cref="NamedLogger{T}"/> that logs messages to a file.
/// </summary>
public class FileLogger<T> : NamedLogger<T>, IDisposable where T : class
{
    private readonly string _logFilePath;
    private readonly StreamWriter _streamWriter;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLogger{T}"/>.
    /// </summary>
    /// <param name="logFilePath">The file to write log messages to.</param>
    /// <param name="minLogLevel"><see cref="NamedLogger{T}.MinLogLevel"/>.</param>
    /// <exception cref="UnauthorizedAccessException">
    /// The access requested is not permitted by the operating system for the specified path,
    /// such as when access is Write or ReadWrite and the file or directory is set for read-only access.
    /// </exception>
    /// <remarks>
    /// If the specified file does not exist, it will be created.
    /// If the specified file exists, it will be appended to.
    /// </remarks>
    public FileLogger(string logFilePath, LogLevel minLogLevel) : base(minLogLevel)
    {
        _logFilePath = logFilePath;
        if (FileLogger.LogFilePathMap.TryGetValue(_logFilePath, out var value))
        {
            FileLogger.LogFilePathMap[_logFilePath] =
                new Tuple<byte, StreamWriter>((byte)(value.Item1 + 1), value.Item2);
            _streamWriter = FileLogger.LogFilePathMap[_logFilePath].Item2;
        }
        else
        {
            var stream = new FileStream(_logFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _streamWriter = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            FileLogger.LogFilePathMap.Add(_logFilePath, new Tuple<byte, StreamWriter>(1, _streamWriter));
        }
    }

    /// <inheritdoc />
    protected override void Log(string name, LogLevel logLevel, string format, params object[] args)
    {
        var line = $"[{name}] [{logLevel}] {string.Format(format, args)}";
        _streamWriter.WriteLine(line);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!FileLogger.LogFilePathMap.TryGetValue(_logFilePath, out var value))
        {
            return;
        }

        if (value.Item1 == 1)
        {
            FileLogger.LogFilePathMap.Remove(_logFilePath);
            _streamWriter.Flush();
            ;
            _streamWriter.Dispose();
        }
        else
        {
            FileLogger.LogFilePathMap[_logFilePath] =
                new Tuple<byte, StreamWriter>((byte)(value.Item1 - 1), value.Item2);
        }
    }
}