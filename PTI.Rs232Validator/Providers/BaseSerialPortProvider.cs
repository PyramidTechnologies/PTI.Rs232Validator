namespace PTI.Rs232Validator.Providers
{
    using System;
    using System.IO.Ports;

    /// <summary>
    ///     Default RS232 serial port configuration
    /// </summary>
    public abstract class BaseSerialPortProvider : ISerialProvider
    {
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
                    Logger?.Info("Port is already open");
                    return true;
                }

                Port?.Open();
                var didOpen = Port?.IsOpen ?? false;

                if (didOpen)
                {
                    // On open, clear any pending reads or writes
                    Port.DiscardInBuffer();
                    Port.DiscardOutBuffer();
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
                Logger?.Error("Cannot read from port that is not open. Try opening it.");
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

                Port.Write(data, 0, data.Length);
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
            Port?.Close();
            Port?.Dispose();

            Logger?.Debug("DefaultSerialPortProvider disposed");
        }
    }
}