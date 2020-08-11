namespace PTI.Rs232Validator.Messages
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Base RS-232 message
    ///     All message have an ACK and host bit
    /// </summary>
    public abstract class Rs232BaseMessage
    {
        /// <summary>
        ///     Create a new message from raw data
        /// </summary>
        /// <param name="messageData">raw message data</param>
        protected Rs232BaseMessage(byte[] messageData)
        {
            RawMessage = messageData;

            // If there is data, the 3rd byte is the msg type and ack byte
            if (!(messageData is null) && messageData.Length > 2)
            {
                // Host: 0x10, Device: 0x20 
                IsHostMessage = messageData[2] >> 4 == 1;

                // ACK toggles with each successfully message
                Ack = (messageData[2] & 1) == 1;
            }
        }

        /// <summary>
        ///     True if this is a host message
        /// </summary>
        public bool IsHostMessage { get; }

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
            return (byte[]) RawMessage.Clone();
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
        protected bool IsBitSet(int bit, byte value)
        {
            return (value & (1 << bit)) == 1 << bit;
        }
        
        /// <summary>
        ///     Returns true if any bits are set in value
        /// </summary>
        /// <param name="bits">0-based bits to test</param>
        /// <param name="value">value to test</param>
        /// <returns>true if any bit from bits are set</returns>
        protected bool AreAnyBitsSet(IEnumerable<int> bits, byte value)
        {
            return bits.Any(b => IsBitSet(b, value));
        }
        
                
        /// <summary>
        ///     Returns true if all bits are set in value
        /// </summary>
        /// <param name="bits">0-based bits to test</param>
        /// <param name="value">value to test</param>
        /// <returns>true if all bits from bits are set</returns>
        protected bool AreAllBitsSet(IEnumerable<int> bits, byte value)
        {
            return bits.All(b => IsBitSet(b, value));
        }
        
        /// <summary>
        ///     Return value with bit set
        /// </summary>
        /// <param name="bit">0-based bit to set</param>
        /// <param name="value">Value to set</param>
        /// <returns>Value with bit set</returns>
        protected byte SetBit(int bit, byte value)
        {
            return (byte) (value | (1 << bit));
        }

        /// <summary>
        ///     Return value with bit cleared
        /// </summary>
        /// <param name="bit">bit to clear</param>
        /// <param name="value">Value to clear</param>
        /// <returns>Value with bit cleared</returns>
        protected byte ClearBit(int bit, byte value)
        {
            return (byte) (value & ~(1 << bit));
        }
    }
}