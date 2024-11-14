using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Messages;
using PTI.Rs232Validator.Messages.Requests;
using PTI.Rs232Validator.Messages.Responses;
using PTI.Rs232Validator.Messages.Responses.Telemetry;
using PTI.Rs232Validator.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Validators;

/// <summary>
/// A hardware connection to a bill acceptor.
/// </summary>
public class BillValidator : IDisposable
{
    private const byte SuccessfulPollsRequired = 2;
    private const byte TimeoutFactor = 5;

    private const byte MaxTimeoutCount = 3;
    private const byte MaxRetryCount = 3;
    private static readonly TimeSpan BackoffIncrement = TimeSpan.FromMilliseconds(50);

    private readonly object _mutex = new();

    private bool _isRunning;

    // TODO: Consider renaming.
    private Queue<Func<bool>> _messageCallbacks = new();
    private Thread? _worker;

    private bool _wasPreviousAckNumberOne;
    private byte _timeoutCount;
    private Func<bool>? _lastMessageCallback;
    private Rs232State _lastState;
    private bool _shouldStack;
    private bool _shouldReturn;
    private bool _wasCashBoxRemovalReported;
    private bool _wasCashBoxAttachmentReported;
    private bool _wasConnectionLostReported;

    /// <summary>
    /// Initializes a new instance of <see cref="BillValidator"/>.
    /// </summary>
    /// <param name="config"><see cref="Rs232Config"/>.</param>
    protected BillValidator(Rs232Config config)
    {
        Config = config;
        Logger.Info("Initialized {0} with config — {1}.", GetType().Name, config);
    }

    /// <summary>
    /// An event that is raised when the state of the acceptor changes.
    /// </summary>
    public event EventHandler<StateChangeArgs> StateChanged;

    /// <summary>
    /// An event that is raised when 1 or more events are reported by the acceptor.
    /// </summary>
    public event EventHandler<Rs232Event> EventReported;

    /// <summary>
    /// An event that is raised when the cash box is removed.
    /// </summary>
    public event EventHandler CashBoxRemoved;

    /// <summary>
    /// An event that is raised when the cash box is attached.
    /// </summary>
    public event EventHandler CashBoxAttached;
    
    /// <summary>
    /// TODO: Finish.
    /// An event that is raised when ...
    /// </summary>
    public event EventHandler<int> BillStacked;
    
    /// <summary>
    /// TODO: Finish.
    /// </summary>
    public event EventHandler<int> BillEscrowed;

    /// <summary>
    /// Raised when the API suspects the connection to the acceptor has been lost.
    /// </summary>
    public event EventHandler ConnectionLost;

    /// <inheritdoc cref="Rs232Config"/>
    public Rs232Config Config { get; }

    /// <inheritdoc cref="Rs232Config.SerialProvider"/>
    protected ISerialProvider SerialProvider => Config.SerialProvider;

