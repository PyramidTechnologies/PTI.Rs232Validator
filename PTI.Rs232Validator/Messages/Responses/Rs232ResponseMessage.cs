using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses;

/// <summary>
/// RS-232 message from acceptor to host.
/// </summary>
internal abstract class Rs232ResponseMessage : Rs232Message
{
    /// <summary>
    /// Initializes a new instance of <see cref="Rs232ResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    protected Rs232ResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (payload.Count == 0)
        {
            IsEmpty = true;
            PayloadIssues.Add("Payload is empty.");
            return;
        }
        
        if (payload.Count < 5)
        {
            PayloadIssues.Add("Payload length is less than 5 bytes.");
            return;
        }
        
        if (payload[1] != 0x20)
        {
            PayloadIssues.Add($"Payload starts with {payload[1]:X2}, but expected 0x20.");
            return;
        }
        
        if (payload[2] != payload.Count)
        {
            PayloadIssues.Add(
                $"Payload length is {payload.Count} bytes, but payload reported a length of {payload[2]} bytes.");
            return;
        }
        
        // TODO: Determine if AckNumber should be validated.
        
        if (MessageType != Rs232MessageType.AcceptorToHost)
        {
            PayloadIssues.Add($"Message type is {(byte)MessageType}, but expected {(byte)Rs232MessageType.AcceptorToHost}.");
            return;
        }
        
        if (payload[payload.Count - 2] != 0x03)
        {
            PayloadIssues.Add($"Payload ends with {payload[payload.Count - 2]:X2}, but expected 0x03.");
            return;
        }
        
        var actualChecksum = payload[payload.Count - 1];
        var expectedChecksum = CalculateChecksum();
        if (actualChecksum != expectedChecksum)
        {
            PayloadIssues.Add($"Payload checksum is {expectedChecksum:X2}, but expected {actualChecksum:X2}.");
        }
    }
    
    /// <summary>
    /// Is <see cref="Rs232Message.Payload"/> the correct length with a valid checksum,
    /// but contains issues.
    /// </summary>
    public abstract bool HasProtocolViolation { get; protected set; }
    
    /// <summary>
    /// Is <see cref="Rs232Message.Payload"/> empty?
    /// </summary>
    public bool IsEmpty { get; }

    /// <summary>
    /// Is the payload well-formed?
    /// </summary>
    public bool IsValid => PayloadIssues.Count != 0;
    
    /// <summary>
    /// Collection of issues with <see cref="Rs232Message.Payload"/>.
    /// </summary>
    protected List<string> PayloadIssues { get; } = [];
    
    /// <summary>
    /// Gets the issues with <see cref="Rs232Message.Payload"/>.
    /// </summary>
    public IEnumerable<string> GetPayloadIssues()
    {
        return PayloadIssues.AsEnumerable();
    }
}