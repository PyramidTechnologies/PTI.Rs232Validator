using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Commands;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
using PTI.Rs232Validator.Messages.Responses.Extended;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.BillValidators;

/// <summary>
/// A hardware connection to a bill acceptor.
/// </summary>
public partial class BillValidator : IDisposable
{
    private const byte SuccessfulPollsRequiredToStartPollingLoop = 2;
    private const byte MaxReadAttempts = 4;
    private const byte MaxIncorrectPayloadPardons = 2;
    private static readonly TimeSpan BackoffIncrement = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan StopLoopTimeout = TimeSpan.FromSeconds(3);

    private readonly ILogger _logger;
    private readonly ISerialProvider _serialProvider;
    private readonly object _mutex = new();

    // A message callback sends a message to the acceptor and returns true if the message can be discarded
    // (i.e. the message should NOT be sent again).
    private readonly Queue<Func<bool>> _messageCallbacks = new();
    private Func<bool>? _lastMessageCallback;

    private Task _worker = Task.CompletedTask;
    private bool _isRunning;
    private bool _lastAck;
    private Rs232State _state;
    private bool _shouldRequestBillStack;
    private bool _shouldRequestBillReturn;
    private bool _wasCashboxRemovalReported;
    private bool _wasCashboxAttachmentReported;
    private bool _wasEscrowedBillReported;
    private bool _wasBarcodeDetectionReported;
    private bool _wasConnectionLostReported;

    /// <summary>
    /// Initializes a new instance of <see cref="BillValidator"/>.
    /// </summary>
    public BillValidator(ILogger logger, ISerialProvider serialProvider, Rs232Configuration configuration)
    {
        _logger = logger;
        _serialProvider = serialProvider;
        Configuration = configuration;
    }

    /// <summary>
    /// An event that is raised when the state of the acceptor changes.
    /// </summary>
    public event EventHandler<StateChangeArgs>? OnStateChanged;

    /// <summary>
    /// An event that is raised when 1 or more events are reported by the acceptor.
    /// </summary>
    public event EventHandler<Rs232Event>? OnEventReported;

    /// <summary>
    /// An event that is raised when the cashbox is removed.
    /// </summary>
    public event EventHandler? OnCashboxRemoved;

    /// <summary>
    /// An event that is raised when the cashbox is attached.
    /// </summary>
    public event EventHandler? OnCashboxAttached;

    /// <summary>
    /// An event that is raised when a bill is stacked.
    /// </summary>
    public event EventHandler<byte>? OnBillStacked;

    /// <summary>
    /// An event that is raised when a bill is escrowed.
    /// </summary>
    public event EventHandler<byte>? OnBillEscrowed;

    /// <summary>
    /// An event that is raised when a barcode is detected.
    /// </summary>
    public event EventHandler<string>? OnBarcodeDetected;

    /// <summary>
    /// An event that is raised when the connection to the acceptor seems to be lost.
    /// </summary>
    public event EventHandler? OnConnectionLost;

    /// <inheritdoc cref="Rs232Configuration"/>
    public Rs232Configuration Configuration { get; }

