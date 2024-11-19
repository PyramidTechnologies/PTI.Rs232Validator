using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
using PTI.Rs232Validator.Messages.Responses.Telemetry;
using PTI.Rs232Validator.Models;
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
    public Task<bool> Ping()
    {
        var requestMessage = new TelemetryRequestMessage(!_lastAck, TelemetryCommand.Ping, []);
        var eventWaitHandle = new ManualResetEvent(false);
        var incorrectPayloadCount = 0;
        var wasPingSuccessful = false;

        EnqueueMessageCallback(() =>
        {
            var messageRetrievalResult = TrySendMessage(requestMessage, Rs232ResponseMessage.MinPayloadByteSize,
                payload => new TelemetryResponseMessage(payload), out _);
            if (messageRetrievalResult == MessageRetrievalResult.Timeout
                || (messageRetrievalResult == MessageRetrievalResult.IncorrectPayload
                    && ++incorrectPayloadCount <= MaxIncorrectPayloadPardons)
                || messageRetrievalResult == MessageRetrievalResult.IncorrectAck)
            {
                return false;
            }

            if (incorrectPayloadCount <= MaxIncorrectPayloadPardons)
            {
                wasPingSuccessful = true;
            }
            
            eventWaitHandle.Set();
            return true;
        });

        return Task.Run(() =>
        {
            eventWaitHandle.WaitOne();
            return wasPingSuccessful;
        });
    }

    public Task<string> GetSerialNumber()
    {
        var requestMessage = new TelemetryRequestMessage(!_lastAck, TelemetryCommand.GetSerialNumber, []);
        var eventWaitHandle = new ManualResetEvent(false);
        var incorrectPayloadCount = 0;
        var serialNumber = string.Empty;

        EnqueueMessageCallback(() =>
        {
            var messageRetrievalResult = TrySendMessage(requestMessage, GetSerialNumberResponseMessage.PayloadByteSize,
                payload => new GetSerialNumberResponseMessage(payload), out var responseMessage);
            if (messageRetrievalResult == MessageRetrievalResult.Timeout
                || (messageRetrievalResult == MessageRetrievalResult.IncorrectPayload
                    && ++incorrectPayloadCount <= MaxIncorrectPayloadPardons)
                || messageRetrievalResult == MessageRetrievalResult.IncorrectAck)
            {
                return false;
            }
            
            if (incorrectPayloadCount <= MaxIncorrectPayloadPardons)
            {
                serialNumber = responseMessage.SerialNumber;
            }
            
            eventWaitHandle.Set();
            return true;
        });

        return Task.Run(() =>
        {
            eventWaitHandle.WaitOne();
            return serialNumber;
        });
    }

    public Task<CashboxMetrics?> GetCashboxMetrics()
    {
        var requestMessage = new TelemetryRequestMessage(!_lastAck, TelemetryCommand.GetCashboxMetrics, []);
        var eventWaitHandle = new ManualResetEvent(false);
        var incorrectPayloadCount = 0;
        CashboxMetrics? cashboxMetrics = null;

        EnqueueMessageCallback(() =>
        {
            var messageRetrievalResult = TrySendMessage(requestMessage,
                GetCashboxMetricsResponseMessage.PayloadByteSize,
                payload => new GetCashboxMetricsResponseMessage(payload), out var responseMessage);
            if (messageRetrievalResult == MessageRetrievalResult.Timeout
                || (messageRetrievalResult == MessageRetrievalResult.IncorrectPayload
                    && ++incorrectPayloadCount <= MaxIncorrectPayloadPardons)
                || messageRetrievalResult == MessageRetrievalResult.IncorrectAck)
            {
                return false;
            }
            
            if (incorrectPayloadCount <= MaxIncorrectPayloadPardons)
            {
                cashboxMetrics = responseMessage.CashboxMetrics;
            }
            
            eventWaitHandle.Set();
            return true;
        });

        return Task.Run(() =>
        {
            eventWaitHandle.WaitOne();
            return cashboxMetrics;
        });
    }

    public Task<bool> ClearCashboxCount()
    {
        var requestMessage = new TelemetryRequestMessage(!_lastAck, TelemetryCommand.ClearCashboxCount, []);
        var eventWaitHandle = new ManualResetEvent(false);
        var incorrectPayloadCount = 0;
        var wasResetSuccessful = false;

        EnqueueMessageCallback(() =>
        {
            var messageRetrievalResult = TrySendMessage(requestMessage, Rs232ResponseMessage.MinPayloadByteSize,
                payload => new TelemetryResponseMessage(payload), out _);
            if (messageRetrievalResult == MessageRetrievalResult.Timeout
                || (messageRetrievalResult == MessageRetrievalResult.IncorrectPayload
                    && ++incorrectPayloadCount <= MaxIncorrectPayloadPardons)
                || messageRetrievalResult == MessageRetrievalResult.IncorrectAck)
            {
                return false;
            }

            if (incorrectPayloadCount <= MaxIncorrectPayloadPardons)
            {
                wasResetSuccessful = true;
            }
            
            eventWaitHandle.Set();
            return true;
        });

        return Task.Run(() =>
        {
            eventWaitHandle.WaitOne();
            return wasResetSuccessful;
        });
    }
}