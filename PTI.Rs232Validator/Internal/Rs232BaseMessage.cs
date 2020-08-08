namespace PTI.Rs232Validator.Internal
{
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class Rs232BaseMessage
    {
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

        protected byte[] RawMessage { get; }

        public byte[] Serialize()
        {
            return (byte[]) RawMessage.Clone();
        }

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

        protected bool IsBitSet(int bit, byte value)
        {
            return (value & (1 << bit)) == 1 << bit;
        }

        protected bool AreBitsSet(IEnumerable<int> bits, byte value)
        {
            return bits.Any(b => IsBitSet(b, value));
        }
    }
}