using PTI.Rs232Validator.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetCashBoxMetrics"/>.
/// </summary>
internal class GetCashBoxMetricsResponseMessage : TelemetryResponseMessage
{
    /// <summary>
    /// The payload size in bytes.
    /// </summary>
    public const byte PayloadByteSize = 53;
    
    /// <summary>
    /// Initializes a new instance of <see cref="GetCashBoxMetricsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetCashBoxMetricsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        CashBoxRemovedCount = data[..8].ConvertToUint32Via4BitEncoding();
        CashBoxFullCount = data[8..16].ConvertToUint32Via4BitEncoding();
        BillsStackedSinceCashBoxRemoved = data[16..24].ConvertToUint32Via4BitEncoding();
        BillsStackedSincePowerUp = data[24..32].ConvertToUint32Via4BitEncoding();
        AverageTimeToStack = data[32..40].ConvertToUint32Via4BitEncoding();
        TotalBillsStacked = data[40..48].ConvertToUint32Via4BitEncoding();
    }

    public ulong CashBoxRemovedCount { get; } = 0;

    public ulong CashBoxFullCount { get; } = 0;

    public ulong BillsStackedSinceCashBoxRemoved { get; } = 0;

    public ulong BillsStackedSincePowerUp { get; } = 0;

    public ulong AverageTimeToStack { get; } = 0;

    public ulong TotalBillsStacked { get; } = 0;
}