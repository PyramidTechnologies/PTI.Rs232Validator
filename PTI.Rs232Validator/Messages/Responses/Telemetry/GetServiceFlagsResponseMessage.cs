﻿using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 message from an acceptor to a host for <see cref="TelemetryCommand.GetServiceFlags"/>.
/// </summary>
public class GetServiceFlagsResponseMessage : TelemetryResponseMessage
{
    private const byte PayloadByteSize = 11;

    /// <summary>
    /// Initializes a new instance of <see cref="GetServiceFlagsResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    public GetServiceFlagsResponseMessage(IReadOnlyList<byte> payload) : base(payload)
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
        TachSensorServiceSuggestor = (ServiceSuggestor)data[0];
        BillPathServiceSuggestor = (ServiceSuggestor)data[1];
        CashboxBeltServiceSuggestor = (ServiceSuggestor)data[2];
        CashboxMechanismServiceSuggestor = (ServiceSuggestor)data[3];
        MasServiceSuggestor = (ServiceSuggestor)data[4];
        SpringRollersServiceSuggestor = (ServiceSuggestor)data[5];
    }

    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the tach sensor.
    /// </summary>
    public ServiceSuggestor TachSensorServiceSuggestor { get; init; }

    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the bill path.
    /// </summary>
    public ServiceSuggestor BillPathServiceSuggestor { get; init; }

    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the cashbox belt.
    /// </summary>
    public ServiceSuggestor CashboxBeltServiceSuggestor { get; init; }

    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the cashbox stacking mechanism.
    /// </summary>
    public ServiceSuggestor CashboxMechanismServiceSuggestor { get; init; }

    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the mechanical anti-stringing lever (MAS).
    /// </summary>
    public ServiceSuggestor MasServiceSuggestor { get; init; }

    /// <summary>
    /// An enumerator of <see cref="ServiceSuggestor"/> for the spring rollers.
    /// </summary>
    public ServiceSuggestor SpringRollersServiceSuggestor { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsValid
            ? $"{nameof(TachSensorServiceSuggestor).AddSpacesToCamelCase()}: {TachSensorServiceSuggestor} | " +
              $"{nameof(BillPathServiceSuggestor).AddSpacesToCamelCase()}: {BillPathServiceSuggestor} | " +
              $"{nameof(CashboxBeltServiceSuggestor).AddSpacesToCamelCase()}: {CashboxBeltServiceSuggestor} | " +
              $"{nameof(CashboxMechanismServiceSuggestor).AddSpacesToCamelCase()}: {CashboxMechanismServiceSuggestor} | " +
              $"{nameof(MasServiceSuggestor).AddSpacesToCamelCase()}: {MasServiceSuggestor} | " +
              $"{nameof(SpringRollersServiceSuggestor).AddSpacesToCamelCase()}: {SpringRollersServiceSuggestor}"
            : base.ToString();
    }

    /// <summary>
    /// The entities that suggest a component requires service.
    /// </summary>
    [Flags]
    public enum ServiceSuggestor : byte
    {
        /// <summary>
        /// No entity suggests that a component requires service.
        /// </summary>
        None = 0,

        /// <summary>
        /// The usage metrics suggest that a component requires service.
        /// </summary>
        UsageMetrics = 1 << 0,

        /// <summary>
        /// The diagnostics and errors of the system suggest that a component requires service.
        /// </summary>
        DiagnosticsAndError = 1 << 1
    }
}