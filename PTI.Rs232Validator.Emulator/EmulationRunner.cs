namespace PTI.Rs232Validator.Emulator
{
    using System;
    using System.Threading;
    using Providers;

    /// <summary>
    ///     Helpers for running an emulator in deterministic sequences
    /// </summary>
    public class EmulationRunner<T> where T : class, IEmulator, ISerialProvider, new()
    {
        private readonly T _emulator;

        /// <summary>
        ///     Create a new emulator with the specified polling period
        /// </summary>
        /// <param name="pollingPeriod">Main loop period</param>
        public EmulationRunner(TimeSpan pollingPeriod)
        {
            _emulator = new T();
            Config = new Rs232Config(_emulator)
            {
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
            var sem = new EmulationLoopSemaphore
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
    }

    /// <summary>
    ///     Acts as a semaphore by signalling an event once
    ///     the specified count of iterations have been completed.
    /// </summary>
    internal class EmulationLoopSemaphore
    {
        /// <summary>
        ///     Signal after this many iterations
        /// </summary>
        public int SignalAt { get; set; }

        /// <summary>
        ///     Current loop iteration count
        /// </summary>
        public int Iterations { get; private set; }

        /// <summary>
        ///     Semaphore signalled upon completion
        /// </summary>
        public AutoResetEvent Gate { get; } = new AutoResetEvent(false);

        public event EventHandler OnLoopCalled;

        /// <summary>
        ///     Handles emulator loop-complete callback
        /// </summary>
        public void LoopCallback(object sender, EventArgs e)
        {
            OnLoopCalled?.Invoke(this, EventArgs.Empty);

            if (++Iterations >= SignalAt)
            {
                Gate.Set();
            }
        }
    }
}