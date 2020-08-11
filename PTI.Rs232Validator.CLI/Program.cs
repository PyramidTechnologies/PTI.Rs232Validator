namespace PTI.Rs232Validator.CLI
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal static class Program
    {
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        private static void Main(string[] args)
        {
            var portName = args.FirstOrDefault();
            if (string.IsNullOrEmpty(portName))
            {
                Console.WriteLine("Usage: rs232validator.cli.exe portName");
                return;
            }

            // Capture ctrl+c to stop process
            ConsoleInterrupt.SetConsoleCtrlHandler(ConsoleHandler, true);

            var config = Rs232Config.UsbRs232Config(
                portName,
                new ConsoleLogger {Level = 4});

            var validator = new ApexValidator(config);

            validator.OnCreditReported += (sender, i) => { Console.WriteLine($"Credit reported: {i}"); };

            validator.OnStateChanged += (sender, state) =>
            {
                Console.WriteLine($"State changed from {state.OldState} tp {state.NewState}");
            };

            validator.OnEventReported += (sender, evt) => { Console.WriteLine($"Event(s) reported: {evt}"); };

            validator.OnCashBoxRemoved += (sender, eventArgs) => { Console.WriteLine("Cash box removed"); };

            if (!validator.StartPollingLoop(TokenSource.Token))
            {
                Console.WriteLine("Failed to start RS232 main loop");
                return;
            }

            Console.WriteLine("CTRL+C to Exit");
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
        }

        /// <summary>
        ///     Console interrupt handler
        /// </summary>
        /// <param name="ctrl">Console control code</param>
        /// <returns>true if interrupt was handled, false otherwise</returns>
        private static bool ConsoleHandler(ConsoleInterrupt.CtrlTypes ctrl)
        {
            // Only handle ctrl+c
            if (ctrl != ConsoleInterrupt.CtrlTypes.CtrlCEvent)
            {
                return false;
            }

            // Stop the task
            TokenSource.Cancel();

            // Detach this handler
            ConsoleInterrupt.SetConsoleCtrlHandler(ConsoleHandler, false);

            // Yes, we handled the interrupt
            return true;
        }
    }
}