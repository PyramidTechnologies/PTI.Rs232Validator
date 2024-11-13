﻿using System.Collections.Generic;
using System.Linq;

namespace PTI.Rs232Validator.Messages.Responses.Telemetry;

/// <summary>
/// An RS-232 telemetry message from an acceptor to a host.
/// </summary>
internal class TelemetryResponseMessage : Rs232ResponseMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="TelemetryResponseMessage"/>.
    /// </summary>
    /// <inheritdoc/>
    internal TelemetryResponseMessage(IReadOnlyList<byte> payload) : base(payload)
    {
        if (PayloadIssues.Count > 0)
        {
            return;
        }

        if (MessageType != Rs232MessageType.TelemetryCommand)
        {
            PayloadIssues.Add(
                $"The message type is {MessageType}, but {Rs232MessageType.TelemetryCommand} is expected.");
            return;
        }

        if (payload.Count < MinPayloadByteSize)
        {
            return;
        }

        Data = payload
            .Skip(3)
            .Take(payload.Count - 5)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// The data.
    /// </summary>
    protected IReadOnlyList<byte> Data { get; } = [];
}