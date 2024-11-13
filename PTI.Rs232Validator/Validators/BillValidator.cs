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
/// An hardware connection to a bill acceptor.
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
    private Queue<Rs232Message> _messageQueue = new();
    private Thread? _worker;

    private bool _wasPreviousAckNumberOne;
    private byte _timeoutCount;

    /// <summary>
    /// Initializes a new instance of <see cref="BillValidator"/>.
    /// </summary>
    /// <param name="config"><see cref="Rs232Config"/>.</param>
    protected BillValidator(Rs232Config config)
    {
        Config = config;
        Logger.Info("Initialized {0} with config — {1}.", GetType().Name, config);
    }

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

        if (Config.DisableLivenessCheck)
        {
            Logger.Info("{0} Polling thread started (no liveness check): {1}",
                GetType().Name, _worker.ManagedThreadId);
        }
        else
        {
            var checkTask = Task.Run(() =>
            {
                var successfulPolls = 0;
                while (true)
                {
                    if (SendPollMessage())
                    {
                        successfulPolls++;
                        if (successfulPolls >= SuccessfulPollsRequired)
                        {
                            Logger.Info("{0} Polling thread started: {1}", GetType().Name, _worker.ManagedThreadId);
                            break;
                        }
                    }

                    Thread.Sleep(Config.PollingPeriod);
                }
            });
            
            var timeoutTask = Task.Delay(Config.PollingPeriod.Multiply(TimeoutFactor));
            
            if (Task.WhenAny(checkTask, timeoutTask).Result == timeoutTask)
            {
                Logger.Info("{0} timed out waiting for a valid polling response", GetType().Name);
                return false;
            }
            
            Logger.Info("{0} Polling thread started: {1}", GetType().Name, _worker.ManagedThreadId);
        }

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

    private bool SendPollMessage()
    {
        return true;
    }
    
    private bool SendTelemetryMessage()
    {
        return true;
    }

    private bool SendMessage<T>(Rs232Message requestMessage, byte expectedResponseByteSize, Func<byte[], Rs232ResponseMessage> createResponseMessage)
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
            return false;
        }

        var payloadIssues = responseMessage.GetPayloadIssues();
        if (payloadIssues.Any())
        {
            Logger.Error("Received an invalid response:");
            foreach (var issue in payloadIssues)
            {
                Logger.Error($"\t{issue}");
            }
            
            // TODO: Retransmit.
            return false;
        }

        if (_wasPreviousAckNumberOne == responseMessage.IsAckNumberOne)
        {
            // TODO: Retransmit.
            return false;
        }

        _wasPreviousAckNumberOne = responseMessage.IsAckNumberOne;
        _timeoutCount = 0;
        return true;
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
            
            Thread.Sleep(Config.PollingPeriod);
        }
    }

    
    
}