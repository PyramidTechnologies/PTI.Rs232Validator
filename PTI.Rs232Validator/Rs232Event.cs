namespace PTI.Rs232Validator
{
    using System;

    /// <summary>
    ///     RS-232 bill validator events.
    ///     By definition, events are reported exactly
    ///     once per occurence.
    /// </summary>
    [Flags]
    public enum Rs232Event : byte
    {
        /// <summary>
        ///     No event flags are set
        /// </summary>
        None = 0,

        /// <summary>
        ///     A bill has been stacked
        /// </summary>
        Stacked = 1 << 0,

        /// <summary>
        ///     A bill has been returned
        /// </summary>
        Returned = 1 << 1,

        /// <summary>
        ///     A cheat attempt has been detected and blocked
        /// </summary>
        Cheated = 1 << 2,

        /// <summary>
        ///     A bill has been rejected
        /// </summary>
        BillRejected = 1 << 3,

        /// <summary>
        ///     An invalid host command has been received
        /// </summary>
        InvalidCommand = 1 << 4,

        /// <summary>
        ///     The bill validator just powered up
        /// </summary>
        PowerUp = 1 << 5
    }
}