using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An implementation of <see cref="Rs232RequestMessage"/> for <see cref="TelemetryCommand"/>.
/// </summary>
public class TelemetryRequestMessage : Rs232RequestMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="TelemetryRequestMessage"/>.
    /// </summary>
    /// <param name="ack"><see cref="Rs232Message.Ack"/></param>
    /// <param name="command">An enumerator of <see cref="TelemetryCommand"/>.</param>
    /// <param name="data">The data.</param>
    public TelemetryRequestMessage(bool ack, TelemetryCommand command, IReadOnlyList<byte> data)
        : base(BuildPayload(ack, command, data))
    {
        Command = command;
    }

    private TelemetryCommand Command { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return base.ToString() + $" | {nameof(Command).AddSpacesToCamelCase()}: {Command}";
    }

    private static ReadOnlyCollection<byte> BuildPayload(
        bool ack,
        TelemetryCommand command,
        IReadOnlyList<byte> data)
    {
        var payload = new List<byte>
        {
            Stx,
            0,
            (byte)((byte)Rs232MessageType.TelemetryCommand | (ack ? 1 : 0)),
            (byte)command
        };

        payload.AddRange(data);
        payload.Add(Etx);
        payload.Add(0);
        payload[1] = (byte)payload.Count;

        return payload.AsReadOnly();
    }
}