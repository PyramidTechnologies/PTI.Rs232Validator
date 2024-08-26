using PTI.Rs232Validator.Loggers;

namespace PTI.Rs232Validator.Providers
{
    using System;
    using System.IO.Ports;

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
        protected BaseSerialPortProvider(string portName)
        {
            _portName = portName;
        }

        /// <summary>
        ///     Native serial port handle
        /// </summary>
        protected abstract SerialPort Port { get; }

        /// <inheritdoc />
        public bool IsOpen => Port?.IsOpen ?? false;

        /// <inheritdoc />
        public bool TryOpen()
        {
            try
            {
                if (IsOpen)
                {
                    Logger?.Info("{0} Port {1} is already open", GetType().Name, _portName);
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
                Logger?.Error("{0} Failed to open port {1} because it some other process or instance has it open",
                    GetType().Name, _portName);
                return false;
            }
            catch (Exception ex)
            {
                Logger?.Error("{0} Failed to open port {1}: {2}", ex.Message, GetType().Name, _portName);
                return false;
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            Port.Close();
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
                Logger?.Error("{0} Cannot read from port that is not open. Try opening it.", GetType().Name);
                return default;
            }

            try
            {
                // Read one byte at a time to avoid timeout issues
                var receive = new byte[count];
                for (var i = 0; i < count; ++i)
                {
                    receive[i] = (byte) Port.ReadByte();
                }

                Logger?.Trace("{0}<< {1}", GetType().Name, receive.ToHexString());

                return receive;
            }
            catch (TimeoutException)
            {
                Logger?.Trace(
                    "{0} A read operation timed out. This is expected behavior while the device is feeding or stacking a bill",
                    GetType().Name);
                return default;
            }
            catch (Exception ex)
            {
                Logger?.Error("{0} Failed to read port: {1}{2}{3}", GetType().Name, ex.Message, Environment.NewLine,
                    ex.StackTrace);
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
                Logger?.Trace("{0}>> {1}", GetType().Name, data.ToHexString());

                Port.Write(data, 0, data.Length);
            }
            catch (TimeoutException)
            {
                Logger?.Trace(
                    "{0} A write operation timed out. This is expected behavior while the device is feeding or stacking a bill",
                    GetType().Name);
            }
            catch (Exception ex)
            {
                Logger?.Error("{0} Failed to write port: {1}{2}{3}", GetType().Name, ex.Message, Environment.NewLine,
                    ex.StackTrace);
            }
        }

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Port?.Close();
            Port?.Dispose();

            Logger?.Trace("{0} disposed", GetType().Name);
        }
    }
}