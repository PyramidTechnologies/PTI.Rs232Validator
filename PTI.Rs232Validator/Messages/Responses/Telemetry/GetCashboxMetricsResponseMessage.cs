using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetCashboxMetrics"/>.
/// </summary>
public class GetCashboxMetricsResponseMessage : TelemetryResponseMessage
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
        CashboxRemovedCount = data[..8].ConvertToUint32Via4BitEncoding();
        CashboxFullCount = data[8..16].ConvertToUint32Via4BitEncoding();
        BillsStackedSinceCashboxRemoved = data[16..24].ConvertToUint32Via4BitEncoding();
        BillsStackedSincePowerUp = data[24..32].ConvertToUint32Via4BitEncoding();
        AverageTimeToStack = data[32..40].ConvertToUint32Via4BitEncoding();
        TotalBillsStacked = data[40..48].ConvertToUint32Via4BitEncoding();
    }

    /// <summary>
    /// The number of times the cashbox has been removed.
    /// </summary>
    public uint CashboxRemovedCount { get; init; }

    /// <summary>
    /// The number of times the cashbox has been full.
    /// </summary>
    public uint CashboxFullCount { get; init; }

    /// <summary>
    /// The count of bills stacked since the cashbox was last removed.
    /// </summary>
    public uint BillsStackedSinceCashboxRemoved { get; init; }

    /// <summary>
    /// The count of bills stacked since the unit has been powered.
    /// </summary>
    public uint BillsStackedSincePowerUp { get; init; }

    /// <summary>
    /// The average time, in milliseconds, it takes to stack a bill.
    /// </summary>
    public uint AverageTimeToStack { get; init; }

    /// <summary>
    /// The total number of bills put in the cashbox for the lifetime of the unit.
    /// </summary>
    public uint TotalBillsStacked { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(CashboxRemovedCount).AddSpacesToCamelCase()}: {CashboxRemovedCount} | " +
              $"{nameof(CashboxFullCount).AddSpacesToCamelCase()}: {CashboxFullCount} | " +
              $"{nameof(BillsStackedSinceCashboxRemoved).AddSpacesToCamelCase()}: {BillsStackedSinceCashboxRemoved} | " +
              $"{nameof(BillsStackedSincePowerUp).AddSpacesToCamelCase()}: {BillsStackedSincePowerUp} | " +
              $"{nameof(AverageTimeToStack).AddSpacesToCamelCase()}: {AverageTimeToStack} | " +
              $"{nameof(TotalBillsStacked).AddSpacesToCamelCase()}: {TotalBillsStacked}"
            : base.ToString();
    }
}