namespace PTI.Rs232Validator.CLI
{
    using System.Runtime.InteropServices;

    /// <summary>
    ///     CTRL+C console escape helper
    /// </summary>
    public static class ConsoleInterrupt
    {
        /// <summary>
        ///     Called when control event is received
        /// </summary>
        /// <param name="ctrlType">Type of control event</param>
        public delegate bool HandlerRoutine(CtrlTypes ctrlType);

        /// <summary>
        ///     Native control types
        /// </summary>
        public enum CtrlTypes
        {
            CtrlCEvent = 0,
            CtrlBreakEvent,
            CtrlCloseEvent,
            CtrlLogoffEvent = 5,
            CtrlShutdownEvent
        }

        /// <summary>
        ///     Add or remove a console control event handler
        /// </summary>
        /// <param name="handler">Callback</param>
        /// <param name="add">True to add, false to remove</param>
        /// <returns>true on success</returns>
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
    }
}