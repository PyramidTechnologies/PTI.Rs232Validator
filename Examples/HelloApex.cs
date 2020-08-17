namespace HelloApex
{
    using System;
    using System.Linq;
    using System.Threading;
    using PTI.Rs232Validator;
    using PTI.Rs232Validator.CLI;

    internal static class Program
    {
        private static readonly string[] BillValues = {"Unknown", "$1", "$2", "$5", "$10", "$20", "$50", "$100"};

        private static void Main(string[] args)
        {
            var portName = args.FirstOrDefault();
            if (string.IsNullOrEmpty(portName))
            {
                Console.WriteLine("Usage: app portName");
                return;
            }

            var logger = new ConsoleLogger {Level = 3};
            var config = Rs232Config.UsbRs232Config(portName, logger);

            var validator = new ApexValidator(config);

            validator.OnLostConnection += (sender, eventArgs) =>
            {
                config.Logger?.Error($"[APP] Lost connection to acceptor");
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
    }

    /// <summary>
    ///     Color console logger
    /// </summary>
    internal class ConsoleLogger : BaseLogger
    {
        /// <inheritdoc />
        public override void Trace(string format, params object[] args)
        {
            if (Level < 4)
            {
                return;
            }

            Log("TRACE", ConsoleColor.DarkGray, format, args);
        }

        /// <inheritdoc />
        public override void Debug(string format, params object[] args)
        {
            if (Level < 3)
            {
                return;
            }

            Log("DEBUG", ConsoleColor.Gray, format, args);
        }

        /// <inheritdoc />
        public override void Info(string format, params object[] args)
        {
            if (Level < 2)
            {
                return;
            }

            Log("INFOR", ConsoleColor.White, format, args);
        }

        /// <inheritdoc />
        public override void Error(string format, params object[] args)
        {
            if (Level < 1)
            {
                return;
            }

            Log("ERROR", ConsoleColor.Red, format, args);
        }

        private void Log(string level, ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.Write($"[{level}] {DateTime.Now - Epoch}::");
            Console.WriteLine(format, args);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}