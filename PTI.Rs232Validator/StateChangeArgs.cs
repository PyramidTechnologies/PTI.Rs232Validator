namespace PTI.Rs232Validator
{
    using System;

    /// <summary>
    ///     State change information reports prior state and new state
    /// </summary>
    public class StateChangeArgs : EventArgs
    {
        /// <summary>
        ///     Create a new start transition event argument
        /// </summary>
        /// <param name="oldState">Device's most recent state</param>
        /// <param name="newState">Device current state</param>
        public StateChangeArgs(Rs232State oldState, Rs232State newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        /// <summary>
        ///     The most recent state of the device
        /// </summary>
        public Rs232State OldState { get; }

        /// <summary>
        ///     The new state of the device
        /// </summary>
        public Rs232State NewState { get; }
    }
}