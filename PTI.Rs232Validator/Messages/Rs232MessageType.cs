namespace PTI.Rs232Validator.Messages;

/// <summary>
/// RS-232 message types.
/// </summary>
internal enum Rs232MessageType : byte
{
    /// <summary>
    /// Poll message from host to acceptor.
    /// </summary>
    HostToAcceptor = 0x10,
    
    /// <summary>
    /// Message from acceptor to host.
    /// </summary>
    AcceptorToHost = 0x20,
    
    /// <summary>
    /// Extended command message from host to acceptor.
    /// </summary>
    ExtendedCommand = 0x70
}