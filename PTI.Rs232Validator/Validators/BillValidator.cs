﻿using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Messages;
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

namespace PTI.Rs232Validator.Validators;

/// <summary>
/// A hardware connection to a bill acceptor.
/// </summary>
public partial class BillValidator : IDisposable
{
    private const byte SuccessfulPollsRequiredToStartMessageLoop = 2;
    private const byte MaxReadAttempts = 3;
    private const byte MaxIncorrectPayloadPardons = 2;
    private static readonly TimeSpan BackoffIncrement = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan StopLoopTimeout = TimeSpan.FromSeconds(10);

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
    private Rs232State _lastState;
    private bool _shouldRequestBillStack;
    private bool _shouldRequestBillReturn;
    private bool _wasCashboxRemovalReported;
    private bool _wasCashboxAttachmentReported;
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
    /// Can the acceptor accept bills?
    /// </summary>
    public bool CanAcceptBills { get; private set; }

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
    /// Starts the RS-232 message loop.
    /// </summary>
    /// <returns>True if the message loop starts; otherwise, false.</returns>
    public bool StartMessageLoop()
    {
        lock (_mutex)
        {
            if (_isRunning)
            {
                _logger.LogDebug("Already sending messages, so ignoring the start request.");
                return false;
            }

            if (!TryOpenPort())
            {
                return false;
            }

            _isRunning = true;
            CanAcceptBills = false;
        }

        var checkForDevice = Task.Run(() =>
        {
            for (var i = 0; i < SuccessfulPollsRequiredToStartMessageLoop; i++)
            {
                // TODO: Alter.
                if (!TrySendPollMessage(ack => new PollRequestMessage(ack)) && !TrySendPollMessage(ack => new PollRequestMessage(ack)))
                {
                    return false;
                }

                Thread.Sleep(Configuration.PollingPeriod);
            }

            return true;
        });

        if (!checkForDevice.Result)
        {
            return false;
        }

        _worker = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);
        return true;
    }

    /// <summary>
    /// Stops the RS-232 message loop.
    /// </summary>
    public void StopMessageLoop()
    {
        lock (_mutex)
        {
            if (!_isRunning)
            {
                _logger.LogDebug("The message loop is not running, so ignoring the stop request.");
                return;
            }

            _isRunning = false;
            CanAcceptBills = false;
        }

        _logger.LogDebug("Stopping the message loop...");

        if (_worker.Wait(StopLoopTimeout))
        {
            _logger.LogInfo("Stopped the message loop.");
        }
        else
        {
            _logger.LogError("Failed to stop the message loop.");
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
        if (!Configuration.ShouldEscrow)
        {
            _logger.LogError("Cannot stack a bill in non-escrow mode.");
            return;
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
        if (!Configuration.ShouldEscrow)
        {
            _logger.LogError("Cannot return a bill in non-escrow mode.");
            return;
        }

        lock (_mutex)
        {
            _shouldRequestBillReturn = true;
        }
    }

    /// <summary>
    /// Allows the acceptor to accept bills.
    /// </summary>
    public void AllowBillAcceptance()
    {
        if (CanAcceptBills)
        {
            _logger.LogDebug("The acceptor is already allowed to accept bills.");
            return;
        }

        lock (_mutex)
        {
            CanAcceptBills = true;
        }
    }

    /// <summary>
    /// Forbids the acceptor from accepting bills.
    /// </summary>
    public void ForbidBillAcceptance()
    {
        if (!CanAcceptBills)
        {
            _logger.LogDebug("The acceptor is already forbidden from accepting bills.");
            return;
        }

        lock (_mutex)
        {
            CanAcceptBills = false;
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

        _logger.LogError("Failed to open the serial provider.");
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

    // TODO: Consider adding a message descriptor.
    private void LogPayloadIssues(Rs232ResponseMessage responseMessage)
    {
        var payloadIssues = responseMessage.GetPayloadIssues();
        if (payloadIssues.Any())
        {
            _logger.LogError("Received an invalid response:");
            foreach (var issue in payloadIssues)
            {
                _logger.LogError($"\t{issue}");
            }
        }
    }

    private MessageRetrievalResult TrySendMessage<TResponseMessage>(
        Func<bool, Rs232Message> createRequestMessage,
        Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage, out TResponseMessage responseMessage)
        where TResponseMessage : Rs232ResponseMessage
    {
        var requestMessage = createRequestMessage(!_lastAck);
        var requestPayload = requestMessage.Payload.ToArray();

        IReadOnlyList<byte> responsePayload = Array.Empty<byte>();
        var backoffTime = BackoffIncrement;
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
            _logger.LogError("Experienced a communication timeout.");
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
            if (responseMessage.FollowsCommonStructure)
            {
                _lastAck = !_lastAck;
            }
            else
            {
                LogPayloadIssues(responseMessage);
            }

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
                if (pollResponseMessage.GetPayloadIssues().Count > 0)
                {
                    return pollResponseMessage;
                }

                var extendedResponseMessage = new ExtendedResponseMessage(payload);
                if (extendedResponseMessage.GetPayloadIssues().Count > 0)
                {
                    return extendedResponseMessage;
                }

                return pollResponseMessage;
            }, out var responseMessage);
        if (messageRetrievalResult != MessageRetrievalResult.Success)
        {
            return false;
        }

        if (responseMessage.State != _lastState)
        {
            _logger.LogInfo("State changed from {0} to {1}.", _lastState, responseMessage.State);
            OnStateChanged?.Invoke(this, new StateChangeArgs(_lastState, responseMessage.State));
            _lastState = responseMessage.State;
        }

        if (responseMessage.Event != Rs232Event.None)
        {
            _logger.LogInfo("Received event(s): {0}.", responseMessage.Event);
            OnEventReported?.Invoke(this, responseMessage.Event);
        }

        if (responseMessage.IsCashboxPresent && !_wasCashboxAttachmentReported)
        {
            _logger.LogInfo("Cashbox was attached.");
            OnCashboxAttached?.Invoke(this, EventArgs.Empty);
            _wasCashboxAttachmentReported = true;
            _wasCashboxRemovalReported = false;
        }

        if (!responseMessage.IsCashboxPresent && !_wasCashboxRemovalReported)
        {
            _logger.LogInfo("Cashbox was removed.");
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
                _logger.LogInfo("Stacked a bill of type {0}.", responseMessage.BillType);
                OnBillStacked?.Invoke(this, responseMessage.BillType);
            }
        }

        if (responseMessage.Event.HasFlag(Rs232State.Escrowed) && Configuration.ShouldEscrow)
        {
            if (responseMessage.BillType == 0)
            {
                _logger.LogError("Escrowed an unknown bill.");
            }
            else
            {
                _logger.LogInfo("Escrowed a bill of type {0}.", responseMessage.BillType);
                OnBillEscrowed?.Invoke(this, responseMessage.BillType);
            }
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
                        LogPayloadIssues(barcodeDetectedResponseMessage);
                        return false;
                    }

                    _logger.LogInfo("Detected a barcode: {0}",
                        barcodeDetectedResponseMessage.Barcode);
                    OnBarcodeDetected?.Invoke(this, barcodeDetectedResponseMessage.Barcode);
                    break;
                default:
                    _logger.LogError("Received an unknown extended command: {0}.", extendedResponseMessage.Command);
                    break;
            }
        }

        return true;
    }

    private async Task<TResponseMessage?> SendNonPollMessageAsync<TResponseMessage>(
        Func<bool, Rs232Message> createRequestMessage,
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
                LogPayloadIssues(r);
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

            while (!messageCallback.Invoke())
            {
                Thread.Sleep(Configuration.PollingPeriod);
            }

            ClosePort();

            return responseMessage;
        });
    }

    private void MainLoop()
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
                    lock (_mutex)
                    {
                        _shouldRequestBillStack = false;
                        _shouldRequestBillReturn = false;
                    }

                    messageCallback = () => TrySendPollMessage(ack =>
                        new PollRequestMessage(ack)
                            .SetAcceptanceMask(CanAcceptBills ? Configuration.AcceptanceMask : (byte)0)
                            .SetEscrowRequested(Configuration.ShouldEscrow)
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