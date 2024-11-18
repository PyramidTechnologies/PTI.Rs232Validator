using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An RS-232 telemetry message from a host to an acceptor.
/// </summary>
internal class TelemetryRequestMessage : Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="TelemetryRequestMessage"/>.
    /// </summary>
    /// <param name="isAckNumberOne"><see cref="Rs232Message.Ack"/></param>
    /// <param name="command"><see cref="Command"/>.</param>
    /// <param name="data"><see cref="Data"/>.</param>
    public TelemetryRequestMessage(bool isAckNumberOne, TelemetryCommand command, IReadOnlyList<byte> data)
        : base(BuildPayload(isAckNumberOne, command, data))
    {
        Command = command;
        Data = data;
        PayloadSource[^1] = CalculateChecksum();
    }

    /// <summary>
    /// An enumerator of <see cref="TelemetryCommand"/>.
    /// </summary>
    public TelemetryCommand Command { get; }

    /// <summary>
    /// The data.
    /// </summary>
    public IReadOnlyList<byte> Data { get; }

    private static ReadOnlyCollection<byte> BuildPayload(
        bool isAckNumberOne,
        TelemetryCommand command,
        IReadOnlyList<byte> data)
    {
        var payload = new List<byte>
        {
            Stx,
            0,
            (byte)((byte)Rs232MessageType.TelemetryCommand | (isAckNumberOne ? 1 : 0)),
            (byte)command
        };

        payload.AddRange(data);
        payload.Add(Etx);
        payload.Add(0);
        payload[1] = (byte)payload.Count;

        return payload.AsReadOnly();
    }
}