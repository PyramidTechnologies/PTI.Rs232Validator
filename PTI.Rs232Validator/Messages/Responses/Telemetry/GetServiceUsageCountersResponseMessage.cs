using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetServiceUsageCounters"/>.
/// </summary>
public class GetServiceUsageCountersResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 53;

    /// <summary>
    /// Initializes a new instance of <see cref="GetServiceUsageCountersResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetServiceUsageCountersResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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

        var data = Data.ToArray();
        DistancedMovedSinceLastTachSensorService = data[..8].ConvertToUint32Via4BitEncoding();
        DistanceMovedSinceLastBillPathService = data[8..16].ConvertToUint32Via4BitEncoding();
        DistancedMoveSinceLastBeltService = data[16..24].ConvertToUint32Via4BitEncoding();
        BillsStackedSinceLastCashboxService = data[24..32].ConvertToUint32Via4BitEncoding();
        DistanceMovedSinceLastMasService = data[32..40].ConvertToUint32Via4BitEncoding();
        DistanceMovedSinceLastSpringRollerService = data[40..48].ConvertToUint32Via4BitEncoding();
    }

    /// <summary>
    /// The total amount of movement since the last tach sensor service in mm.
    /// </summary>
    public uint DistancedMovedSinceLastTachSensorService { get; init; }

    /// <summary>
    /// The total amount of movement since the last bill path service in mm.
    /// </summary>
    public uint DistanceMovedSinceLastBillPathService { get; init; }

    /// <summary>
    /// The total amount of movement since the last belt service in mm.
    /// </summary>
    public uint DistancedMoveSinceLastBeltService { get; init; }

    /// <summary>
    /// The total amount of bills stacked since the last cashbox mechanism service in mm.
    /// </summary>
    public uint BillsStackedSinceLastCashboxService { get; init; }

    /// <summary>
    /// The total amount of movement since the last mechanical anti-stringing lever (MAS) service in mm.
    /// </summary>
    public uint DistanceMovedSinceLastMasService { get; init; }

    /// <summary>
    /// The total amount of movement since the last spring roller service in mm.
    /// </summary>
    public uint DistanceMovedSinceLastSpringRollerService { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(DistancedMovedSinceLastTachSensorService).AddSpacesToCamelCase()}: {DistancedMovedSinceLastTachSensorService} | " +
              $"{nameof(DistanceMovedSinceLastBillPathService).AddSpacesToCamelCase()}: {DistanceMovedSinceLastBillPathService} | " +
              $"{nameof(DistancedMoveSinceLastBeltService).AddSpacesToCamelCase()}: {DistancedMoveSinceLastBeltService} | " +
              $"{nameof(BillsStackedSinceLastCashboxService).AddSpacesToCamelCase()}: {BillsStackedSinceLastCashboxService} | " +
              $"{nameof(DistanceMovedSinceLastMasService).AddSpacesToCamelCase()}: {DistanceMovedSinceLastMasService} | " +
              $"{nameof(DistanceMovedSinceLastSpringRollerService).AddSpacesToCamelCase()}: {DistanceMovedSinceLastSpringRollerService}"
            : base.ToString();
    }
}