using System;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Extended;

/// <summary>
/// An RS-232 extended message from an acceptor to a host.
/// </summary>
internal class ExtendedResponseMessage : PollResponseMessage
{
    /// <summary>
    /// The minimum payload size in bytes.
    /// </summary>
    public new const byte MinPayloadByteSize = 12;
    
    /// <summary>
    /// Initializes a new instance of <see cref="ExtendedResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public ExtendedResponseMessage(IReadOnlyList<byte> payload) : base(payload, GetStatus(payload))
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }
        
        if (payload.Count < MinPayloadByteSize)
        {
            PayloadIssues.Add(
                $"The payload size is {payload.Count} bytes, but at least {MinPayloadByteSize} bytes are expected.");
            return;
        }

        if (MessageType != Rs232MessageType.ExtendedCommand)
        {
            PayloadIssues.Add(
                $"The message type is {MessageType}, but {Rs232MessageType.ExtendedCommand} is expected.");
            return;
        }
        
        Command = (ExtendedCommand)payload[3];
        
        Data = payload
            .Skip(10)
            .Take(payload.Count - MinPayloadByteSize)
            .ToList()
            .AsReadOnly();
    }
    
    /// <summary>
    /// An enumerator of <see cref="ExtendedCommand"/>.
    /// </summary>
    public ExtendedCommand Command { get; }
    
    /// <summary>
    /// The data.
    /// </summary>
    protected IReadOnlyList<byte> Data { get; } = [];

    private static IReadOnlyList<byte> GetStatus(IReadOnlyList<byte> payload)
    {
        if (payload.Count < MinPayloadByteSize)
        {
            return Array.Empty<byte>();
        }

        return payload
            .Skip(4)
            .Take(StatusByteSize)
            .ToList()
            .AsReadOnly();
    }
}