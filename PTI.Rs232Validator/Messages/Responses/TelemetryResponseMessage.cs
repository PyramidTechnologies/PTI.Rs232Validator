using System.Collections.Generic;

namespace PTI.Rs232Validator.Messages.Responses;

using System.Linq;

/// <summary>
/// RS-232 extended command message from acceptor to host.
/// </summary>
internal class TelemetryResponseMessage : Rs232ResponseMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="TelemetryResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    internal TelemetryResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (payload.Count < 5)
        {
            return;
        }

        IsNakBitOne = (payload[2] & 0b10000000) == 0b10000000;
        Data = payload
            .Skip(4)
            .Take(payload.Count - 6)
            .ToList()
            .AsReadOnly();
    }
    
    /// <inheritdoc/>
    public override bool HasProtocolViolation { get; protected set; }
    
    /// <summary>
    /// Is the NAK bit set to 1?
    /// </summary>
    /// <remarks>
    /// A 1 indicates that the message was valid, but the command is unsupported or the data received was invalid.
    /// </remarks>
    public bool IsNakBitOne { get; }
    
    /// <summary>
    /// Data fields.
    /// </summary>
    public IReadOnlyList<byte> Data { get; } = [];
}