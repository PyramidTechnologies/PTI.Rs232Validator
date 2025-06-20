using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoftBill
{
    public class Monitor
    {
        private readonly double _interval;
        private readonly Action _deadFunction;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _expired;

        public Monitor(double interval, Action deadFunction)
        {
            _interval = interval;
            _deadFunction = deadFunction;
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(DoMonitor, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void Reset()
        {
            _expired = false;
        }

        private async Task DoMonitor()
        {
            try
            {
                while (!_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    _expired = true;
                    await Task.Delay(TimeSpan.FromSeconds(_interval), _cancellationTokenSource.Token);
                    
                    if (_expired && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _deadFunction();
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }
}