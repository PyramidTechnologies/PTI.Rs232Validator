using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetSerialNumber"/>.
/// </summary>
internal class GetSerialNumberResponseMessage : TelemetryResponseMessage
{
    /// <summary>
    /// The payload size in bytes.
    /// </summary>
    public const byte PayloadByteSize = 14;
    
    /// <summary>
    /// Initializes a new instance of <see cref="GetSerialNumberResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    internal GetSerialNumberResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }
        
        if (payload.Count != PayloadByteSize)
        {
            PayloadIssues.Add($"The payload size is {payload.Count} bytes, but {PayloadByteSize} bytes are expected.");
            return;
        }

        var serialNumber = Encoding.ASCII.GetString(Data.ToArray());
        foreach (var c in serialNumber)
        {
            if (!char.IsDigit(c))
            {
                PayloadIssues.Add($"The data contains a non-digit character: {serialNumber}.");
                return;
            }
        }
        
        SerialNumber = serialNumber;
    }

    /// <summary>
    /// Serial number of an acceptor.
    /// </summary>
    public string SerialNumber { get; } = "";
}