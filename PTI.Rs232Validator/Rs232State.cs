namespace PTI.Rs232Validator
{
    public enum Rs232State
    {
        None,
        Idling,
        Accepting,
        Escrowed,
        Stacking,
        Returning,
        BillJammed,
        StackerFull,
        Failure
    }
}