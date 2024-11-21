using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An RS-232 extended message from a host to an acceptor.
/// </summary>
internal class ExtendedRequestMessage : Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExtendedRequestMessage"/>.
    /// </summary>
    /// <param name="ack"><see cref="Rs232Message.Ack"/></param>
    /// <param name="command"><see cref="Command"/>.</param>
    /// <param name="data"><see cref="Data"/>.</param>
    public ExtendedRequestMessage(bool ack, ExtendedCommand command, IReadOnlyList<byte> data) : base(BuildPayload(ack, command, data))
    {
        Command = command;
        Data = data;
    }
    
    /// <summary>
    /// An enumerator of <see cref="ExtendedCommand"/>.
    /// </summary>
    public ExtendedCommand Command { get; }
    
    /// <summary>
    /// The data.
    /// </summary>
    public IReadOnlyList<byte> Data { get; }
    
    private static ReadOnlyCollection<byte> BuildPayload(
        bool ack,
        ExtendedCommand command,
        IReadOnlyList<byte> data)
    {
        var payload = new List<byte>
        {
            Stx,
            0,
            (byte)((byte)Rs232MessageType.ExtendedCommand | (ack ? 1 : 0)),
            (byte)command
        };

        payload.AddRange(data);
        payload.Add(Etx);
        payload.Add(0);
        payload[1] = (byte)payload.Count;

        return payload.AsReadOnly();
    }
}