    /// <inheritdoc cref="Rs232Config.Logger"/>
    protected ILogger Logger => Config.Logger;

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SerialProvider.Dispose();
        Logger.Info("{0} was disposed.", GetType().Name);
    }

    /// <summary>
    ///     Attempt to start the RS232 polling loop
    /// </summary>
    /// <returns>True when loop starts</returns>
    public bool StartMessageLoop()
    {
        lock (_mutex)
        {
            if (_isRunning)
            {
                Logger.Error("Already polling, so ignoring start request.");
                return false;
            }

            if (!SerialProvider.TryOpen())
            {
                Logger.Error("Failed to open serial provider.");
                return false;
            }

            _isRunning = true;
        }

        _worker = new Thread(MainLoop)
        {
            // Terminate if the parent thread is terminated.
            IsBackground = true
        };
        
        // TODO: Perform liveness check.

        _worker.Start();
        return true;
    }

    /// <summary>
    ///     Stop the RS232 polling loop
    /// </summary>
    public void StopMessageLoop()
    {
        lock (_mutex)
        {
            if (!_isRunning)
            {
                Logger.Error("{0} Polling loop is not running, ignoring stop command", GetType().Name);
                return;
            }

            _isRunning = false;
        }

        Logger.Debug("{0} Stopping polling loop...", GetType().Name);

        if (!_worker.Join(TimeSpan.FromSeconds(10)))
        {
            Logger.Error("{0} Failed to stop polling loop", GetType().Name);
        }
        else
        {
            Logger.Info("{0} Polling loop stopped", GetType().Name);
        }

        try
        {
            SerialProvider.Close();
        }
        catch (Exception ex)
        {
            Logger.Error("Unable to close serial provider: {1}", GetType().Name, ex.Message);
        }
    }

    /// <summary>
    /// TODO: Finish.
    /// </summary>
    /// <returns></returns>
    public Task<bool> Ping()
    {
        var requestMessage = new TelemetryRequestMessage(!_wasPreviousAckNumberOne, TelemetryCommand.Ping, []);
        var eventWaitHandle = new ManualResetEvent(false);
        var wasPingSuccessful = false;
        EnqueueMessageCallback(() =>
        {
            var responseMessage = SendMessage(requestMessage, Rs232ResponseMessage.MinPayloadByteSize,
                payload => new TelemetryResponseMessage(payload));
            if (responseMessage is null)
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

    private IReadOnlyList<byte> ReadFromPort(byte byteCount)
    {
        var backoffTime = BackoffIncrement;
        for (var i = 0; i < MaxRetryCount; i++)
        {
            var payload = SerialProvider.Read(byteCount);
            if (payload.Length != 0)
            {
                return payload;
            }

            Thread.Sleep(backoffTime);
            backoffTime += BackoffIncrement;
        }

        return [];
    }

    private TResponseMessage? SendMessage<TResponseMessage>(Rs232Message requestMessage, byte expectedResponseByteSize,
        Func<byte[], TResponseMessage> createResponseMessage) where TResponseMessage : Rs232ResponseMessage
    {
        SerialProvider.Write(requestMessage.Payload.ToArray());

        var responsePayload = SerialProvider.Read(expectedResponseByteSize);
        var responseMessage = createResponseMessage(responsePayload);

        // TODO: Log request and response payloads.

        if (responsePayload.Length == 0)
        {
            _timeoutCount++;
        }

        if (_timeoutCount > MaxTimeoutCount)
        {
            Logger.Error("Experienced too many timeouts.");
            // TODO: Raise a lost connection event.
            return null;
        }

        var payloadIssues = responseMessage.GetPayloadIssues();
        if (payloadIssues.Any())
        {
            Logger.Error("Received an invalid response:");
            foreach (var issue in payloadIssues)
            {
                Logger.Error($"\t{issue}");
            }

            return null;
        }

        if (_wasPreviousAckNumberOne == responseMessage.IsAckNumberOne)
        {
            return null;
        }

        _wasPreviousAckNumberOne = responseMessage.IsAckNumberOne;
        _timeoutCount = 0;
        return responseMessage;
    }

    private MessageResult SendPollMessage()
    {
        var requestMessage = new PollRequestMessage(!_wasPreviousAckNumberOne)
            .SetEnableMask(Config.EnableMask)
            .SetEscrowRequested(Config.IsEscrowMode)
            .SetStackRequested(_shouldStack)
            .SetReturnRequested(_shouldReturn);

        _shouldStack = false;
        _shouldReturn = false;

        var responseMessage = SendMessage(requestMessage, PollResponseMessage.PayloadByteSize,
            payload => new PollResponseMessage(payload));
        if (responseMessage is null)
        {
            return false;
        }
        
        if (responseMessage.State != _lastState)
        {
            // TODO: Log.
            StateChanged.Invoke(this, new StateChangeArgs(_lastState, responseMessage.State));
            _lastState = responseMessage.State;
        }

        if (responseMessage.Event != Rs232Event.None)
        {
            // TODO: Log.
            EventReported.Invoke(this, responseMessage.Event);
        }

        if (responseMessage.IsCashBoxPresent && !_wasCashBoxAttachmentReported)
        {
            // TODO: Log.
            CashBoxAttached.Invoke(this, EventArgs.Empty);
            _wasCashBoxAttachmentReported = true;
            _wasCashBoxRemovalReported = false;
        }
        
        if (!responseMessage.IsCashBoxPresent && !_wasCashBoxRemovalReported)
        {
            // TODO: Log.
            CashBoxRemoved.Invoke(this, EventArgs.Empty);
            _wasCashBoxRemovalReported = true;
            _wasCashBoxAttachmentReported = false;
        }

        if (responseMessage.Event.HasFlag(Rs232Event.Stacked))
        {
            if (responseMessage.BillType == 0)
            {
                // TODO: Log.
            }
            else
            {
                // TODO: Log.
                BillStacked.Invoke(this, responseMessage.BillType);
            }
        }

        if (responseMessage.Event.HasFlag(Rs232State.Escrowed) && Config.IsEscrowMode)
        {
            if (responseMessage.BillType == 0)
            {
                // TODO: Log.
            }
            else
            {
                // TODO: Log.
                BillEscrowed.Invoke(this, responseMessage.BillType);
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
                    Logger.Debug("{0} received the stop signal.", GetType().Name);
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
                    if (!SendPollMessage())
                    {
                        _lastMessageCallback = SendPollMessage;
                    }
                }
            }

            Thread.Sleep(Config.PollingPeriod);
        }
    }

    /// <summary>
    /// TODO: Rename.
    /// </summary>
    private enum MessageResult : byte
    {
        Success,
        Timeout,
        IncorrectDelivery,
        IncorrectPayload
    }
}