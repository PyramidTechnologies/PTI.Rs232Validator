namespace PTI.Rs232Validator.Messages;

/// <summary>
/// The RS-232 message types.
/// </summary>
internal enum Rs232MessageType : byte
{
    /// <summary>
    /// An unknown message type.
    /// </summary>
    Unknown = 0x00,
    
    /// <summary>
    /// A poll message from a host to an acceptor.
    /// </summary>
    HostToAcceptor = 0x10,
    
    /// <summary>
    /// A poll message from an acceptor to a host.
    /// </summary>
    AcceptorToHost = 0x20,
    
    /// <summary>
    /// A telemetry command message.
    /// </summary>
    TelemetryCommand = 0x60,
    
    /// <summary>
    /// An extended command message.
    /// </summary>
    ExtendedCommand = 0x70
}