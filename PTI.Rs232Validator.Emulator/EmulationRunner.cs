namespace PTI.Rs232Validator.Emulator
{
    using System;
    using System.Threading;
    using Providers;
    using Validators;

    /// <summary>
    ///     Helpers for running an emulator in deterministic sequences
    /// </summary>
    public class EmulationRunner<T> where T : class, IEmulator, ISerialProvider, new()
    {
        private readonly T _emulator;
        private readonly CancellationToken _token;

        /// <summary>
        ///     Create a new emulator with the specified polling period
        /// </summary>
        /// <param name="pollingPeriod">Main loop period</param>
        /// <param name="logger">Optional logger</param>
        public EmulationRunner(TimeSpan pollingPeriod, ILogger logger = null) : this(pollingPeriod,
            CancellationToken.None, logger)
        {
        }

        /// <summary>
        ///     Create a new emulator with the specified polling period
        /// </summary>
        /// <param name="pollingPeriod">Main loop period</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="logger">Optional logger</param>
        public EmulationRunner(TimeSpan pollingPeriod, CancellationToken token, ILogger logger = null)
        {
            _emulator = new T();
            _token = token;
            Config = new Rs232Config(_emulator)
            {
                Logger = logger,
                PollingPeriod = pollingPeriod
            };
        }

        /// <summary>
        ///     Device configuration
        /// </summary>
        public Rs232Config Config { get; }

        /// <summary>
        ///     Runs polling loop this many times at a minimum.
        ///     Due to timing limitations, this is a minimum run count
        ///     and their may be 1 or more extra loops executed.
        /// </summary>
        /// <param name="loops">Loops to run</param>
        /// <returns></returns>
        public T RunIdleFor(int loops)
        {
            // Setup a semaphore to wait for this many polling loops
            var sem = new EmulationLoopSemaphore(_token)
            {
                SignalAt = loops
            };
            _emulator.OnPollResponseSent += sem.LoopCallback;
            sem.OnLoopCalled += (sender, args) =>
            {
                // Always be idle
                _emulator.CurrentState = Rs232State.Idling;
            };

            // Create a new validator so we have perfect control of the state
            var validator = new ApexValidator(Config);
            validator.StartPollingLoop();

            // Wait for signal
            sem.Gate.WaitOne();

            // Cleanup
            validator.StopPollingLoop();
            _emulator.OnPollResponseSent -= sem.LoopCallback;

            return _emulator;
        }

        /// <summary>
        ///     Every n loops, issue the specified credit value
        /// </summary>
        /// <param name="loops">Count of loops between credits</param>
        /// <param name="count">Run this many times. -1 to loop forever.</param>
        /// <param name="creditIndices">Credits to issue</param>
        public T CreditEveryNLoops(int loops, int count, params byte[] creditIndices)
        {
            // Create a new validator so we have perfect control of the state
            var validator = new ApexValidator(Config);

            // Setup a semaphore to wait for this many polling loops
            var sem = new EmulationLoopSemaphore(_token)
            {
                SignalAt = loops * count
            };

            // Start in idling state
            _emulator.CurrentState = Rs232State.Idling;
            _emulator.CurrentEvents = Rs232Event.None;

            var next = 0;
            _emulator.OnPollResponseSent += sem.LoopCallback;
            sem.OnLoopCalled += (sender, args) =>
            {
                switch (_emulator.CurrentState)
                {
                    case Rs232State.None:
                        _emulator.CurrentState = Rs232State.Idling;
                        _emulator.CurrentEvents = Rs232Event.None;
                        break;

                    case Rs232State.Idling:
                        if (sem.Iterations % loops == 0)
                        {
                            _emulator.CurrentState = Rs232State.Accepting;
                            _emulator.CurrentEvents = Rs232Event.None;
                        }

                        break;

                    case Rs232State.Accepting:
                        _emulator.CurrentState = Rs232State.Stacking;
                        _emulator.CurrentEvents = Rs232Event.None;
                        break;

                    case Rs232State.Stacking:
                        _emulator.CurrentState = Rs232State.Idling;
                        _emulator.CurrentEvents = Rs232Event.Stacked;
                        _emulator.Credit = creditIndices[next++ % creditIndices.Length];
                        break;
                }
            };

            validator.StartPollingLoop();

            // Wait for signal
            sem.Gate.WaitOne();

            // Cleanup
            validator.StopPollingLoop();
            _emulator.OnPollResponseSent -= sem.LoopCallback;

            return _emulator;
        }
    }

    /// <summary>
    ///     Acts as a semaphore by signalling an event once
    ///     the specified count of iterations have been completed.
    /// </summary>
    internal class EmulationLoopSemaphore
    {
        private readonly CancellationToken _token;
        
        public EmulationLoopSemaphore(CancellationToken token)
        {
            _token = token;
        }
        
        /// <summary>
        ///     Signal after this many iterations
        /// </summary>
        public int SignalAt { get; set; }

        /// <summary>
        ///     Current loop iteration count
        ///     A negative value will loop forever
        /// </summary>
        public int Iterations { get; private set; }

        /// <summary>
        ///     Semaphore signalled upon completion
        /// </summary>
        public AutoResetEvent Gate { get; } = new AutoResetEvent(false);

        /// <summary>
        ///     Raised when <see cref="LoopCallback"/> is called
        /// </summary>
        public event EventHandler OnLoopCalled;

        /// <summary>
        ///     Handles emulator loop-complete callback
        /// </summary>
        public void LoopCallback(object sender, EventArgs e)
        {
            OnLoopCalled?.Invoke(this, EventArgs.Empty);

            ++Iterations;

            if (SignalAt > 0 && Iterations >= SignalAt)
            {
                Gate.Set();
            }

            if (_token.IsCancellationRequested)
            {
                Gate.Set();
            }
        }
    }
}