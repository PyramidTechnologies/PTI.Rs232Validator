using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.BillValidators;

public partial class BillValidator
{
    /// <summary>
    /// Pings the acceptor.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="TelemetryResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<TelemetryResponseMessage> PingAsync()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.Ping, [],
            payload => new TelemetryResponseMessage(payload));
    }

    /// <summary>
    /// Gets the serial number assigned to the acceptor.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetSerialNumberResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetSerialNumberResponseMessage> GetSerialNumberAsync()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetSerialNumber, [],
            payload => new GetSerialNumberResponseMessage(payload));
    }

    /// <summary>
    /// Gets the telemetry metrics about the cashbox.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetCashboxMetricsResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetCashboxMetricsResponseMessage> GetCashboxMetrics()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetCashboxMetrics, [],
            payload => new GetCashboxMetricsResponseMessage(payload));
    }

    /// <summary>
    /// Clears the count of bills in the cashbox.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="TelemetryResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<TelemetryResponseMessage> ClearCashboxCount()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.ClearCashboxCount, [],
            payload => new TelemetryResponseMessage(payload));
    }

    /// <summary>
    /// Gets the general telemetry metrics for an acceptor.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetUnitMetricsResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetUnitMetricsResponseMessage> GetUnitMetrics()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetUnitMetrics, [],
            payload => new GetUnitMetricsResponseMessage(payload));
    }

    /// <summary>
    /// Gets the telemetry metrics since the last time an acceptor was serviced.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetServiceUsageCountersResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetServiceUsageCountersResponseMessage> GetServiceUsageCounters()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetServiceUsageCounters, [],
            payload => new GetServiceUsageCountersResponseMessage(payload));
    }

    /// <summary>
    /// Gets the flags about what needs to be serviced.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetServiceFlagsResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetServiceFlagsResponseMessage> GetServiceFlags()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetServiceFlags, [],
            payload => new GetServiceFlagsResponseMessage(payload));
    }

    /// <summary>
    /// Clears 1 or more service flags.
    /// </summary>
    /// <param name="correctableComponent">The component to clear the service flag for.</param>
    /// <returns>
    /// An instance of <see cref="TelemetryResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<TelemetryResponseMessage> ClearServiceFlags(CorrectableComponent correctableComponent)
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.ClearServiceFlags,
            [(byte)correctableComponent], payload => new TelemetryResponseMessage(payload));
    }

    /// <summary>
    /// Gets the info that was attached to the last service.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetServiceInfoResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetServiceInfoResponseMessage> GetServiceInfo()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetServiceInfo, [],
            payload => new GetServiceInfoResponseMessage(payload));
    }

    /// <summary>
    /// Gets the telemetry metrics that pertain to an acceptor's firmware.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GetFirmwareMetricsResponseMessage"/> with
    /// <see cref="TelemetryResponseMessage.IsValid"/> set to true if successful.
    /// </returns>
    /// <remarks>The work is queued on the thread pool.</remarks>
    public async Task<GetFirmwareMetricsResponseMessage> GetFirmwareMetrics()
    {
        return await SendTelemetryMessageAsync(TelemetryCommand.GetFirmwareMetrics, [],
            payload => new GetFirmwareMetricsResponseMessage(payload));
    }

    private async Task<TResponseMessage> SendTelemetryMessageAsync<TResponseMessage>(TelemetryCommand command,
        IReadOnlyList<byte> requestData, Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage)
        where TResponseMessage : TelemetryResponseMessage
    {
        return await SendNonPollMessageAsync(
            ack => new TelemetryRequestMessage(ack, command, requestData), createResponseMessage);
    }
}