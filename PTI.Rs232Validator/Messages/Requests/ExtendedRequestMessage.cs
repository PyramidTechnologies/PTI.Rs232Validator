using System.Collections.Generic;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// RS-232 extended command message from host to acceptor.
/// </summary>
internal class ExtendedRequestMessage : Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExtendedRequestMessage"/>.
    /// </summary>
    /// <param name="isAckNumberOne">True to set ACK number to 1; otherwise, false to set it to 0.</param>
    /// <param name="command"><see cref="Command"/>.</param>
    /// <param name="data"><see cref="Data"/>.</param>
    public ExtendedRequestMessage(bool isAckNumberOne, Rs232ExtendedCommand command, IReadOnlyList<byte> data)
        : base(BuildPayload(isAckNumberOne, command, data))
    {
        Command = command;
        Data = data;
        Payload[^1] = CalculateChecksum();
    }

    private static IReadOnlyList<byte> BuildPayload(
        bool isAckNumberOne,
        Rs232ExtendedCommand command,
        IReadOnlyList<byte> data)
    {
        var payload = new List<byte>
        {
            0x02, 0x00,
            isAckNumberOne ? (byte)0x11 : (byte)0x10,
            (byte)command
        };
        
        payload.AddRange(data);
        payload.Add(0x03);
        payload.Add(0x00);
        payload[1] = (byte)payload.Count;

        return payload.AsReadOnly();
    }
    
    /// <inheritdoc cref="Rs232ExtendedCommand"/>
    public Rs232ExtendedCommand Command { get; }
    
    /// <summary>
    /// Data fields.
    /// </summary>
    public IReadOnlyList<byte> Data { get; }
}