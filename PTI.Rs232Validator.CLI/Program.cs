namespace PTI.Rs232Validator.CLI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Emulator;

    internal static class Program
    {
        private static readonly string[] BillValues = {"Unknown", "$1", "$2", "$5", "$10", "$20", "$50", "$100"};

        private static CancellationTokenSource s_cancellationTokenSource;

        private static void Main(string[] args)
        {
            // Capture ctrl+c to stop process
            ConsoleInterrupt.SetConsoleCtrlHandler(ConsoleHandler, true);

            s_cancellationTokenSource = new CancellationTokenSource();

            var loggers = new List<ILogger>
            {
                new FileLogger("trace.log") {Level = 4},
                new FileLogger("debug.log") {Level = 3},
                new FileLogger("info.log") {Level = 2},
                new FileLogger("error.log") {Level = 1},
                new ConsoleLogger {Level = 4}
            };

            var logger = new MultiLogger(loggers);

            // RunEmulator(logger);

            RunValidator(logger, args);
        }

        /// <summary>
        ///     Runs a virtual bill validator, forever
        /// </summary>
        /// <param name="logger">Logger attaches to emulator</param>
        private static void RunEmulator(ILogger logger)
        {
            var runner = new EmulationRunner<ApexEmulator>(TimeSpan.FromMilliseconds(1),
                s_cancellationTokenSource.Token, logger);
            runner.CreditEveryNLoops(10, -1, 1, 2, 3, 4, 5, 6, 7);
        }

        /// <summary>
        ///     Run a real bill validator
        /// </summary>
        /// <param name="logger">Logger attaches to validator</param>
        /// <param name="args">Program arguments</param>
        private static void RunValidator(ILogger logger, string[] args)
        {            
            var portName = args.FirstOrDefault();
            if (string.IsNullOrEmpty(portName))
            {
                Console.WriteLine("Usage: rs232validator.cli.exe portName");
                return;
            }
            

            var config = Rs232Config.UsbRs232Config(portName, logger);
            
            var validator = new ApexValidator(config);

            validator.OnLostConnection += (sender, eventArgs) =>
            {
                config.Logger?.Error($"[APP] Lost connection to acceptor");
            };

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

            validator.OnCreditIndexReported += (sender, i) =>
            {
                config.Logger.Info($"[APP] Credit issued: {BillValues[i]}");
            };

            validator.OnStateChanged += (sender, state) =>
            {
                config.Logger.Info($"[APP] State changed from {state.OldState} to {state.NewState}");
            };

            validator.OnEventReported += (sender, evt) => { config.Logger.Info($"[APP] Event(s) reported: {evt}"); };

            validator.OnCashBoxRemoved += (sender, eventArgs) => { config.Logger.Info("[APP] Cash box removed"); };

            validator.OnCashBoxAttached += (sender, eventArgs) => { config.Logger.Info("[APP] Cash box attached"); };

            if (!validator.StartPollingLoop())
            {
                config.Logger.Error("[APP] Failed to start RS232 main loop");
                return;
            }

            config.Logger.Info("[APP] Validator is now running. CTRL+C to Exit");
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));

                if (!validator.IsUnresponsive)
                {
                    continue;
                }

                config.Logger?.Error("[APP] validator failed to start. Quitting now");

                validator.StopPollingLoop();

                break;
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
            
            // Cancel running tasks
            s_cancellationTokenSource.Cancel();

            // Yes, we handled the interrupt
            return true;
        }
    }
}