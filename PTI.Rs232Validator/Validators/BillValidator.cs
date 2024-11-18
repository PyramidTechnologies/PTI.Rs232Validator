using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
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
    private const byte MaxReadAttempts = 4;
    private static readonly TimeSpan BackoffIncrement = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan StopLoopTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger _logger;
    private readonly ISerialProvider _serialProvider;
    private readonly object _mutex = new();
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
    protected BillValidator(ILogger logger, ISerialProvider serialProvider, Rs232Configuration configuration)
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
    public event EventHandler<int>? OnBillStacked;

    /// <summary>
    /// An event that is raised when a bill is escrowed.
    /// </summary>
    public event EventHandler<int>? OnBillEscrowed;

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

            if (!_serialProvider.TryOpen())
            {
                _logger.LogError("Failed to open the serial provider.");
                return false;
            }

            _isRunning = true;
            CanAcceptBills = false;
        }

        var checkForDevice = Task.Run(() =>
        {
            for (var i = 0; i < SuccessfulPollsRequiredToStartMessageLoop; i++)
            {
                var pollRequestMessage = new PollRequestMessage(!_lastAck);
                if (!SendPollMessage(pollRequestMessage))
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

        try
        {
            _serialProvider.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to close the serial provider: {0}", ex.Message);
        }
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

    private IReadOnlyList<byte> ReadFromPort(byte byteCount)
    {
        var backoffTime = BackoffIncrement;
        for (var i = 1; i < MaxReadAttempts; i++)
        {
            var payload = _serialProvider.Read(byteCount).AsReadOnly();
            if (payload.Count != 0)
            {
                return payload;
            }

            Thread.Sleep(backoffTime);
            backoffTime += BackoffIncrement;
        }

        return Array.Empty<byte>();
    }

    private MessageRetrievalResult TrySendMessage<TResponseMessage>(Rs232Message requestMessage,
        byte expectedResponseByteSize,
        Func<IReadOnlyList<byte>, TResponseMessage> createResponseMessage, out TResponseMessage responseMessage)
        where TResponseMessage : Rs232ResponseMessage
    {
        _serialProvider.Write(requestMessage.Payload.ToArray());

        var responsePayload = ReadFromPort(expectedResponseByteSize);
        responseMessage = createResponseMessage(responsePayload);

        _logger.LogTrace("Sent: {0}", requestMessage.Payload.ConvertToHexString(true));
        _logger.LogTrace("Received: {0}", responseMessage.Payload.ConvertToHexString(true));

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

        var payloadIssues = responseMessage.GetPayloadIssues();
        if (payloadIssues.Any())
        {
            _logger.LogError("Received an invalid response:");
            foreach (var issue in payloadIssues)
            {
                _logger.LogError($"\t{issue}");
            }

            return MessageRetrievalResult.IncorrectPayload;
        }

        if (_lastAck == responseMessage.Ack)
        {
            return MessageRetrievalResult.IncorrectAck;
        }

        _lastAck = responseMessage.Ack;
        return MessageRetrievalResult.Success;
    }

    private bool SendPollMessage(PollRequestMessage pollRequestMessage)
    {
        var messageRetrievalResult = TrySendMessage(pollRequestMessage, PollResponseMessage.PayloadByteSize,
            payload => new PollResponseMessage(payload), out var pollResponseMessage);
        if (messageRetrievalResult != MessageRetrievalResult.Success)
        {
            return false;
        }

        if (pollResponseMessage.State != _lastState)
        {
            _logger.LogInfo("State changed from {0} to {1}.", _lastState, pollResponseMessage.State);
            OnStateChanged?.Invoke(this, new StateChangeArgs(_lastState, pollResponseMessage.State));
            _lastState = pollResponseMessage.State;
        }

        if (pollResponseMessage.Event != Rs232Event.None)
        {
            _logger.LogInfo("Received event(s): {0}.", pollResponseMessage.Event);
            OnEventReported?.Invoke(this, pollResponseMessage.Event);
        }

        if (pollResponseMessage.IsCashboxPresent && !_wasCashboxAttachmentReported)
        {
            _logger.LogInfo("Cashbox was attached.");
            OnCashboxAttached?.Invoke(this, EventArgs.Empty);
            _wasCashboxAttachmentReported = true;
            _wasCashboxRemovalReported = false;
        }

        if (!pollResponseMessage.IsCashboxPresent && !_wasCashboxRemovalReported)
        {
            _logger.LogInfo("Cashbox was removed.");
            OnCashboxRemoved?.Invoke(this, EventArgs.Empty);
            _wasCashboxRemovalReported = true;
            _wasCashboxAttachmentReported = false;
        }

        if (pollResponseMessage.Event.HasFlag(Rs232Event.Stacked))
        {
            if (pollResponseMessage.BillType == 0)
            {
                _logger.LogError("Stacked an unknown bill.");
            }
            else
            {
                _logger.LogInfo("Stacked a bill of type {0}.", pollResponseMessage.BillType);
                OnBillStacked?.Invoke(this, pollResponseMessage.BillType);
            }
        }

        if (pollResponseMessage.Event.HasFlag(Rs232State.Escrowed) && Configuration.ShouldEscrow)
        {
            if (pollResponseMessage.BillType == 0)
            {
                _logger.LogError("Escrowed an unknown bill.");
            }
            else
            {
                _logger.LogInfo("Escrowed a bill of type {0}.", pollResponseMessage.BillType);
                OnBillEscrowed?.Invoke(this, pollResponseMessage.BillType);
            }
        }

        return true;
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
                    var pollRequestMessage = new PollRequestMessage(!_lastAck)
                        .SetEnableMask(CanAcceptBills ? Configuration.EnableMask : (byte)0)
                        .SetEscrowRequested(Configuration.ShouldEscrow)
                        .SetStackRequested(_shouldRequestBillStack)
                        .SetReturnRequested(_shouldRequestBillReturn);

                    lock (_mutex)
                    {
                        _shouldRequestBillStack = false;
                        _shouldRequestBillReturn = false;
                    }

                    messageCallback = () => SendPollMessage(pollRequestMessage);
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
        IncorrectPayload,
        IncorrectAck
    }
}