    /// <summary>
    /// Is the connection to the acceptor present?
    /// </summary>
    public bool IsConnectionPresent => !_wasConnectionLostReported;

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _serialProvider.Dispose();
    }

    /// <summary>
    /// Starts the RS-232 polling loop.
    /// </summary>
    /// <returns>True if the polling loop starts; otherwise, false.</returns>
    public bool StartPollingLoop()
    {
        lock (_mutex)
        {
            if (_isRunning)
            {
                _logger.LogDebug("Already polling, so ignoring the start request.");
                return false;
            }
        }

        if (!TryOpenPort())
        {
            return false;
        }

        if (!CheckForDevice())
        {
            ClosePort();
            return false;
        }

        lock (_mutex)
        {
            _isRunning = true;
        }

        _worker = Task.Factory.StartNew(LoopPollMessages, TaskCreationOptions.LongRunning);
        return true;
    }

    /// <summary>
    /// Stops the RS-232 polling loop.
    /// </summary>
    public void StopPollingLoop()
    {
        lock (_mutex)
        {
            if (!_isRunning)
            {
                _logger.LogDebug("The polling loop is not running, so ignoring the stop request.");
                return;
            }

            _isRunning = false;
        }

        _logger.LogDebug("Stopping the polling loop...");

        if (_worker.Wait(StopLoopTimeout))
        {
            _logger.LogDebug("Stopped the polling loop.");
        }
        else
        {
            _logger.LogError("Failed to stop the polling loop.");
        }

        _messageCallbacks.Clear();
        _lastMessageCallback = null;
        _shouldRequestBillStack = false;
        _shouldRequestBillReturn = false;
        _wasCashboxRemovalReported = false;
        _wasCashboxAttachmentReported = false;
        _wasConnectionLostReported = false;

        ClosePort();
    }

    /// <summary>
    /// Stacks a bill in escrow.
    /// </summary>
    public void StackBill()
    {
        lock (_mutex)
        {
            if (_state != Rs232State.Escrowed)
            {
                _logger.LogDebug("Cannot stack a bill that is not in escrow.");
                return;
            }
        }
        

        lock (_mutex)
        {
            _shouldRequestBillStack = true;
        }
    }

    /// <summary>
    /// Returns a bill in escrow.
    /// </summary>
    public void ReturnBill()
    {
        lock (_mutex)
        {
            if (_state != Rs232State.Escrowed)
            {
                _logger.LogDebug("Cannot return a bill that is not in escrow.");
                return;
            }
        }

        lock (_mutex)
        {
            _shouldRequestBillReturn = true;
        }
    }

    private void EnqueueMessageCallback(Func<bool> messageCallback)
    {
        lock (_mutex)
        {
            _messageCallbacks.Enqueue(messageCallback);
        }
    }

    private Func<bool>? DequeueMessageCallback()
    {
        lock (_mutex)
        {
            if (_messageCallbacks.Count > 0)
            {
                return _messageCallbacks.Dequeue();
            }
        }

        return null;
    }

    private bool TryOpenPort()
    {
        if (_serialProvider.TryOpen())
        {
            return true;
        }

        _logger.LogDebug("Failed to open the serial provider.");
        return false;
    }

    private void ClosePort()
    {
        try
        {
            _serialProvider.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to close the serial provider: {0}", ex.Message);
        }
    }

    private void LogPayloadIssues<TResponseMessage>(Rs232ResponseMessage responseMessage)
        where TResponseMessage : Rs232ResponseMessage
    {
        var payloadIssues = responseMessage.GetPayloadIssues();
        if (!payloadIssues.Any())
        {
            return;
        }

        var errorMessage = "Received an invalid response for a {0}:";
        var errorArgs = new object[payloadIssues.Count + 1];
        errorArgs[0] = typeof(TResponseMessage).Name.AddSpacesToCamelCase();
        for (var i = 0; i < payloadIssues.Count; i++)
        {
            errorMessage += $"\n\t{{{i + 1}}}";
            errorArgs[i + 1] = payloadIssues[i];
        }

        _logger.LogError(errorMessage, errorArgs);
    }

    private MessageRetrievalResult TrySendMessage<TResponseMessage>(
        Func<bool, Rs232RequestMessage> createRequestMessage,
        Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage, out TResponseMessage responseMessage)
        where TResponseMessage : Rs232ResponseMessage
    {
        var requestMessage = createRequestMessage(!_lastAck);
        var requestPayload = requestMessage.Payload.ToArray();

        IReadOnlyList<byte> responsePayload = Array.Empty<byte>();
        var backoffTime = Configuration.PollingPeriod;
        for (var i = 0; i < MaxReadAttempts; i++)
        {
            _serialProvider.Write(requestPayload);

            responsePayload = _serialProvider.Read(2);
            if (responsePayload.Count == 2)
            {
                var remainingByteCount = (uint)(responsePayload[1] - 2);
                responsePayload = responsePayload.Concat(_serialProvider.Read(remainingByteCount)).ToArray();
                break;
            }

            Thread.Sleep(backoffTime);
            backoffTime += BackoffIncrement;
        }

        responseMessage = createResponseMessage(responsePayload);

        _logger.LogTrace("Sent data to acceptor: {0}", requestMessage.Payload.ConvertToHexString(true));
        _logger.LogTrace("Received data from acceptor: {0}", responseMessage.Payload.ConvertToHexString(true));

        if (responsePayload.Count == 0)
        {
            _logger.LogDebug("Experienced a communication timeout.");
            if (!_wasConnectionLostReported)
            {
                OnConnectionLost?.Invoke(this, EventArgs.Empty);
                _wasConnectionLostReported = true;
            }

            return MessageRetrievalResult.Timeout;
        }

        _wasConnectionLostReported = false;

        if (!responseMessage.IsValid)
        {
            return MessageRetrievalResult.IncorrectPayload;
        }

        if (requestMessage.Ack != responseMessage.Ack)
        {
            return MessageRetrievalResult.IncorrectAck;
        }

        _lastAck = responseMessage.Ack;
        return MessageRetrievalResult.Success;
    }

    private bool TrySendPollMessage(Func<bool, PollRequestMessage> createPollRequestMessage)
    {
        var messageRetrievalResult = TrySendMessage(createPollRequestMessage,
            payload =>
            {
                var pollResponseMessage = new PollResponseMessage(payload);
                if (pollResponseMessage.GetPayloadIssues().Count == 0)
                {
                    return pollResponseMessage;
                }

                var extendedResponseMessage = new ExtendedResponseMessage(payload);
                if (extendedResponseMessage.GetPayloadIssues().Count == 0)
                {
                    return extendedResponseMessage;
                }

                return pollResponseMessage;
            }, out var responseMessage);
        if (messageRetrievalResult != MessageRetrievalResult.Success)
        {
            if (messageRetrievalResult == MessageRetrievalResult.IncorrectPayload)
            {
                LogPayloadIssues<PollResponseMessage>(responseMessage);
            }

            return false;
        }

        if (responseMessage.State != _state)
        {
            _logger.LogDebug("The state changed from {0} to {1}.", _state, responseMessage.State);
            OnStateChanged?.Invoke(this, new StateChangeArgs(_state, responseMessage.State));

            lock (_mutex)
            {
                _state = responseMessage.State;
            }
        }

        if (responseMessage.Event != Rs232Event.None)
        {
            _logger.LogDebug("Received event(s): {0}.", responseMessage.Event);
            OnEventReported?.Invoke(this, responseMessage.Event);
        }

        if (responseMessage.IsCashboxPresent && !_wasCashboxAttachmentReported)
        {
            _logger.LogDebug("The cashbox was attached.");
            OnCashboxAttached?.Invoke(this, EventArgs.Empty);
            _wasCashboxAttachmentReported = true;
            _wasCashboxRemovalReported = false;
        }

        if (!responseMessage.IsCashboxPresent && !_wasCashboxRemovalReported)
        {
            _logger.LogDebug("The cashbox was removed.");
            OnCashboxRemoved?.Invoke(this, EventArgs.Empty);
            _wasCashboxRemovalReported = true;
            _wasCashboxAttachmentReported = false;
        }

        if (responseMessage.Event.HasFlag(Rs232Event.Stacked))
        {
            if (responseMessage.BillType == 0)
            {
                _logger.LogError("Stacked an unknown bill.");
            }
            else
            {
                _logger.LogDebug("Stacked a bill of type {0}.", responseMessage.BillType);
            }

            OnBillStacked?.Invoke(this, responseMessage.BillType);
        }

        if (responseMessage.State == Rs232State.Escrowed && !_wasEscrowedBillReported)
        {
            if (responseMessage.BillType == 0)
            {
                _logger.LogError("Escrowed an unknown bill.");
            }
            else
            {
                _logger.LogDebug("Escrowed a bill of type {0}.", responseMessage.BillType);
            }

            OnBillEscrowed?.Invoke(this, responseMessage.BillType);
            _wasEscrowedBillReported = true;
        }

        if (responseMessage.State != Rs232State.Escrowed)
        {
            lock (_mutex)
            {
                _shouldRequestBillStack = false;
                _shouldRequestBillReturn = false;
            }

            _wasEscrowedBillReported = false;
            _wasBarcodeDetectionReported = false;
        }

        if (responseMessage.MessageType == Rs232MessageType.ExtendedCommand)
        {
            _logger.LogDebug("Received an extended response message.");
            var extendedResponseMessage = (ExtendedResponseMessage)responseMessage;
            switch (extendedResponseMessage.Command)
            {
                case ExtendedCommand.BarcodeDetected:
                    var barcodeDetectedResponseMessage =
                        new BarcodeDetectedResponseMessage(extendedResponseMessage.Payload);
                    if (!barcodeDetectedResponseMessage.IsValid)
                    {
                        LogPayloadIssues<BarcodeDetectedResponseMessage>(barcodeDetectedResponseMessage);
                        return false;
                    }

                    if (!_wasBarcodeDetectionReported)
                    {
                        _logger.LogDebug("Detected a barcode: {0}", barcodeDetectedResponseMessage.Barcode);
                        OnBarcodeDetected?.Invoke(this, barcodeDetectedResponseMessage.Barcode);
                        _wasBarcodeDetectionReported = true;
                    }
                    
                    break;
                default:
                    _logger.LogError("Received an unknown extended command: {0}.", extendedResponseMessage.Command);
                    break;
            }
        }

        return true;
    }

    private async Task<TResponseMessage?> SendNonPollMessageAsync<TResponseMessage>(
        Func<bool, Rs232RequestMessage> createRequestMessage,
        Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage)
        where TResponseMessage : Rs232ResponseMessage
    {
        var eventWaitHandle = new ManualResetEvent(false);
        var incorrectPayloadCount = 0;
        TResponseMessage? responseMessage = null;
        var messageCallback = new Func<bool>(() =>
        {
            var messageRetrievalResult = TrySendMessage(createRequestMessage, createResponseMessage, out var r);
            switch (messageRetrievalResult)
            {
                case MessageRetrievalResult.IncorrectPayload when ++incorrectPayloadCount <= MaxIncorrectPayloadPardons:
                case MessageRetrievalResult.IncorrectAck:
                    return false;
                case MessageRetrievalResult.Success:
                    responseMessage = r;
                    break;
            }

            if (messageRetrievalResult == MessageRetrievalResult.IncorrectPayload)
            {
                LogPayloadIssues<TResponseMessage>(r);
            }

            eventWaitHandle.Set();
            return true;
        });

        bool isRunning;
        lock (_mutex)
        {
            isRunning = _isRunning;
        }

        if (isRunning)
        {
            EnqueueMessageCallback(messageCallback);
            return await Task.Run(() =>
            {
                eventWaitHandle.WaitOne();
                return responseMessage;
            });
        }

        return await Task.Run(() =>
        {
            if (!TryOpenPort())
            {
                return null;
            }

            if (!CheckForDevice())
            {
                ClosePort();
                return null;
            }

            while (!messageCallback.Invoke())
            {
                Thread.Sleep(Configuration.PollingPeriod);
            }

            ClosePort();
            return responseMessage;
        });
    }

    private bool CheckForDevice()
    {
        var successfulPolls = 0;
        var wasAckFlipped = false;
        while (successfulPolls < SuccessfulPollsRequiredToStartPollingLoop)
        {
            var messageRetrievalResult = TrySendMessage(
                ack => new PollRequestMessage(ack),
                payload => new PollResponseMessage(payload),
                out var pollResponseMessage);

            if (messageRetrievalResult != MessageRetrievalResult.Success)
            {
                if (messageRetrievalResult != MessageRetrievalResult.IncorrectPayload)
                {
                    return false;
                }

                if (wasAckFlipped)
                {
                    LogPayloadIssues<PollResponseMessage>(pollResponseMessage);
                    return false;
                }

                wasAckFlipped = true;
                _lastAck = !_lastAck;
                continue;
            }

            successfulPolls++;
            Thread.Sleep(Configuration.PollingPeriod);
        }

        return true;
    }

    private void LoopPollMessages()
    {
        while (true)
        {
            lock (_mutex)
            {
                if (!_isRunning)
                {
                    _logger.LogDebug("Received the stop signal.");
                    return;
                }
            }

            if (_lastMessageCallback is not null)
            {
                if (_lastMessageCallback.Invoke())
                {
                    _lastMessageCallback = null;
                }
            }
            else
            {
                var messageCallback = DequeueMessageCallback();
                if (messageCallback is not null)
                {
                    if (!messageCallback.Invoke())
                    {
                        _lastMessageCallback = messageCallback;
                    }
                }
                else
                {
                    messageCallback = () => TrySendPollMessage(ack =>
                        new PollRequestMessage(ack)
                            .SetEnableMask(Configuration.EnableMask)
                            .SetEscrowRequested(Configuration.ShouldEscrow || _shouldRequestBillStack || _shouldRequestBillReturn)
                            .SetStackRequested(_shouldRequestBillStack)
                            .SetReturnRequested(_shouldRequestBillReturn)
                            .SetBarcodeDetectionRequested(Configuration.ShouldDetectBarcodes));
                    if (!messageCallback())
                    {
                        _lastMessageCallback = messageCallback;
                    }
                }
            }

            Thread.Sleep(Configuration.PollingPeriod);
        }
    }

    private enum MessageRetrievalResult : byte
    {
        Success,
        Timeout,
        IncorrectAck,
        IncorrectPayload
    }
}