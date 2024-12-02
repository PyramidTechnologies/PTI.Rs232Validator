using PTI.Rs232Validator.Messages.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetSerialNumber"/>.
/// </summary>
public class GetSerialNumberResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 14;

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

        var serialNumber = Encoding.ASCII.GetString(Data.ToArray()).Trim('\0');
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
    /// The serial number of an acceptor.
    /// </summary>
    /// <remarks>If the string is empty, then the acceptor was not assigned a serial number.</remarks>
    public string SerialNumber { get; } = "";

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsValid ? $"Serial Number: {SerialNumber}" : base.ToString();
    }
}