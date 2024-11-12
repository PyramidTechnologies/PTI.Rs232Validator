using System.Collections.Generic;

namespace PTI.Rs232Validator.Messages.Requests;

/// <summary>
/// An RS-232 telemetry command message from a host to an acceptor.
/// </summary>
internal class TelemetryRequestMessage : Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="TelemetryRequestMessage"/>.
    /// </summary>
    /// <param name="isAckNumberOne"><see cref="Rs232Message.IsAckNumberOne"/></param>
    /// <param name="command"><see cref="Command"/>.</param>
    /// <param name="dataFields"><see cref="DataFields"/>.</param>
    public TelemetryRequestMessage(bool isAckNumberOne, Rs232TelemetryCommand command, IReadOnlyList<byte> dataFields)
        : base(BuildPayload(isAckNumberOne, command, dataFields))
    {
        Command = command;
        DataFields = dataFields;
        Payload[^1] = CalculateChecksum();
    }
    
    /// <summary>
    /// An enumerator of <see cref="Rs232TelemetryCommand"/>.
    /// </summary>
    public Rs232TelemetryCommand Command { get; }
    
    /// <summary>
    /// The data fields.
    /// </summary>
    public IReadOnlyList<byte> DataFields { get; }
    
    private static IReadOnlyList<byte> BuildPayload(
        bool isAckNumberOne,
        Rs232TelemetryCommand command,
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
}