namespace PTI.Rs232Validator
{
    using System;
    using System.IO.Ports;
    using Internal;

    /// <summary>
    ///     Default RS232 serial port configuration
    /// </summary>
    public class DefaultSerialPortProvider : ISerialProvider
    {
        /// <summary>
        ///     Native serial port handle
        /// </summary>
        private readonly SerialPort _port;

        /// <summary>
        ///     Create a new hardware serial port provider
        /// </summary>
        /// <param name="portName">OS port name</param>
        public DefaultSerialPortProvider(string portName)
        {
            try
            {
                _port = new SerialPort
                {
                    BaudRate = 9600,
                    Parity = Parity.Even,
                    DataBits = 7,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 2000,
                    WriteTimeout = 2000,
                    WriteBufferSize = 1024,
                    ReadBufferSize = 1024,
                    DtrEnable = false,
                    RtsEnable = false,
                    DiscardNull = false,
                    PortName = portName
                };
            }
            catch (Exception ex)
            {
                Logger?.Error("Failed to create port: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
            }
        }

        /// <inheritdoc />
        public bool IsOpen => _port?.IsOpen ?? false;

        public bool TryOpen()
        {
            try
            {
                _port?.Open();
                var didOpen = _port?.IsOpen ?? false;

                if (didOpen)
                {
                    // On open, clear any pending reads or writes
                    _port.DiscardInBuffer();
                    _port.DiscardOutBuffer();
                }

                return didOpen;
            }
            catch (UnauthorizedAccessException)
            {
                Logger?.Error("Failed to open port because it some other process or instance has it open");
                return false;
            }
            catch (Exception ex)
            {
                Logger?.Error("Failed to open port: {0}", ex.Message);
                return false;
            }
        }

        public void Close()
        {
            _port.Close();
        }

        /// <inheritdoc />
        public byte[] Read(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException($"{count} must be greater than zero");
            }

            if (!IsOpen)
            {
                Logger?.Error("Cannot read from port that is not open. Try opening it.");
                return default;
            }

            try
            {
                // Read one byte at a time to avoid timeout issues
                var receive = new byte[count];
                for (var i = 0; i < count; ++i)
                {
                    receive[i] = (byte) _port.ReadByte();
                }

                Logger?.Trace("<< {0}", receive.ToHexString());

                return receive;
            }
            catch (Exception ex)
            {
                Logger?.Error("Failed to read port: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
                return default;
            }
        }

        /// <inheritdoc />
        public void Write(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                Logger?.Trace(">> {0}", data.ToHexString());

                _port.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Logger?.Error("Failed to write port: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
            }
        }

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            _port?.Close();
            _port?.Dispose();

            Logger?.Debug("DefaultSerialPortProvider disposed");
        }
    }
}