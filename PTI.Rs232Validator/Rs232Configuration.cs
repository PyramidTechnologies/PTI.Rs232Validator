using System;

namespace PTI.Rs232Validator;

/// <summary>
/// The configuration for communicating with an RS-232 bill acceptor.
/// </summary>
public class Rs232Configuration
{
    /// <summary>
    /// The acceptance mask, which represents types of bills to accept.
    /// </summary>
    /// <remarks>
    /// 0b00000001: only accept the 1st bill type (e.g. $1).
    /// 0b00000010: only accept the 2nd bill type (e.g. $2).
    /// 0b00000100: only accept the 3rd bill type (e.g. $5).
    /// 0b00001000: only accept the 4th bill type (e.g. $10).
    /// 0b00010000: only accept the 5th bill type (e.g. $20).
    /// 0b00100000: only accept the 6th bill type (e.g. $50).
    /// 0b01000000: only accept the 7th bill type (e.g. $100).
    /// </remarks>
    public byte AcceptanceMask { get; set; } = 0x7F;

    /// <summary>
    /// Should the acceptor escrow each bill?
    /// </summary>
    /// <remarks>
    /// Setting this to true will cause the acceptor to place each bill in escrow and wait for the host to stack or return it.
    /// Setting this to false will cause the acceptor to automatically stack or return each bill.
    /// </remarks>
    public bool ShouldEscrow { get; set; }
    
    /// <summary>
    /// Should the acceptor detect barcodes?
    /// </summary>
    public bool ShouldDetectBarcodes { get; set; }

    /// <summary>
    /// The time period between messages sent from the host to the acceptor.
    /// </summary>
    public TimeSpan PollingPeriod { get; set; } = TimeSpan.FromMilliseconds(100);
}