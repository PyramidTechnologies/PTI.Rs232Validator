namespace PTI.Rs232Validator.Providers
{
    using System;
    using System.IO.Ports;

    /// <summary>
    ///     Traditional hardware RS-232 using DB9 with full
    ///     RTS and DTR support.
    /// </summary>
    public sealed class TtlSerialProvider : BaseSerialPortProvider
    {
        /// <summary>
        ///     Create a new serial port connection
        ///     This is for true DB9 serial ports.
        /// </summary>
        /// <param name="portName">OS name of port</param>
        public TtlSerialProvider(string portName) : base(portName)
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
                    ReadTimeout = 250,
                    WriteTimeout = 250,
                    WriteBufferSize = 1024,
                    ReadBufferSize = 1024,
                    DtrEnable = true,
                    RtsEnable = true,
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