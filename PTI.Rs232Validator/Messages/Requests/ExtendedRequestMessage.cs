using PTI.Rs232Validator.Messages.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An implementation of <see cref="Rs232RequestMessage"/> for <see cref="ExtendedCommand"/>.
/// </summary>
internal class ExtendedRequestMessage : Rs232RequestMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExtendedRequestMessage"/>.
    /// </summary>
    /// <param name="ack"><see cref="Rs232Message.Ack"/></param>
    /// <param name="command">An enumerator of <see cref="ExtendedCommand"/>.</param>
    /// <param name="data">The data.</param>
    public ExtendedRequestMessage(bool ack, ExtendedCommand command, IReadOnlyList<byte> data) : base(BuildPayload(ack, command, data))
    {
    }
    
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