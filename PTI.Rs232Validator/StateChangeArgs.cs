namespace PTI.Rs232Validator
{
    using System;

    public class StateChangeArgs : EventArgs
    {
        public StateChangeArgs(Rs232State oldState, Rs232State newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public Rs232State OldState { get; }

        public Rs232State NewState { get; }
    }
}