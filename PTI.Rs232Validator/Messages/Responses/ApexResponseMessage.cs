namespace PTI.Rs232Validator.Messages
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Message from Apex in response to a host poll message
    /// </summary>
    internal class ApexResponseMessage : Rs232ResponseMessage
    {
        private const int CashBoxBit = 4;
        private static readonly int[] CreditBits = {3, 4, 5};

        /// <summary>
        ///     State map keys by (byte, bit) into the payload.
        ///     e.g.
        ///     (1,2) => byte 1 bit 2 of payload
        /// </summary>
        private static readonly Dictionary<(byte, byte), Rs232State> StateMap =
            new Dictionary<(byte, byte), Rs232State>
            {
                {(0, 0), Rs232State.Idling},
                {(0, 1), Rs232State.Accepting},
                {(0, 2), Rs232State.Escrowed},
                {(0, 3), Rs232State.Stacking},
                {(0, 5), Rs232State.Returning},
                {(1, 2), Rs232State.BillJammed},
                {(1, 3), Rs232State.StackerFull},
                {(2, 2), Rs232State.Failure}
            };

        /// <summary>
        ///     Event map keys by (byte, bit) into the payload.
        ///     e.g.
        ///     (1,2) => byte 1 bit 2 of payload
        /// </summary>
        private static readonly Dictionary<(byte, byte), Rs232Event> EventMap =
            new Dictionary<(byte, byte), Rs232Event>
            {
                {(0, 4), Rs232Event.Stacked},
                {(0, 6), Rs232Event.Returned},
                {(1, 0), Rs232Event.Cheated},
                {(1, 1), Rs232Event.BillRejected},
                {(2, 0), Rs232Event.PowerUp},
                {(2, 1), Rs232Event.InvalidCommand}
            };

        /// <summary>
        ///     Map of reserved bits for each byte index of payload
        /// </summary>
        private static readonly Dictionary<int, int[]> ReservedBits = new Dictionary<int, int[]>
        {
            {0, new[] {7}},
            {1, new[] {5, 6, 7}},
            {2, new[] {6, 7}},
            {3, new[] {0, 1, 2, 3, 4, 5, 6, 7}},
            {4, new[] {7}},
            {5, new[] {7}}
        };

        /// <summary>
        ///     Holds a copy of the just data portion of the device's response
        /// </summary>
        private readonly byte[] _payload;

        /// <summary>
        ///     Create and parse a new response message
        /// </summary>
        /// <param name="data">Device response data</param>
        public ApexResponseMessage(byte[] data) : base(data)
        {
            if (data is null)
            {
                IsEmptyResponse = true;
            }
            else if (data.Length == 11)
            {
                _payload = data.Skip(3).Take(6).ToArray();

                IsValid = Parse();
            }
        }

        /// <inheritdoc />
        public override int Model { get; protected internal set; }

        /// <inheritdoc />
        public override int Revision { get; protected internal set; }

        /// <summary>
        ///     Fully parse message data
        /// </summary>
        /// <returns>True if message was fully parsed</returns>
        private bool Parse()
        {
            if (RawMessage is null)
            {
                PacketIssues.Add("Empty packet");

                IsEmptyResponse = true;

                // Return early, nothing to parse
                return false;
            }

            if (RawMessage.Length != 11)
            {
                PacketIssues.Add($"Packet length is {RawMessage.Length}, expected 11");

                // Return early, not enough to parse
                return false;
            }

            var actualChecksum = CalculateChecksum();
            var expectedChecksum = RawMessage[RawMessage.Length - 1];
            if (actualChecksum != expectedChecksum)
            {
                PacketIssues.Add($"Packet checksum is {actualChecksum}, expected {expectedChecksum}");

                // Return early, can't trust the data
                return false;
            }

            // Extract state
            var states = new List<Rs232State>(8);
            foreach (var kv in StateMap)
            {
                var index = kv.Key.Item1;
                var bit = kv.Key.Item2;

                if (!IsBitSet(bit, _payload[index]))
                {
                    continue;
                }

                State = kv.Value;
                states.Add(kv.Value);
            }

            // Extract events
            foreach (var kv in EventMap)
            {
                var index = kv.Key.Item1;
                var bit = kv.Key.Item2;
                if (IsBitSet(bit, _payload[index]))
                {
                    Event |= kv.Value;
                }
            }

            // Check cash box presence
            IsCashBoxPresent = IsBitSet(CashBoxBit, _payload[1]);

            // Extract model
            Model = _payload[4];

            // Extract revision
            Revision = _payload[5];

            // Check all bytes for reserved bits
            foreach (var kv in ReservedBits)
            {
                var index = kv.Key;
                var bits = kv.Value;

                if (!AreAnyBitsSet(bits, _payload[index]))
                {
                    continue;
                }

                PacketIssues.Add($"Byte {index} has one more reserved bits set ({string.Join(",", bits)}");
                HasProtocolViolation = true;
            }

            // Having not state is a violation
            if (states.Count == 0)
            {
                PacketIssues.Add("No state bit set");
                HasProtocolViolation = true;
            }
            // Have more than one state is a violation
            else if (states.Count > 1)
            {
                PacketIssues.Add($"More than one state set: {string.Join(",", states.Select(x => x.ToString()))}");
                HasProtocolViolation = true;
            }

            if (!HasProtocolViolation)
            {
                CreditIndex = AreAnyBitsSet(CreditBits, _payload[2]) ? _payload[2] >> 3 : (int?) null;
            }

            return !HasProtocolViolation;
        }
    }
}