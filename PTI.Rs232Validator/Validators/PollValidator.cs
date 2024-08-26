using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PTI.Rs232Validator.Validators;

public class PollValidator : IDisposable
{
    private const int SuccessfulPollsRequired = 2;
    private const int TimeoutFactor = 5;
    private readonly object _mutex = new();
    private bool _isRunning;
    private Task _t = Task.CompletedTask;
    private Thread _worker;

    /// <summary>
    /// Initializes a new instance of <see cref="PollValidator"/>.
    /// </summary>
    /// <param name="config"><see cref="Rs232Config"/>.</param>
    protected PollValidator(Rs232Config config)
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
            Logger?.Error("{0} Unable to close serial provider: {1}", GetType().Name, ex.Message);
        }
    }

    protected virtual bool SendPollMessage()
    {
        return false;
    }

    protected virtual bool SendMessage()
    {
        return false;
    }

    private void MainLoop()
    {
        while (true)
        {
            lock (_mutex)
            {
                if (!_isRunning)
                {
                    // TODO: Alter.
                    Logger.Debug("{0} received stop signal.", GetType().Name);
                    return;
                }
            }
        }

        _ = SendMessage();
        Thread.Sleep(Config.PollingPeriod);
    }
}