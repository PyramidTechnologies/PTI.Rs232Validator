namespace PTI.Rs232Validator.Emulator
{
    using System;

    public interface IEmulator
    {
        /// <summary>
        ///     Total count of complete polling transactions
        ///     One transaction is host->device
        /// </summary>
        int TotalPollCount { get; }

        /// <summary>
        ///     When true, the cash box is reported as present
        /// </summary>
        /// <remarks>
        ///     The cash box state is always reported. For stackerless devices
        ///     this property is always true.
        /// </remarks>
        bool CashBoxPresent { get; set; }

        /// <summary>
        ///     Current acceptor state
        ///     Setting this property has no effect until the next polling message
        ///     is received.
        /// </summary>
        /// <remarks>State persists across polling messages</remarks>
        Rs232State CurrentState { get; set; }

        /// <summary>
        ///     Current acceptor events
        ///     Setting this property has no effect until the next polling message
        ///     is received.
        /// </summary>
        /// <remarks>Events are one-shot meaning that they are automatically cleared once sent</remarks>
        Rs232Event CurrentEvents { get; set; }

        /// <summary>
        ///     Next credit to report
        ///     Value must be in range (0,7)
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when value is greater than 7</exception>
        byte? Credit { get; set; }

        /// <summary>
        ///     Raised when this instance completes a poll response
        /// </summary>
        event EventHandler OnPollResponseSent;
    }
}