using PTI.Rs232Validator.Models;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetUnitMetrics"/>.
/// </summary>
internal class GetUnitMetricsResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 69;

    /// <summary>
    /// Initializes a new instance of <see cref="GetUnitMetricsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetUnitMetricsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (Payload.Count > 0)
        {
            return;
        }
        
        if (payload.Count != PayloadByteSize)
        {
            PayloadIssues.Add($"The payload size is {payload.Count} bytes, but {PayloadByteSize} bytes are expected.");
            return;
        }

        var data = Data.ToArray();
        UnitMetrics = new UnitMetrics
        {
            TotalValueStacked = data[..8].ConvertToUint32Via4BitEncoding(),
            TotalDistanceMoved = data[8..16].ConvertToUint32Via4BitEncoding(),
            PowerUpCount = data[16..24].ConvertToUint32Via4BitEncoding(),
            PushButtonCount = data[24..32].ConvertToUint32Via4BitEncoding(),
            ConfigurationCount = data[32..40].ConvertToUint32Via4BitEncoding(),
            UsbEnumerationsCount = data[40..48].ConvertToUint32Via4BitEncoding(),
            TotalCheatAttemptsDetected = data[48..56].ConvertToUint32Via4BitEncoding(),
            TotalSecurityLockupCount = data[56..64].ConvertToUint32Via4BitEncoding()
        };
    }

    /// <inheritdoc cref="Models.UnitMetrics"/>
    public UnitMetrics UnitMetrics { get; } = new();
}