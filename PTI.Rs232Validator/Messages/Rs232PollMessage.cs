namespace PTI.Rs232Validator.Messages
{
    /// <summary>
    ///     Message from host to device
    /// </summary>
    internal class Rs232PollMessage : Rs232BaseMessage
    {
        /// <summary>
        ///     Base !ACK message
        /// </summary>
        private static readonly byte[] BaseMessageNoAck =
        {
            0x02, 0x08, 0x10, 0x00, 0x00, 0x00, 0x03, 0x00
        };

        /// <summary>
        ///     Base ACK message
        /// </summary>
        private static readonly byte[] BaseMessageAck =
        {
            0x02, 0x08, 0x11, 0x00, 0x00, 0x00, 0x03, 0x00
        };

        private bool _stack;
        private bool _return;
        private bool _escrow;
        private byte _enableMask;

        /// <summary>
        ///     Create a new polling message in the specified ACK state
        /// </summary>
        /// <param name="ack">True to set ACK bit</param>
        public Rs232PollMessage(bool ack) : base(ack ? BaseMessageAck : BaseMessageNoAck)
        {
        }

        /// <summary>
        ///     Set enable flags
        ///     A bit mask representing which bills to accept
        ///     0b00000001: $1 or first note
        ///     0b00000010: $2 or second note
        ///     0b00000100: $5 or third note
        ///     0b00001000: $10 or fourth note
        ///     0b00010000: $20 or fifth note
        ///     0b00100000: $50 or sixth note
        ///     0b01000000: $100 of seventh note
        /// </summary>
        /// <param name="mask">Enable mask</param>
        /// <returns>this</returns>
        public Rs232PollMessage SetEnableMask(byte mask)
        {
            _enableMask = mask;
            RawMessage[3] = (byte) (mask & 0x7F);
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();
            return this;
        }

        /// <summary>
        ///     Set escrow mode bit
        /// </summary>
        /// <param name="enabled">True to enable escrow mode</param>
        /// <returns>this</returns>
        public Rs232PollMessage SetEscrowMode(bool enabled)
        {
            _escrow = enabled;
            var v = RawMessage[4];
            RawMessage[4] = (byte) (enabled ? v | 0x10 : v & ~0x10);
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();
            return this;
        }

        /// <summary>
        ///     Set stack bit
        /// </summary>
        /// <param name="doStack">true to perform stack</param>
        /// <remarks>Only used in escrow mode</remarks>
        /// <returns>this</returns>
        public Rs232PollMessage SetStack(bool doStack)
        {
            _stack = doStack;
            var v = RawMessage[4];

            // Set or clear the stack bit
            RawMessage[4] = (byte) (doStack ? v | 0x20 : v & ~0x20);
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();

            return this;
        }

        /// <summary>
        ///     Set return bit
        /// </summary>
        /// <param name="doReturn">true to perform bill return</param>
        /// <remarks>Only used in escrow mode</remarks>
        /// <returns>this</returns>
        public Rs232PollMessage SetReturn(bool doReturn)
        {
            _return = doReturn;
            var v = RawMessage[4];

            // Set or clear the return bit
            RawMessage[4] = (byte) (doReturn ? v | 0x40 : v & ~0x40);
            RawMessage[RawMessage.Length - 1] = CalculateChecksum();

            return this;
        }

        /// <summary>
        ///     Return poll message details as parsed bits
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Fixed width log entry
            return
                $"Ack: {Ack,5}, Enabled: 0b{_enableMask.ToBinary()}, Escrow: {_escrow,5}, Stack: {_stack,5}, Return: {_return,5}";
        }
    }
}