namespace PTI.Rs232Validator.CLI
{
    using System;
    using System.Linq;
    using System.Threading;

    internal static class Program
    {
        private static readonly string[] BillValues = {"Unknown", "$1", "$2", "$5", "$10", "$20", "$50", "$100"};

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

            var logger = new ConsoleLogger {Level = 2};
            var config = Rs232Config.UsbRs232Config(portName, logger);

            config.IsEscrowMode = true;

            RunValidator(config);
        }

        private static void RunValidator(Rs232Config config)
        {
            var validator = new ApexValidator(config);

            validator.OnBillInEscrow += (sender, i) =>
            {
                // For USA this index represent $20. This example will always return a $20
                // Alternatively you could set the Rs232Config mask to 0x5F to disable a 20.
                if (i == 5)
                {
                    config.Logger.Info($"[APP] Issuing a return command for this {BillValues[i]}");

                    validator.Return();
                }
                else
                {
                    config.Logger.Info($"[APP] Issuing stack command for this {BillValues[i]}");

                    validator.Stack();
                }
            };

            validator.OnCreditIndexReported += (sender, i) => { config.Logger.Info($"[APP] Credit issued: {BillValues[i]}"); };

            validator.OnStateChanged += (sender, state) =>
            {
                config.Logger.Info($"[APP] State changed from {state.OldState} to {state.NewState}");
            };

            validator.OnEventReported += (sender, evt) => { config.Logger.Info($"[APP] Event(s) reported: {evt}"); };

            validator.OnCashBoxRemoved += (sender, eventArgs) => { config.Logger.Info("[APP] Cash box removed"); };

            if (!validator.StartPollingLoop())
            {
                config.Logger.Error("[APP] Failed to start RS232 main loop");
                return;
            }

            config.Logger.Info("[APP] Validator is now running. CTRL+C to Exit");
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

            // Detach this handler
            ConsoleInterrupt.SetConsoleCtrlHandler(ConsoleHandler, false);

            // Yes, we handled the interrupt
            return true;
        }
    }
}