using PTI.Rs232Validator.Utility;

namespace PTI.Rs232Validator.Models;

/// <summary>
/// The metrics about the cashbox
/// </summary>
public class CashboxMetrics
{
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
        return
            $"{nameof(CashboxRemovedCount).AddSpacesToCamelCase()}: {CashboxRemovedCount} | " +
            $"{nameof(CashboxFullCount).AddSpacesToCamelCase()}: {CashboxFullCount} | " +
            $"{nameof(BillsStackedSinceCashboxRemoved).AddSpacesToCamelCase()}: {BillsStackedSinceCashboxRemoved} | " +
            $"{nameof(BillsStackedSincePowerUp).AddSpacesToCamelCase()}: {BillsStackedSincePowerUp} | " +
            $"{nameof(AverageTimeToStack).AddSpacesToCamelCase()}: {AverageTimeToStack} | " +
            $"{nameof(TotalBillsStacked).AddSpacesToCamelCase()}: {TotalBillsStacked}";
    }
}