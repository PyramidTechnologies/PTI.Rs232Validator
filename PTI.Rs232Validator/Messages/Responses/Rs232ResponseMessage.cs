using System.Collections.Generic;

namespace PTI.Rs232Validator.Messages.Responses;

/// <summary>
/// An RS-232 message from an acceptor to a host.
/// </summary>
internal abstract class Rs232ResponseMessage : Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="Rs232ResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    protected Rs232ResponseMessage(IReadOnlyList<byte> payload)
    {
        Payload = payload;
        
        if (payload.Count == 0)
        {
            PayloadIssues.Add("The payload is empty.");
            return;
        }

        if (payload.Count < MinPayloadByteSize)
        {
            PayloadIssues.Add(
                $"The payload size is {payload.Count} bytes, but at least {MinPayloadByteSize} bytes are expected.");
            return;
        }

        if (payload[0] != Stx)
        {
            PayloadIssues.Add($"The payload starts with {payload[0]:X2}, but {Stx:X2} is expected.");
        }

        if (payload[1] != payload.Count)
        {
            PayloadIssues.Add(
                $"The payload size is {payload.Count} bytes, but the payload reported a size of {payload[1]} bytes.");
        }

        if (MessageType == Rs232MessageType.HostToAcceptor)
        {
            PayloadIssues.Add($"The message type is {MessageType}, which should never occur.");
        }

        if (payload[^2] != Etx)
        {
            PayloadIssues.Add($"The payload ends with {payload[^2]:X2}, but {Etx:X2} is expected.");
        }

        var actualChecksum = payload[^1];
        var expectedChecksum = CalculateChecksum(payload);
        if (actualChecksum != expectedChecksum)
        {
            PayloadIssues.Add(
                $"The payload has a checksum of {actualChecksum:X2}, but {expectedChecksum:X2} is expected.");
        }
    }
    
    /// <inheritdoc/>
    public override IReadOnlyList<byte> Payload { get; }
    
    /// <summary>
    /// Is this instance valid (i.e. are there no issues with <see cref="Rs232Message.Payload"/>)?
    /// </summary>
    public bool IsValid => PayloadIssues.Count == 0;

    /// <summary>
    /// A collection of issues with <see cref="Rs232Message.Payload"/>.
    /// </summary>
    protected List<string> PayloadIssues { get; } = [];

    /// <summary>
    /// Gets the issues with <see cref="Rs232Message.Payload"/>.
    /// </summary>
    public IReadOnlyList<string> GetPayloadIssues()
    {
        return PayloadIssues.AsReadOnly();
    }
}