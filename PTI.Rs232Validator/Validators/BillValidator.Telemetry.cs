using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
using PTI.Rs232Validator.Messages.Responses.Telemetry;
using System.Threading;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Validators;

public partial class BillValidator
{
    /// <summary>
    /// Pings the acceptor.
    /// </summary>
    /// <returns>True if communications are operational; otherwise, false.</returns>
    public Task<bool> Ping()
    {
        var requestMessage = new TelemetryRequestMessage(!_lastAck, TelemetryCommand.Ping, []);
        var eventWaitHandle = new ManualResetEvent(false);
        var wasPingSuccessful = false;
        EnqueueMessageCallback(() =>
        {
            var messageRetrievalResult = TrySendMessage(requestMessage, Rs232ResponseMessage.MinPayloadByteSize,
                payload => new TelemetryResponseMessage(payload), out _);
            if (messageRetrievalResult != MessageRetrievalResult.Success &&
                messageRetrievalResult != MessageRetrievalResult.IncorrectAck)
            {
                wasPingSuccessful = false;
                return true;
            }

            wasPingSuccessful = true;
            eventWaitHandle.Set();
            return true;
        });

        return Task.Run(() =>
        {
            eventWaitHandle.WaitOne();
            return wasPingSuccessful;
        });
    }
}