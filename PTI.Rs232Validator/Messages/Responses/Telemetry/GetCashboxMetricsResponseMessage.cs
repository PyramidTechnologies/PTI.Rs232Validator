using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Models;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetCashboxMetrics"/>.
/// </summary>
internal class GetCashboxMetricsResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 53;
    
    /// <summary>
    /// Initializes a new instance of <see cref="GetCashboxMetricsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetCashboxMetricsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        CashboxMetrics = new CashboxMetrics
        {
            CashboxRemovedCount = data[..8].ConvertToUint32Via4BitEncoding(),
            CashboxFullCount = data[8..16].ConvertToUint32Via4BitEncoding(),
            BillsStackedSinceCashboxRemoved = data[16..24].ConvertToUint32Via4BitEncoding(),
            BillsStackedSincePowerUp = data[24..32].ConvertToUint32Via4BitEncoding(),
            AverageTimeToStack = data[32..40].ConvertToUint32Via4BitEncoding(),
            TotalBillsStacked = data[40..48].ConvertToUint32Via4BitEncoding()
        };
    }
    
    /// <inheritdoc cref="Models.CashboxMetrics"/>
    public CashboxMetrics CashboxMetrics { get; } = new();
}