using PTI.Rs232Validator.Loggers;
using System;
using System.Collections.Generic;
using System.IO;

namespace PTI.Rs232Validator.CLI.Loggers;

/// <summary>
/// An implementation of <see cref="NamedLogger{T}"/> that logs messages to a file.
/// </summary>
public class FileLogger<T> : NamedLogger<T>, IDisposable where T : class
{
    private static readonly Dictionary<string, Tuple<byte, StreamWriter>> _logFilePathMap = new();

    private readonly string _logFilePath;
    private readonly StreamWriter _streamWriter;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLogger{T}"/>.
    /// </summary>
    /// <param name="logFilePath">The file to write log messages to.</param>
    /// <param name="minLogLevel"><see cref="NamedLogger.MinLogLevel"/>.</param>
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
        if (_logFilePathMap.TryGetValue(_logFilePath, out var value))
        {
            _logFilePathMap[_logFilePath] = new Tuple<byte, StreamWriter>((byte)(value.Item1 + 1), value.Item2);
            _streamWriter = _logFilePathMap[_logFilePath].Item2;
        }
        else
        {
            var stream = new FileStream(_logFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _streamWriter = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            _logFilePathMap.Add(_logFilePath, new Tuple<byte, StreamWriter>(1, _streamWriter));
        }
    }

    /// <inheritdoc />
    protected override void Log(LogLevel logLevel, string message)
    {
        var line = $"[{logLevel}] {message}";
        _streamWriter.WriteLine(line);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!_logFilePathMap.TryGetValue(_logFilePath, out var value))
        {
            return;
        }
        
        if (value.Item1 == 1)
        {
            _logFilePathMap.Remove(_logFilePath);
            _streamWriter.Flush();;
            _streamWriter.Dispose();
        }
        else
        {
            _logFilePathMap[_logFilePath] = new Tuple<byte, StreamWriter>((byte)(value.Item1 - 1), value.Item2);
        }
    }
}