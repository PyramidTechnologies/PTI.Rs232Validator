namespace PTI.Rs232Validator.Messages
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Base RS-232 message
    ///     All message have an ACK and host bit
    /// </summary>
    public abstract class Rs232Message
    {
        /// <summary>
        ///     Create a new message from raw data
        /// </summary>
        /// <param name="payload">raw message data</param>
        protected Rs232Message(byte[] payload)
        {
            RawMessage = payload;

            // If there is data, the 3rd byte is the msg type and ack byte
            if (payload.Length <= 2)
            {
                return;
            }
            
            MessageType = (Rs232MessageType)(payload[2] >> 4 & 0b111);

            // ACK toggles with each successfully message
            Ack = (payload[2] & 1) == 1;
        }

        /// <summary>
        ///     TODO: Add description.
        /// </summary>
        public Rs232MessageType MessageType { get; }

        /// <summary>
        ///     Toggle bit state
        /// </summary>
        public bool Ack { get; }

        /// <summary>
        ///     Original raw message
        /// </summary>
        protected byte[] RawMessage { get; }

        /// <summary>
        ///     Returns a copy of the original message data
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            return (byte[])RawMessage.Clone();
        }

        /// <summary>
        ///     Calculate and return the RS-232 checksum for this message
        /// </summary>
        /// <returns>XOR 1-byt checksum</returns>
        protected byte CalculateChecksum()
        {
            // No packet can have less than this many bytes
            if (RawMessage.Length < 5)
            {
                return 0;
            }

            byte checksum = 0;
            for (var i = 1; i < RawMessage.Length - 2; ++i)
            {
                checksum ^= RawMessage[i];
            }

            return checksum;
        }

        /// <summary>
        ///     Returns true if bit is set in value
        /// </summary>
        /// <param name="bit">0-based bit to test</param>
        /// <param name="value">value to test</param>
        /// <returns>true if bit is set</returns>
        protected static bool IsBitSet(int bit, byte value)
        {
            return (value & (1 << bit)) == 1 << bit;
        }

        /// <summary>
        ///     Returns true if any bits are set in value
        /// </summary>
        /// <param name="bits">0-based bits to test</param>
        /// <param name="value">value to test</param>
        /// <returns>true if any bit from bits are set</returns>
        protected static bool AreAnyBitsSet(IEnumerable<int> bits, byte value)
        {
            return bits.Any(b => IsBitSet(b, value));
        }


        /// <summary>
        ///     Returns true if all bits are set in value
        /// </summary>
        /// <param name="bits">0-based bits to test</param>
        /// <param name="value">value to test</param>
        /// <returns>true if all bits from bits are set</returns>
        protected static bool AreAllBitsSet(IEnumerable<int> bits, byte value)
        {
            return bits.All(b => IsBitSet(b, value));
        }

        /// <summary>
        ///     Return value with bit set
        /// </summary>
        /// <param name="bit">0-based bit to set</param>
        /// <param name="value">Value to set</param>
        /// <returns>Value with bit set</returns>
        protected static byte SetBit(int bit, byte value)
        {
            return (byte)(value | (1 << bit));
        }

        /// <summary>
        ///     Return value with bit cleared
        /// </summary>
        /// <param name="bit">bit to clear</param>
        /// <param name="value">Value to clear</param>
        /// <returns>Value with bit cleared</returns>
        protected static byte ClearBit(int bit, byte value)
        {
            return (byte)(value & ~(1 << bit));
        }
    }
}