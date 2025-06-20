using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SoftBill
{
    public class Acceptor
    {
        // Constants
        private const double PowerUp = 0.4;
        private const double SoftDelay = 0.3;
        private const double Transition = 0.9;
        private const int CashboxSize = 250;
        private const int CheatRate = 50;

        public static bool Cheating { get; set; }

        public bool Running { get; private set; } = true;

        private byte _enables = 0x07;
        private bool _lrcOk = true;
        private readonly object _mutex = new();
        private byte _state = 0x01;
        private byte _event = 0x10;
        private byte _ext = 0x01;
        private byte _value = 0x00;
        private readonly byte _resd = 0x00;
        private readonly byte _model = 0x01;
        private readonly byte _rev = 0x01;
        private int _noteCount = 0;
        private bool _cheatFlag = false;
        private int _ack = -1;

        private readonly ConcurrentQueue<byte> _b0Ephemeral = new();
        private readonly ConcurrentQueue<byte> _b1Ephemeral = new();
        private readonly ConcurrentQueue<byte> _b2Ephemeral = new();

        private Task? _serialTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private byte[]? _lastMsg;
        private Monitor? _monitor;
        private readonly Random _random = new();

        public void EnableNote(int index)
        {
            if (index > 0 && index <= 7)
            {
                byte flag = (byte)Math.Pow(2, index - 1);
                _enables |= flag;
                Console.WriteLine($"Enabled note {index}");
            }
            else
            {
                Console.WriteLine($"Invalid enable {index}");
            }
        }

        public void DisableNote(int index)
        {
            if (index > 0 && index <= 7)
            {
                byte flag = (byte)Math.Pow(2, index - 1);
                _enables &= (byte)~flag;
                Console.WriteLine($"Disabled note {index}");
            }
            else
            {
                Console.WriteLine($"Invalid disable {index}");
            }
        }

        public void Start(string portName)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _monitor = new Monitor(5, TimedOut);
            _monitor.Start();

            // Simulate power up
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(PowerUp));
                _ext &= 0xFE; // Clear power up bit
            });

            _serialTask = Task.Run(() => SerialRunner(portName, _cancellationTokenSource.Token));
        }

        public void Stop()
        {
            Console.WriteLine("Shutting down...");
            Running = false;
            _cancellationTokenSource?.Cancel();
            _monitor?.Stop();
            _serialTask?.Wait();
        }

        public int ParseCommand(string cmd)
        {
            if (cmd == "Q") return 1;
            if (cmd is "?" or "H") return 2;
            if (cmd == "A") return 3;

            lock (_mutex)
            {
                // Handle bill feed command
                if (int.TryParse(cmd, out int val))
                {
                    byte flag = (byte)Math.Pow(2, val - 1);
                    if ((flag & _enables) != 0)
                    {
                        // Are we idle?
                        if ((_state & 0x01) == 1)
                        {
                            _ = Task.Run(() => StartAccepting(val));
                        }
                        else
                        {
                            // Double insertion - clear queue and return to idle
                            _b0Ephemeral.Clear();
                            _state = 0x01;
                            _b1Ephemeral.Enqueue(0x02);
                        }
                    }
                    else
                    {
                        if (val is 0 or > 7)
                        {
                            Console.WriteLine($"Invalid Bill Number {val}");
                        }
                        else
                        {
                            Console.WriteLine($"Note {val} disabled");
                            _b1Ephemeral.Enqueue(0x02);
                        }
                    }
                }
                // Handle enable/disable commands
                else if (cmd.Length == 2)
                {
                    if (cmd[0] == 'D' && int.TryParse(cmd[1].ToString(), out int disableIndex))
                    {
                        DisableNote(disableIndex);
                    }
                    else if (cmd[0] == 'E' && int.TryParse(cmd[1].ToString(), out int enableIndex))
                    {
                        EnableNote(enableIndex);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown E/D command {cmd}");
                    }
                }
                else
                {
                    switch (cmd)
                    {
                        case "C":
                            Cheating = !Cheating;
                            Console.WriteLine(Cheating 
                                ? $"Cheat Mode Enabled: {CheatRate}% Chance of Cheat"
                                : "Cheat Mode Disabled");
                            break;
                        case "R":
                            _b1Ephemeral.Enqueue(0x02);
                            break;
                        case "J":
                            _event ^= 0x04;
                            break;
                        case "F":
                            _event ^= 0x08;
                            break;
                        case "P":
                            _lrcOk = !_lrcOk;
                            break;
                        case "W":
                            _ext ^= 0x01;
                            break;
                        case "I":
                            _b2Ephemeral.Enqueue(0x02);
                            break;
                        case "X":
                            _b2Ephemeral.Enqueue(0x04);
                            break;
                        case "Y":
                            _noteCount = 0;
                            break;
                        case "L":
                            Console.WriteLine(Convert.ToString(_enables, 2).PadLeft(8, '0'));
                            break;
                        default:
                            Console.WriteLine($"Unknown Command: {cmd}");
                            break;
                    }
                }
            }

            return 0;
        }

        private async void SerialRunner(string portName, CancellationToken cancellationToken)
        {
            try
            {
                using var serialPort = new SerialPort(portName, 9600, Parity.Even, 7, StopBits.One);
                serialPort.Open();

                while (serialPort.IsOpen && Running && !cancellationToken.IsCancellationRequested)
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        var buffer = new byte[8];
                        int bytesRead = serialPort.Read(buffer, 0, 8);
                        if (bytesRead > 0)
                        {
                            _monitor?.Reset();
                            
                            lock (_mutex)
                            {
                                // Update enable/disable register
                                if (bytesRead > 3)
                                    _enables = buffer[3];

                                // Check and toggle ACK
                                byte mack = (byte)(buffer[2] & 1);
                                if (_ack == -1)
                                    _ack = mack;
                                _ack ^= 1;

                                // Build next message
                                var msg = GetMessage();
                                msg[2] |= mack;

                                // Check if we need to stack or return
                                AcceptOrReturn(buffer);

                                // Set checksum
                                msg[10] = (byte)(msg[1] ^ msg[2]);
                                for (int i = 3; i < 9; i++)
                                {
                                    msg[10] ^= msg[i];
                                }

                                // Clear value if idle
                                if (msg[3] == 0x01)
                                    _value = 0x00;

                                // Send message
                                serialPort.Write(msg, 0, msg.Length);
                                _lastMsg = msg;
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(SoftDelay), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial communication error: {ex.Message}");
            }
        }

        private void AcceptOrReturn(byte[] master)
        {
            if (master.Length < 5) return;
            
            byte cmd = master[4];
            if ((cmd & 0x20) == 0x20 && _state == 0x04)
            {
                AcceptBill();
            }
            else if ((cmd & 0x40) == 0x40 && _state == 0x04)
            {
                ReturnBill();
            }
        }

        private byte[] GetMessage()
        {
            CheckLrc();

            byte state = _state;
            byte evt = _event;
            byte ext = _ext;

            // Pull ephemerals from queues
            if (_b0Ephemeral.TryDequeue(out byte b0))
                state |= b0;
            if (_b1Ephemeral.TryDequeue(out byte b1))
                evt |= b1;
            if (_b2Ephemeral.TryDequeue(out byte b2))
                ext |= b2;

            var msg = new byte[] { 0x02, 0x0B, 0x20, state, evt, 
                                  (byte)(ext | (_value << 3)), _resd, _model, _rev, 0x03, 0x3A };

            // Clear cheat flag if event set
            if ((ext & 0x01) != 0)
                _cheatFlag = false;

            return msg;
        }

        private void CheckLrc()
        {
            if (_lrcOk)
                _event |= 0x10;
            else
                _event &= 0xEF;

            // Set stacker full if we have enough notes
            if (_noteCount >= CashboxSize)
                _event |= 0x08;
        }

        private async void StartAccepting(int val)
        {
            // If stacker is full, reject note
            if (_noteCount >= CashboxSize)
            {
                _event |= 0x08;
                _b1Ephemeral.Enqueue(0x02);
            }
            else
            {
                // Accepting
                _state = 0x02;

                if (Cheating)
                    Cheat();

                await Task.Delay(TimeSpan.FromSeconds(Transition));
                
                // Only enter escrow if cheat flag is not set
                if (!_cheatFlag)
                {
                    lock (_mutex)
                    {
                        _state = 0x04;
                        _value = (byte)val;
                    }
                }
                else
                {
                    // Return to idle, set reject flag
                    _state = 0x01;
                    _b1Ephemeral.Enqueue(0x02);
                    _cheatFlag = false;
                }
            }
        }

        private async void AcceptBill()
        {
            // Stacking
            _state = 0x08;
            await Task.Delay(TimeSpan.FromSeconds(Transition));
            
            // Stacked + Idle
            _b0Ephemeral.Enqueue(0x10);
            _state = 0x01;
            _noteCount++;
        }

        private async void ReturnBill()
        {
            // Returning
            _state = 0x20;
            await Task.Delay(TimeSpan.FromSeconds(Transition));
            
            // Returned + Idle
            _b0Ephemeral.Enqueue(0x50);
            _state = 0x01;
        }

        private void Cheat()
        {
            if (_random.Next(1, 101) <= CheatRate)
            {
                _b1Ephemeral.Enqueue(0x01);
                _cheatFlag = true;
            }
        }

        private void TimedOut()
        {
            Console.WriteLine("Comm timeout");
            // Effectively stop all acceptance
            _enables = 0;
        }
    }

    public static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _)) { }
        }
    }
}