namespace PTI.Rs232Validator.Messages;

/// <summary>
/// TODO: Add description.
/// </summary>
public enum Rs232MessageType : byte
{
    HostToAcceptor = 1,
    AcceptorToHost = 2,
    ExtendedCommand = 7,
    Reserved = 0
}