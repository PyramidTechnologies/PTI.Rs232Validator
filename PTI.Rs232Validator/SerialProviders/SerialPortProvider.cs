using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Utility;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace PTI.Rs232Validator.SerialProviders;

/// <summary>
/// A runtime implementation of <see cref="ISerialProvider"/>.
/// </summary>
public class SerialPortProvider : ISerialProvider
{
    private bool _isDisposed;
    
    /// <summary>
    /// Initializes a new instance of <see cref="SerialPortProvider"/>.
    /// </summary>
    /// <param name="logger">An instance of <see cref="ILogger"/>.</param>
    /// <param name="serialPort"><see cref="Port"/>.</param>
    protected SerialPortProvider(ILogger logger, SerialPort serialPort)
    {
        Logger = logger;
        Port = serialPort;
    }

    /// <inheritdoc />
    public bool IsOpen => Port.IsOpen;

    /// <summary>
    /// An instance of <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }
    
    /// <summary>
    /// An instance of <see cref="SerialPort"/>.
    /// </summary>
    protected SerialPort Port { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SerialPortProvider"/> for USB serial emulators.
    /// </summary>
    /// <param name="logger"><see cref="Logger"/>.</param>
    /// <param name="serialPortName">The name of the serial port to use.</param>
    /// <returns>An instance of <see cref="ISerialProvider"/> if successful; otherwise, null.</returns>
    public static ISerialProvider? CreateUsbSerialProvider(ILogger logger, string serialPortName)
    {
        try
        {
            var serialPort = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.Even,
                DataBits = 7,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 100,
                WriteTimeout = 100,
                WriteBufferSize = 1024,
                ReadBufferSize = 1024,
                DtrEnable = false,
                RtsEnable = false,
                DiscardNull = false,
                PortName = serialPortName
            };
            
            return new SerialPortProvider(logger, serialPort);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to create serial port: {0}", ex.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="SerialPortProvider"/> for traditional RS-232 hardware using DB9 with full
    /// RTS and DTR support.
    /// </summary>
    /// <inheritdoc cref="CreateUsbSerialProvider"/>
    public static ISerialProvider? CreateTtlSerialProvider(ILogger logger, string serialPortName)
    {
        try
        {
            var serialPort = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.Even,
                DataBits = 7,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 250,
                WriteTimeout = 250,
                WriteBufferSize = 1024,
                ReadBufferSize = 1024,
                DtrEnable = true,
                RtsEnable = true,
                DiscardNull = false,
                PortName = serialPortName
            };
            
            return new SerialPortProvider(logger, serialPort);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to create serial port: {0}", ex.Message);
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool TryOpen()
    {
        try
        {
            if (IsOpen)
            {
                Logger.LogInfo("Tried to open serial port {0}, but it is already open.", Port.PortName);
                return true;
            }

            Port.Open();
            if (!Port.IsOpen)
            {
                return false;
            }
            
            Port.DiscardInBuffer();
            Port.DiscardOutBuffer();

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Logger.LogError("Failed to open serial port {0} because it is already in use.", Port.PortName);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to open serial port {0}: {1}", Port.PortName, ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        Port.Close();
    }

    /// <inheritdoc />
    public byte[] Read(uint count)
    {
        if (count == 0)
        {
            throw new ArgumentException("Cannot read 0 bytes.", nameof(count));
        }

        if (!IsOpen)
        {
            Logger.LogError("Cannot read data while closed.");
            return [];
        }

        var payload = new List<byte>();
        uint index = 0;
        try
        {
            for (index = 0; index < count; index++)
            {
                payload.Add((byte)Port.ReadByte());
            }
        }
        catch (TimeoutException)
        {
            Logger.LogTrace("A read operation timed out at index {0}.", index);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to read from serial port {0}: {1}", Port.PortName, ex.Message);
            return [];
        }
        
        Logger.LogTrace("Received serial data: {0}", payload.ToArray().ConvertToHexString(true));
        return payload.ToArray();
    }

    /// <inheritdoc />
    public void Write(byte[] data)
    {
        if (!IsOpen)
        {
            Logger.LogError("Cannot write data while closed.");
            return;
        }
        
        try
        {
            Logger.LogTrace("Sent data to serial port: {0}", data.ConvertToHexString(true));
            Port.Write(data, 0, data.Length);
        }
        catch (TimeoutException)
        {
            Logger.LogTrace("A write operation timed out.");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to write to serial port {0}: {1}", Port.PortName, ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_isDisposed)
        {
            return;
        }

        Port.Close();
        Port.Dispose();
        _isDisposed = true;
    }
}