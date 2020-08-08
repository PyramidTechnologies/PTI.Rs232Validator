namespace PTI.Rs232Validator
{
    using System;

    [Flags]
    public enum Rs232Event
    {
        None = 0,
        Stacked = 1 << 0,
        Returned = 1 << 1,
        Cheated = 1 << 2,
        BillRejected = 1 << 3,
        InvalidCommand = 1 << 4,
        PowerUp = 1 << 5
    }
}