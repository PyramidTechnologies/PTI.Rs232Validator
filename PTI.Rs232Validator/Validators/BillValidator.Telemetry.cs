using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
using PTI.Rs232Validator.Messages.Responses.Telemetry;
using PTI.Rs232Validator.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Validators;

public partial class BillValidator
{
    private const byte MaxIncorrectPayloadPardons = 2;

    /// <summary>
    /// Pings the acceptor.
    /// </summary>
    /// <returns>True if communications are operational; otherwise, false.</returns>
    public async Task<bool> PingAsync()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.Ping, [],
            Rs232ResponseMessage.MinPayloadByteSize, payload => new TelemetryResponseMessage(payload));
        return responseMessage is not null;
    }

    /// <summary>
    /// Gets the 9-character serial number assigned to the acceptor.
    /// </summary>
    /// <returns>A 9-character string if successful; otherwise, an empty string.</returns>
    public async Task<string> GetSerialNumberAsync()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetSerialNumber, [],
            GetSerialNumberResponseMessage.PayloadByteSize, payload => new GetSerialNumberResponseMessage(payload));
        return responseMessage?.SerialNumber ?? string.Empty;
    }

    /// <summary>
    /// Gets the telemetry metrics about the cashbox.
    /// </summary>
    /// <returns>An instance of <see cref="CashboxMetrics"/> if successful; otherwise, null.</returns>
    public async Task<CashboxMetrics?> GetCashboxMetrics()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetCashboxMetrics, [],
            GetCashboxMetricsResponseMessage.PayloadByteSize, payload => new GetCashboxMetricsResponseMessage(payload));
        return responseMessage?.CashboxMetrics;
    }

    /// <summary>
    /// Clears the count of bills in the cashbox.
    /// </summary>
    /// <returns>True if successful; otherwise, false.</returns>
    public async Task<bool> ClearCashboxCount()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.ClearCashboxCount, [],
            Rs232ResponseMessage.MinPayloadByteSize, payload => new TelemetryResponseMessage(payload));
        return responseMessage is not null;
    }

    /// <summary>
    /// Gets the general telemetry metrics for an acceptor.
    /// </summary>
    /// <returns>An instance of <see cref="UnitMetrics"/> if successful; otherwise, null.</returns>
    public async Task<UnitMetrics?> GetUnitMetrics()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetUnitMetrics, [],
            GetUnitMetricsResponseMessage.PayloadByteSize, payload => new GetUnitMetricsResponseMessage(payload));
        return responseMessage?.UnitMetrics;
    }

    /// <summary>
    /// Gets the telemetry metrics since the last time an acceptor was serviced.
    /// </summary>
    /// <returns>An instance of <see cref="ServiceUsageCounters"/> if successful; otherwise, null.</returns>
    public async Task<ServiceUsageCounters?> GetServiceUsageCounters()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetServiceUsageCounters, [],
            GetServiceUsageCountersResponseMessage.PayloadByteSize,
            payload => new GetServiceUsageCountersResponseMessage(payload));
        return responseMessage?.ServiceUsageCounters;
    }

    /// <summary>
    /// Gets the flags about what needs to be serviced.
    /// </summary>
    /// <returns>An instance of <see cref="ServiceFlags"/> if successful; otherwise, null.</returns>
    public async Task<ServiceFlags?> GetServiceFlags()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetServiceFlags, [],
            GetServiceFlagsResponseMessage.PayloadByteSize, payload => new GetServiceFlagsResponseMessage(payload));
        return responseMessage?.ServiceFlags;
    }

    /// <summary>
    /// Clears 1 or more service flags.
    /// </summary>
    /// <param name="correctableComponent">The component to clear the service flag for.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public async Task<bool> ClearServiceFlags(CorrectableComponent correctableComponent)
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.ClearServiceFlags,
            [(byte)correctableComponent],
            Rs232ResponseMessage.MinPayloadByteSize, payload => new TelemetryResponseMessage(payload));
        return responseMessage is not null;
    }

    /// <summary>
    /// Gets the info that was attached to the last service.
    /// </summary>
    /// <returns>An instance of <see cref="ServiceInfo"/> if successful; otherwise, null.</returns>
    public async Task<ServiceInfo?> GetServiceInfo()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetServiceInfo, [],
            GetServiceInfoResponseMessage.PayloadByteSize, payload => new GetServiceInfoResponseMessage(payload));
        return responseMessage?.ServiceInfo;
    }

    /// <summary>
    /// Gets the telemetry metrics that pertain to an acceptor's firmware.
    /// </summary>
    /// <returns>An instance of <see cref="FirmwareMetrics"/> if successful; otherwise, null.</returns>
    public async Task<FirmwareMetrics?> GetFirmwareMetrics()
    {
        var responseMessage = await SendTelemetryMessageAsync(TelemetryCommand.GetFirmwareMetrics, [],
            GetFirmwareMetricsResponseMessage.PayloadByteSize,
            payload => new GetFirmwareMetricsResponseMessage(payload));
        return responseMessage?.FirmwareMetrics;
    }

    private async Task<TResponseMessage?> SendTelemetryMessageAsync<TResponseMessage>(TelemetryCommand command,
        IReadOnlyList<byte> requestData, byte expectedResponseByteSize,
        Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage)
        where TResponseMessage : TelemetryResponseMessage
    {
        var requestMessage = new TelemetryRequestMessage(!_lastAck, command, requestData);
        return await SendNonPollMessageAsync(requestMessage, expectedResponseByteSize, createResponseMessage);
    }
}