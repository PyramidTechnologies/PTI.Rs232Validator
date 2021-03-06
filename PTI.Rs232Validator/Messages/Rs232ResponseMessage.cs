namespace PTI.Rs232Validator.Messages
{
    using System.Collections.Generic;

    internal abstract class Rs232ResponseMessage : Rs232BaseMessage
    {
        protected readonly IList<string> PacketIssues = new List<string>();

        protected Rs232ResponseMessage(byte[] messageData) : base(messageData)
        {
        }

        /// <summary>
        ///     True if packet is well-formed
        /// </summary>
        public bool IsValid { get; protected set; }
        
        /// <summary>
        ///     True when...
        ///     * Packet is correct length with correct checksum AND
        ///     * one or more reserved bits set OR
        ///     * more than one state is set OR
        ///     * a state is missing an accompanying but (e.g. stack+credit) 
        /// </summary>
        public bool HasProtocolViolation { get; protected set; }

        /// <summary>
        ///     Credit index, if any, from this message
        ///     Will be null if <see cref="IsValid" /> is false
        /// </summary>
        public int? CreditIndex { get; protected set; }

        /// <summary>
        ///     State reported by acceptor
        /// </summary>
        public Rs232State State { get; protected set; }

        /// <summary>
        ///     Events reported in this message
        /// </summary>
        public Rs232Event Event { get; protected set; }

        /// <summary>
        ///     If true, cash box is attached
        /// </summary>
        /// <remarks>For stackerless models, this will always be true</remarks>
        public bool IsCashBoxPresent { get; protected set; }

        /// <summary>
        ///     If true, the device might be busy and unable to respond
        /// </summary>
        public bool IsEmptyResponse { get; protected set; }

        /// <summary>
        ///     Acceptor model
        /// </summary>
        public abstract int Model { get; protected internal set; }

        /// <summary>
        ///     Firmware revision
        ///     1.17 return 17
        /// </summary>
        public abstract int Revision { get; protected internal set; }


        /// <summary>
        ///     List of packet issues
        /// </summary>
        public IEnumerable<string> AllPacketIssues => PacketIssues;

        /// <summary>
        ///     Formats the decoded bits from the data
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Fixed width log entry
            return !IsValid
                ? "Invalid Poll Response"
                : $"State: {State,12}, Event(s): {Event,-24}, Credit: {CreditIndex,4}, Model: 0x{Model:X2}, Rev.: 0x{Revision:X2}, CB Present: {IsCashBoxPresent,5}";
        }
    }
}