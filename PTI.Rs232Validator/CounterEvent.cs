namespace PTI.Rs232Validator
{
    using System;
    using System.Threading;

    /// <summary>
    ///     A single-use event that signals after the specified number of signals
    /// </summary>
    internal class CounterEvent
    {
        private readonly AutoResetEvent _event;
        private readonly int _signalAt;
        private int _counter;
        private bool _executed;

        public CounterEvent(int signalAt)
        {
            _signalAt = signalAt;
            _event = new AutoResetEvent(false);
        }

        /// <summary>
        ///     Signal the event
        /// </summary>
        public void Set()
        {
            if (_executed)
            {
                return;
            }

            ++_counter;
            
            if (_counter != _signalAt)
            {
                return;
            }

            _event.Set();
            _executed = true;
        }

        /// <summary>
        ///     Wait for event to be signalled
        /// </summary>
        /// <param name="timeSpan">Timeout</param>
        /// <returns>true if event is signalled before timeout</returns>
        public bool WaitOne(TimeSpan timeSpan)
        {
            return _event.WaitOne(timeSpan);
        }
    }
}