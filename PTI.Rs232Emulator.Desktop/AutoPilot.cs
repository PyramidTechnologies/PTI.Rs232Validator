using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoftBill
{
    public class AutoPilot
    {
        private static readonly string[] CommandPool = { "H", "1", "2", "3", "4", "5", "6", "7", "C", "R", "J", "F", "P", "W", "I", "X", "Y", "E", "D" };
        private readonly Random _random = new();
        private readonly BillAcceptor _acceptor;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _running;

        public bool Running => _running;

        public AutoPilot(BillAcceptor acceptor)
        {
            _acceptor = acceptor;
        }

        public void Start()
        {
            Console.WriteLine("Starting AutoPilot...");
            _running = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(DoWork, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            Console.WriteLine("Shutting down AutoPilot...");
            _running = false;
            _cancellationTokenSource?.Cancel();
        }

        private async Task DoWork()
        {
            try
            {
                while (_running && !_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    var cmd = CommandPool[_random.Next(CommandPool.Length)];
                    
                    // E and D commands require parameters
                    if (cmd is "E" or "D")
                    {
                        cmd += _random.Next(1, 8).ToString();
                    }

                    Console.WriteLine($"AutoPilot: {cmd}");
                    _acceptor.ParseCommand(cmd);

                    // Wait between 0.9 and 3.4 seconds
                    var sleepTime = _random.NextDouble() * 2.5 + 0.9;
                    await Task.Delay(TimeSpan.FromSeconds(sleepTime), _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            
            Console.WriteLine("AutoPilot Terminated!");
        }
    }
}