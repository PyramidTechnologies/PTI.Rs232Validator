namespace PTI.Rs232Validator.Providers
{
    using System;
    using System.IO.Ports;

    /// <summary>
    ///     A serial port provider for USB serial emulators
    ///     This provider does not use an RTS or DTR and has a longer
    ///     read timeout.
    /// </summary>
    public sealed class UsbSerialProvider : BaseSerialPortProvider
    {
        /// <summary>
        ///     Create a new serial port connection
        ///     This is for USB serial port emulators
        /// </summary>
        /// <param name="portName">OS name of port</param>
        public UsbSerialProvider(string portName) : base(portName)
        {
            try
            {
                Port = new SerialPort
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
                    PortName = portName
                };
            }
            catch (Exception ex)
            {
                Logger?.Error("{0} Failed to create port: {1}{2}{3}", GetType().Name, ex.Message, Environment.NewLine,
                    ex.StackTrace);
            }
        }

        /// <inheritdoc />
        protected override SerialPort Port { get; }
    }
}