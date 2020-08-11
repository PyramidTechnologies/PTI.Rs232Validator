namespace PTI.Rs232Validator.CLI
{
    using System;
    using System.Linq;
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

            var logger = new ConsoleLogger {Level = 4};
            var config = Rs232Config.UsbRs232Config(portName, logger);

            config.IsEscrowMode = true;

            RunValidator(config);
        }

        private static void RunValidator(Rs232Config config)
        {
            var validator = new ApexValidator(config);

            validator.OnBillInEscrow += (sender, i) => { validator.Stack(); };

            validator.OnCreditIndexReported += (sender, i) => { Console.WriteLine($"Credit issued: {i}"); };

            validator.OnStateChanged += (sender, state) =>
            {
                Console.WriteLine($"State changed from {state.OldState} to {state.NewState}");
            };

            validator.OnEventReported += (sender, evt) => { Console.WriteLine($"Event(s) reported: {evt}"); };

            validator.OnCashBoxRemoved += (sender, eventArgs) => { Console.WriteLine("Cash box removed"); };

            if (!validator.StartPollingLoop(TokenSource.Token))
            {
                Console.WriteLine("Failed to start RS232 main loop");
                return;
            }

            Console.WriteLine("Validator is now running. CTRL+C to Exit");
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