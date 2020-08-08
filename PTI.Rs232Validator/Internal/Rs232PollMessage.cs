namespace PTI.Rs232Validator.Internal
{
    /// <summary>
    ///     Message from host to device
    /// </summary>
    internal class Rs232PollMessage : Rs232BaseMessage
    {
        private static readonly byte[] BaseMessageNoAck =
        {
            0x02, 0x08, 0x10, 0x00, 0x00, 0x00, 0x03, 0x00
        };

        private static readonly byte[] BaseMessageAck =
        {
            0x02, 0x08, 0x11, 0x00, 0x00, 0x00, 0x03, 0x00
        };

        public Rs232PollMessage(bool ack) : base(ack ? BaseMessageAck : BaseMessageNoAck)
        {
        }

        public Rs232PollMessage SetEnableMask(byte mask)
        {
            RawMessage[3] = (byte) (mask & 0x7F);
            RawMessage[^1] = CalculateChecksum();
            return this;
        }

        public Rs232PollMessage SetEscrowMode(bool enabled)
        {
            var v = RawMessage[4];
            RawMessage[4] = (byte) (enabled ? v | 0x10 : v & ~0x10);
            RawMessage[^1] = CalculateChecksum();
            return this;
        }

        public Rs232PollMessage SetStack(bool doStack)
        {
            var v = RawMessage[4];

            // Clear both stack and return bits
            v = (byte) (v & ~0x60);

            // Set stack bit if requested
            if (doStack)
            {
                RawMessage[4] = (byte) (v | 0x20);
                RawMessage[^1] = CalculateChecksum();
            }

            return this;
        }

        public Rs232PollMessage SetReturn(bool doReturn)
        {
            var v = RawMessage[4];

            // Clear both stack and return bits
            v = (byte) (v & ~0x60);

            // Set return bit if requested
            if (doReturn)
            {
                RawMessage[4] = (byte) (v | 0x40);
                RawMessage[^1] = CalculateChecksum();
            }

            return this;
        }
    }
}