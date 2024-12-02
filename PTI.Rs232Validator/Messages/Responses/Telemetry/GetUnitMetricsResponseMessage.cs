using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetUnitMetrics"/>.
/// </summary>
public class GetUnitMetricsResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 69;

    /// <summary>
    /// Initializes a new instance of <see cref="GetUnitMetricsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetUnitMetricsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        TotalValueStacked = data[..8].ConvertToUint32Via4BitEncoding();
        TotalDistanceMoved = data[8..16].ConvertToUint32Via4BitEncoding();
        PowerUpCount = data[16..24].ConvertToUint32Via4BitEncoding();
        PushButtonCount = data[24..32].ConvertToUint32Via4BitEncoding();
        ConfigurationCount = data[32..40].ConvertToUint32Via4BitEncoding();
        UsbEnumerationsCount = data[40..48].ConvertToUint32Via4BitEncoding();
        TotalCheatAttemptsDetected = data[48..56].ConvertToUint32Via4BitEncoding();
        TotalSecurityLockupCount = data[56..64].ConvertToUint32Via4BitEncoding();
    }

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

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(TotalValueStacked).AddSpacesToCamelCase()}: {TotalValueStacked} | " +
              $"{nameof(TotalDistanceMoved).AddSpacesToCamelCase()}: {TotalDistanceMoved} | " +
              $"{nameof(PowerUpCount).AddSpacesToCamelCase()}: {PowerUpCount} | " +
              $"{nameof(PushButtonCount).AddSpacesToCamelCase()}: {PushButtonCount} | " +
              $"{nameof(ConfigurationCount).AddSpacesToCamelCase()}: {ConfigurationCount} | " +
              $"{nameof(UsbEnumerationsCount).AddSpacesToCamelCase()}: {UsbEnumerationsCount} | " +
              $"{nameof(TotalCheatAttemptsDetected).AddSpacesToCamelCase()}: {TotalCheatAttemptsDetected} | " +
              $"{nameof(TotalSecurityLockupCount).AddSpacesToCamelCase()}: {TotalSecurityLockupCount}"
            : base.ToString();
    }
}