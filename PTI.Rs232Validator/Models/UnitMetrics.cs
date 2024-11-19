namespace PTI.Rs232Validator.Models;

/// <summary>
/// The metrics about an acceptor unit.
/// </summary>
public class UnitMetrics
{
    /// <summary>
    /// The total value of currency accepted for the lifetime of the acceptor.
    /// </summary>
    public uint TotalValueStacked { get; init; }

    /// <summary>
    /// The total distance the acceptance motor has moved in mm.
    /// </summary>
    public uint TotalDistanceMoved { get; init; }

    /// <summary>
    /// The total times the acceptor has powered on.
    /// </summary>
    public uint PowerUpCount { get; init; }

    /// <summary>
    /// The total times the diagnostics push button has been pressed.
    /// </summary>
    public uint PushButtonCount { get; init; }

    /// <summary>
    /// The total times the acceptor has been re-configured.
    /// </summary>
    public uint ConfigurationCount { get; init; }

    /// <summary>
    /// The total times the acceptor has had the USB device port plugged in.
    /// </summary>
    public uint UsbEnumerationsCount { get; init; }

    /// <summary>
    /// The total times the acceptor has detected a cheat attempt.
    /// </summary>
    public uint TotalCheatAttemptsDetected { get; init; }

    /// <summary>
    /// The total times the acceptor went into a security lockup due to cheat attempts.
    /// </summary>
    public uint TotalSecurityLockupCount { get; init; }
}