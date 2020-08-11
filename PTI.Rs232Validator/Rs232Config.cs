namespace PTI.Rs232Validator
{
    using System;
    using Providers;

    public class Rs232Config
    {
        /// <summary>
        ///     Default to accepting all notes
        /// </summary>
        public const byte DefaultEnableMask = 0x7F;

        /// <summary>
        ///     Default period between polling message sent from host to device
        /// </summary>
        public static readonly TimeSpan DefaultPollingPeriod = TimeSpan.FromMilliseconds(100);

        /// <summary>
        ///     Create a new configuration using a custom serial provider
        /// </summary>
        /// <param name="provider">serial provider implementation</param>
        /// <param name="logger">Optional system logger</param>
        public Rs232Config(ISerialProvider provider, ILogger logger = null)
        {
            SerialProvider = provider;
            Logger = logger ?? new NullLogger();
        }

        /// <summary>
        ///     A bit mask representing which bills to accept
        ///     0b00000001: $1 or first note
        ///     0b00000010: $2 or second note
        ///     0b00000100: $5 or third note
        ///     0b00001000: $10 or fourth note
        ///     0b00010000: $20 or fifth note
        ///     0b00100000: $50 or sixth note
        ///     0b01000000: $100 of seventh note
        /// </summary>
        public byte EnableMask { get; set; } = DefaultEnableMask;

        /// <summary>
        ///     Escrow mode allows you to manually stack or return a node
        ///     based on the credit value reported by the device.
        ///     When this mode is enabled, you must manually call
        ///     the Stack and Return functions. Otherwise, the device
        ///     will perform the stacking and returning automatically
        ///     based on the validation of the bill.
        /// </summary>
        public bool IsEscrowMode { get; set; } = false;
        
        /// <summary>
        ///     This protocol reports the cash box state for every polling message.
        ///     This may overwhelm your logs so we will reports the event only
        ///     once by default. To receive notifications for all cash box
        ///     removal messages, set this flag to true.
        /// </summary>
        public bool ReportAllCashBoxRemovalEvents { get; set; } = false;

        /// <summary>
        ///     Optionally provide your own serial port or mock implementation
        /// </summary>
        public ISerialProvider SerialProvider { get; }

        /// <summary>
        ///     Time period between messages sent from host to device
        /// </summary>
        public TimeSpan PollingPeriod { get; set; } = DefaultPollingPeriod;

        /// <summary>
        ///     Automatic logger
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        ///     Create a new config using a USB serial port configuration
        /// </summary>
        /// <param name="portName">OS port name to use for bill validator connection</param>
        /// <param name="logger">Optional system logger</param>
        public static Rs232Config UsbRs232Config(string portName, ILogger logger = null)
        {
            return new Rs232Config(new UsbSerialProvider(portName), logger);
        }

        /// <summary>
        ///     Create a new config using a TTL (DB9) serial port configuration
        /// </summary>
        /// <param name="portName">OS port name to use for bill validator connection</param>
        /// <param name="logger">Optional system logger</param>
        public static Rs232Config TtlRs232Config(string portName, ILogger logger = null)
        {
            return new Rs232Config(new TtlSerialProvider(portName), logger);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return
                $"EnableMask: {EnableMask:X8}, PollingPeriod: {PollingPeriod}, EscrowMode: {IsEscrowMode}";
        }
    }
}