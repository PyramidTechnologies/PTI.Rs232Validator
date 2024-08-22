namespace PTI.Rs232Validator.Emulator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Messages;

    /// <summary>
    ///     Apex RS-232 poll response message
    /// </summary>
    internal class ApexDeviceMessage : Rs232Message
    {
        private const int AckByte = 2;
        private const int CashBoxByte = 4;
        private const int CashBoxBit = 4;
        private const int CreditByte = 5;

        /// <summary>
        ///     Standard device message
        /// </summary>
        private static readonly byte[] BaseMessage =
        {
            0x02, 0x0B, 0x20, 0x00, 0x00, 0x00, 0x00, 0x12, 0x13, 0x03, 0x3B
        };

        /// <summary>
        ///     State map keys by (byte, bit) into the payload.
        ///     e.g.
        ///     (1,2) => byte 1 bit 2 of payload
        /// </summary>
        private static readonly Dictionary<Rs232State, (byte, byte)> StateMap =
            new Dictionary<Rs232State, (byte, byte)>
            {
                {Rs232State.Idling, (3, 0)},
                {Rs232State.Accepting, (3, 1)},
                {Rs232State.Escrowed, (3, 2)},
                {Rs232State.Stacking, (3, 3)},
                {Rs232State.Returning, (3, 5)},
                {Rs232State.BillJammed, (4, 2)},
                {Rs232State.StackerFull, (4, 3)},
                {Rs232State.Failure, (5, 2)}
            };

        /// <summary>
        ///     Event map keys by (byte, bit) into the payload.
        ///     e.g.
        ///     (1,2) => byte 1 bit 2 of payload
        /// </summary>
        private static readonly Dictionary<Rs232Event, (byte, byte)> EventMap =
            new Dictionary<Rs232Event, (byte, byte)>
            {
                {Rs232Event.Stacked, (3, 4)},
                {Rs232Event.Returned, (3, 6)},
                {Rs232Event.Cheated, (4, 0)},
                {Rs232Event.BillRejected, (4, 1)},
                {Rs232Event.PowerUp, (5, 0)},
                {Rs232Event.InvalidCommand, (5, 1)}
            };

        /// <inheritdoc />
        public ApexDeviceMessage() : base(BaseMessage)
        {
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();
        }

        /// <summary>
        ///     Sets ack bit and recalculates checksum
        /// </summary>
        /// <param name="ack">True to set ack bit</param>
        /// <returns>this</returns>
        public ApexDeviceMessage SetAck(bool ack)
        {
            RawMessage[AckByte] = (byte) (ack ? 0x21 : 0x20);
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();
            return this;
        }

        /// <summary>
        ///     Sets state bit and recalculates checksum
        /// </summary>
        /// <param name="state">Which state to set</param>
        /// <returns>this</returns>
        public ApexDeviceMessage SetState(Rs232State state)
        {
            if (state == Rs232State.None)
            {
                return this;
            }

            var (index, bit) = StateMap[state];

            // Clear current state
            RawMessage[index] = 0;
            RawMessage[index] = SetBit(bit, RawMessage[index]);
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();

            return this;
        }

        /// <summary>
        ///     Sets event bit(s) and recalculates checksum
        /// </summary>
        /// <param name="events">Event(s) to set</param>
        /// <returns>this</returns>
        public ApexDeviceMessage SetEvents(Rs232Event events)
        {
            foreach (var evt in Enum.GetValues(typeof(Rs232Event)).Cast<Rs232Event>())
            {
                if (evt == Rs232Event.None)
                {
                    continue;
                }

                var (index, bit) = EventMap[evt];
                
                RawMessage[index] = events.HasFlag(evt)
                    ? SetBit(bit, RawMessage[index])
                    : ClearBit(bit, RawMessage[index]);
            }

            RawMessage[RawMessage.Length - 1] = CalculateChecksum();

            return this;
        }

        /// <summary>
        ///     Sets cash box present state recalculates checksum
        /// </summary>
        /// <param name="present">True to set cash box present</param>
        /// <returns>this</returns>
        public ApexDeviceMessage SetCashBoxState(bool present)
        {
            RawMessage[CashBoxByte] = present
                ? SetBit(CashBoxBit, RawMessage[CashBoxByte])
                : ClearBit(CashBoxBit, RawMessage[CashBoxByte]);

            RawMessage[RawMessage.Length - 1] = CalculateChecksum();

            return this;
        }

        /// <summary>
        ///     Sets credit index to report. Null for no credit to report.
        /// </summary>
        /// <param name="credit">Credit index in range (0,7)</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentException">Thrown if credit is greater than 7</exception>
        public ApexDeviceMessage SetCredit(int credit)
        {
            if (credit < 0 || credit > 7)
            {
                throw new ArgumentException($"Invalid credit value: {nameof(credit)}. Must in range (0,7).");
            }

            credit = (credit << 3) & 0b00111000;
            RawMessage[CreditByte] = (byte) credit;

            RawMessage[RawMessage.Length - 1] = CalculateChecksum();

            return this;
        }
    }
}