using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Utility;
using System;
using System.IO.Ports;

namespace PTI.Rs232Validator.SerialProviders;

/// <summary>
///     Default RS232 serial port configuration
///     At 9600 baud with 10 bits per transmit, we have a max data rate
///     of 89.6 KB/second.
/// </summary>
public abstract class BaseSerialPortProvider : ISerialProvider
{
    /// <summary>
    ///     Original port name
    /// </summary>
    private readonly string _portName;

    /// <summary>
    ///     Create a new base port provider on this serial port
    /// </summary>
    /// <param name="portName">OS port name</param>
    /// <param name="logger"><see cref="Logger"/>.</param>
    protected BaseSerialPortProvider(string portName, ILogger logger)
    {
        _portName = portName;
        Logger = logger;
    }

    /// <summary>
    ///     Native serial port handle
    /// </summary>
    protected abstract SerialPort? Port { get; }

    /// <inheritdoc />
    public bool IsOpen => Port?.IsOpen ?? false;

    /// <summary>
    /// An instance of <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public bool TryOpen()
    {
        try
        {
            if (IsOpen)
            {
                Logger.LogInfo("Port {0} is already open", _portName);
                return true;
            }

            Port?.Open();
            if (Port is null || !Port.IsOpen)
            {
                return false;
            }

            // On open, clear any pending reads or writes
            Port.DiscardInBuffer();
            Port.DiscardOutBuffer();

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Logger.LogError("Failed to open port {0} because it some other process or instance has it open",
                _portName);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to open port {0}: {1}", _portName, ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        Port?.Close();
    }

    /// <inheritdoc />
    public byte[] Read(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException($"{count} must be greater than zero");
        }

        if (Port is null || !IsOpen)
        {
            Logger.LogError("Cannot read from port that is not open. Try opening it.");
            return [];
        }

        try
        {
            // Read one byte at a time to avoid timeout issues
            var receive = new byte[count];
            for (var i = 0; i < count; ++i)
            {
                receive[i] = (byte)Port.ReadByte();
            }

            Logger.LogTrace("Received {0}", receive.ConvertToHexString(true));

            return receive;
        }
        catch (TimeoutException)
        {
            Logger.LogTrace(
                "A read operation timed out. This is expected behavior while the device is feeding or stacking a bill");
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to read port: {0}{1}{2}", ex.Message, Environment.NewLine,
                ex.StackTrace ?? string.Empty);
            return [];
        }
    }

    /// <inheritdoc />
    public void Write(byte[] data)
    {
        try
        {
            Logger.LogTrace("Sent {0}", data.ConvertToHexString(true));

            Port?.Write(data, 0, data.Length);
        }
        catch (TimeoutException)
        {
            Logger.LogTrace(
                "A write operation timed out. This is expected behavior while the device is feeding or stacking a bill");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to write port: {0}{1}{2}", ex.Message, Environment.NewLine,
                ex.StackTrace ?? string.Empty);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Port?.Close();
        Port?.Dispose();
    }
}