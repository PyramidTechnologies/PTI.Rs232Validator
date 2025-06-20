using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SoftBill
{
    public enum BillAcceptorState
    {
        Idling = 0x01,
        Accepting = 0x02,
        Escrowed = 0x04,
        Stacking = 0x08,
        Stacked = 0x10,
        Returning = 0x20,
        Returned = 0x40
    }

    public class BillAcceptor
    {
        // Constants from documentation
        private const int BAUD_RATE = 9600;
        private const Parity PARITY = Parity.Even;
        private const int DATA_BITS = 7;
        private const StopBits STOP_BITS = StopBits.One;
        private const int COMM_TIMEOUT = 5000; // 5 seconds
        private const double STATE_TRANSITION_TIME = 0.9; // seconds
        private const int CASHBOX_SIZE = 250;

        // Protocol constants
        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const byte MSG_LENGTH = 0x0B;
        private const byte MSG_TYPE_SLAVE = 0x02; // MSG Type = 2 for slave to master (bits 4-6)
        private const byte MODEL_NUMBER = 0x01;
        private const byte FIRMWARE_REVISION = 0x01;

        // Events
        public event Action<byte[], bool>? MessageReceived; // message, isSent
        public event Action<BillAcceptorState, int>? StateChanged; // state, billValue

        // State variables
        public bool Running { get; private set; } = true;
        private SerialPort? _serialPort;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _communicationTask;
        private readonly object _stateLock = new();

        // Acceptor state (matches documentation exactly)
        private byte _acceptorState = (byte)BillAcceptorState.Idling;
        private byte _statusEvents = 0x10; // Cassette present by default
        private byte _powerValue = 0x01; // Power up bit set initially
        private readonly byte _reserved = 0x00;

        // Bill handling
        private int _currentBillValue = 0;
        private int _noteCount = 0;
        private byte _billEnables = 0x7F; // All bills enabled by default

        // Communication
        private byte _lastMasterAck = 0;
        private DateTime _lastPollTime = DateTime.Now;
        private bool _escrowEnabled = true; // Track if master has escrow enabled

        // One-time event queues (cleared after one transmission)
        private readonly ConcurrentQueue<byte> _oneTimeStateEvents = new();
        private readonly ConcurrentQueue<byte> _oneTimeStatusEvents = new();
        private readonly ConcurrentQueue<byte> _oneTimePowerEvents = new();

        // Random for cheat simulation
        private readonly Random _random = new();
        public static bool CheatModeEnabled { get; set; } = false;
        private const int CHEAT_RATE = 50; // 50% chance

        public void Start(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, BAUD_RATE, PARITY, DATA_BITS, STOP_BITS)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                _serialPort.Open();
                _cancellationTokenSource = new CancellationTokenSource();
                _communicationTask = Task.Run(CommunicationLoop);

                // Simulate power-up sequence (3 seconds as per spec)
                Task.Run(async () =>
                {
                    await Task.Delay(3000); // 3 second power up time as per spec
                    lock (_stateLock)
                    {
                        _powerValue &= 0xFE; // Clear power up bit
                    }
                });

                Running = true;
                OnStateChanged(BillAcceptorState.Idling, 0);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start on {portName}: {ex.Message}", ex);
            }
        }

        public void Stop()
        {
            Running = false;
            _cancellationTokenSource?.Cancel();
            _communicationTask?.Wait(1000);
            _serialPort?.Close();
            _serialPort?.Dispose();
        }

        public void SendReset()
        {
            // Send reset sequence: 0x7F 0x7F 0x7F (don't acknowledge this command)
            var resetMessage = new byte[] { 0x7F, 0x7F, 0x7F };
            _serialPort?.Write(resetMessage, 0, resetMessage.Length);
            OnMessageReceived(resetMessage, true);

            // Perform reset after sending
            Task.Run(async () =>
            {
                await Task.Delay(100);
                PerformReset();
            });
        }

        private void PerformReset()
        {
            lock (_stateLock)
            {
                _acceptorState = (byte)BillAcceptorState.Idling;
                _statusEvents = 0x10; // Cassette present
                _powerValue = 0x01; // Power up bit
                _currentBillValue = 0;
                _noteCount = 0;
                _billEnables = 0x7F;
                _lastMasterAck = 0;
                _escrowEnabled = true; // Default to escrow enabled

                // Clear queues
                while (_oneTimeStateEvents.TryDequeue(out _)) { }
                while (_oneTimeStatusEvents.TryDequeue(out _)) { }
                while (_oneTimePowerEvents.TryDequeue(out _)) { }

                OnStateChanged(BillAcceptorState.Idling, 0);
            }

            // Clear power up bit after 3 second initialization period
            Task.Run(async () =>
            {
                await Task.Delay(3000); // 3 second initialization as per spec
                lock (_stateLock)
                {
                    _powerValue &= 0xFE; // Clear power up bit
                }
            });
        }

        public int ParseCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return 0;

            command = command.ToUpper().Trim();

            lock (_stateLock)
            {
                switch (command)
                {
                    case "Q": return 1; // Quit
                    case "?" or "H": return 2; // Help
                    case "A": return 3; // AutoPilot

                    // Bill insertion commands (1-7)
                    case var cmd when int.TryParse(cmd, out int billNum) && billNum >= 1 && billNum <= 7:
                        InsertBill(billNum);
                        break;

                    // Enable/Disable commands
                    case var cmd when cmd.Length == 2 && cmd[0] == 'E' && int.TryParse(cmd[1].ToString(), out int enableNum):
                        EnableBill(enableNum);
                        break;
                    case var cmd when cmd.Length == 2 && cmd[0] == 'D' && int.TryParse(cmd[1].ToString(), out int disableNum):
                        DisableBill(disableNum);
                        break;

                    // Event commands
                    case "C": // Toggle cheat mode
                        CheatModeEnabled = !CheatModeEnabled;
                        break;
                    case "R": // Reject
                        _oneTimeStatusEvents.Enqueue(0x02);
                        break;
                    case "J": // Toggle jam
                        _statusEvents ^= 0x04;
                        break;
                    case "F": // Toggle stacker full
                        _statusEvents ^= 0x08;
                        break;
                    case "P": // Toggle cassette present
                        _statusEvents ^= 0x10;
                        break;
                    case "W": // Power up event
                        _oneTimePowerEvents.Enqueue(0x01);
                        break;
                    case "I": // Invalid command
                        _oneTimePowerEvents.Enqueue(0x02);
                        break;
                    case "X": // Failure
                        _oneTimePowerEvents.Enqueue(0x04);
                        break;
                    case "Y": // Empty cashbox
                        _noteCount = 0;
                        break;
                    case "L": // Show enables
                        Console.WriteLine($"Bill enables: {Convert.ToString(_billEnables, 2).PadLeft(8, '0')}");
                        break;
                }
            }

            return 0;
        }

        private void InsertBill(int billValue)
        {
            // Check if this bill denomination is enabled
            byte billMask = (byte)(1 << (billValue - 1));
            if ((_billEnables & billMask) == 0)
            {
                _oneTimeStatusEvents.Enqueue(0x02); // Reject
                return;
            }

            // Check if acceptor is idle
            if (_acceptorState != (byte)BillAcceptorState.Idling)
            {
                _oneTimeStatusEvents.Enqueue(0x02); // Reject - not idle
                return;
            }

            // Check if stacker is full
            if (_noteCount >= CASHBOX_SIZE)
            {
                _statusEvents |= 0x08; // Set stacker full
                _oneTimeStatusEvents.Enqueue(0x02); // Reject
                return;
            }

            // Start bill acceptance process
            Task.Run(() => ProcessBillInsertion(billValue));
        }

        private async Task ProcessBillInsertion(int billValue)
        {
            try
            {
                // Accepting state
                lock (_stateLock)
                {
                    _acceptorState = (byte)BillAcceptorState.Accepting;
                    _currentBillValue = billValue;
                }
                OnStateChanged(BillAcceptorState.Accepting, billValue);

                await Task.Delay(TimeSpan.FromSeconds(STATE_TRANSITION_TIME));

                // Check for cheat event
                bool cheated = false;
                if (CheatModeEnabled && _random.Next(1, 101) <= CHEAT_RATE)
                {
                    cheated = true;
                    _oneTimeStatusEvents.Enqueue(0x01); // Cheat event
                }

                lock (_stateLock)
                {
                    if (cheated)
                    {
                        // Return to idle, bill rejected due to cheat
                        _acceptorState = (byte)BillAcceptorState.Idling;
                        _currentBillValue = 0;
                        _oneTimeStatusEvents.Enqueue(0x02); // Reject
                        OnStateChanged(BillAcceptorState.Idling, 0);
                    }
                    else if (_escrowEnabled)
                    {
                        // Move to escrow (normal mode)
                        _acceptorState = (byte)BillAcceptorState.Escrowed;
                        OnStateChanged(BillAcceptorState.Escrowed, billValue);
                    }
                    else
                    {
                        // Auto-accept when escrow disabled - go directly to stacking
                        Console.WriteLine($"Auto-accepting ${GetBillValueText(billValue)} (escrow disabled)");
                        _ = Task.Run(StackBill);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in bill processing: {ex.Message}");
            }
        }

        private string GetBillValueText(int value)
        {
            return value switch
            {
                1 => "1",
                2 => "2", 
                3 => "5",
                4 => "10",
                5 => "20",
                6 => "50",
                7 => "100",
                _ => "Unknown"
            };
        }

        private void EnableBill(int billNumber)
        {
            if (billNumber >= 1 && billNumber <= 7)
            {
                byte mask = (byte)(1 << (billNumber - 1));
                _billEnables |= mask;
            }
        }

        private void DisableBill(int billNumber)
        {
            if (billNumber >= 1 && billNumber <= 7)
            {
                byte mask = (byte)(1 << (billNumber - 1));
                _billEnables &= (byte)~mask;
            }
        }

        private async Task CommunicationLoop()
        {
            var buffer = new byte[16];
            var timeoutTask = Task.Run(MonitorTimeout);

            try
            {
                while (!_cancellationTokenSource!.Token.IsCancellationRequested && _serialPort!.IsOpen)
                {
                    try
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                var receivedData = new byte[bytesRead];
                                Array.Copy(buffer, receivedData, bytesRead);
                                await ProcessReceivedData(receivedData);
                            }
                        }

                        await Task.Delay(50, _cancellationTokenSource.Token); // 50ms polling
                    }
                    catch (TimeoutException)
                    {
                        // Normal timeout, continue
                    }
                    catch (Exception ex) when (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Console.WriteLine($"Communication error: {ex.Message}");
                        await Task.Delay(1000);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
        }

        private async Task MonitorTimeout()
        {
            while (!_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                if ((DateTime.Now - _lastPollTime).TotalMilliseconds > COMM_TIMEOUT)
                {
                    // Communication timeout - reject any escrowed bill and stop accepting
                    lock (_stateLock)
                    {
                        if (_acceptorState == (byte)BillAcceptorState.Escrowed)
                        {
                            // Return bill automatically
                            Task.Run(ReturnBill);
                        }
                        _billEnables = 0; // Stop accepting bills
                    }
                    Console.WriteLine("Communication timeout - bill acceptance disabled");
                }

                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }

        private async Task ProcessReceivedData(byte[] data)
        {
            _lastPollTime = DateTime.Now;
            OnMessageReceived(data, false);

            // Validate message format
            if (data.Length < 6 || data[0] != STX || data[data.Length - 2] != ETX)
            {
                Console.WriteLine("Invalid message format received");
                return;
            }

            // Verify checksum
            byte expectedChecksum = 0;
            for (int i = 1; i < data.Length - 1; i++)
            {
                expectedChecksum ^= data[i];
            }
            if (expectedChecksum != data[data.Length - 1])
            {
                Console.WriteLine($"Master message checksum mismatch. Expected: {expectedChecksum:X2}, Got: {data[data.Length - 1]:X2}");
                // Still process the message but log the error
            }

            // Re-enable bills after communication resumes
            if (_billEnables == 0)
            {
                _billEnables = 0x7F; // Re-enable all bills
            }

            // Extract MSG Type and ACK from master
            byte msgTypeAndAck = data[2];
            byte masterMsgType = (byte)((msgTypeAndAck >> 4) & 0x07); // Bits 4-6
            byte masterAck = (byte)(msgTypeAndAck & 0x0F); // Bits 0-3

            // Check for reset command (MSG Type = 6)
            if (masterMsgType == 6 && data.Length >= 8)
            {
                // Check for reset pattern: all three data bytes = 0x7F
                if (data[3] == 0x7F && data[4] == 0x7F && data[5] == 0x7F)
                {
                    Console.WriteLine("Reset command received - performing reset");
                    // Don't acknowledge reset command, just perform reset
                    _ = Task.Run(PerformReset);
                    return;
                }
            }

            // Store master's ACK for our response
            _lastMasterAck = masterAck;

            // Process master commands (MSG Type = 1)
            if (masterMsgType == 1 && data.Length >= 8)
            {
                // Byte 0: Bill enables
                _billEnables = data[3];
                
                // Byte 1: Commands
                byte commands = data[4];
                _escrowEnabled = (commands & 0x10) != 0;
                bool stackCommand = (commands & 0x20) != 0;
                bool returnCommand = (commands & 0x40) != 0;

                // Process stack/return commands
                if (_acceptorState == (byte)BillAcceptorState.Escrowed)
                {
                    if (stackCommand && !returnCommand)
                    {
                        _ = Task.Run(StackBill);
                    }
                    else if (returnCommand && !stackCommand)
                    {
                        _ = Task.Run(ReturnBill);
                    }
                }
            }

            // Send response
            var response = BuildResponseMessage();
            _serialPort!.Write(response, 0, response.Length);
            OnMessageReceived(response, true);

            await Task.Delay(30); // Small delay for stability
        }

        private async Task StackBill()
        {
            lock (_stateLock)
            {
                _acceptorState = (byte)BillAcceptorState.Stacking;
                // Keep bill value during stacking
            }
            OnStateChanged(BillAcceptorState.Stacking, _currentBillValue);

            await Task.Delay(TimeSpan.FromSeconds(STATE_TRANSITION_TIME));

            lock (_stateLock)
            {
                // "Stacked" event with Idle state (both bits set as per documentation)
                // IMPORTANT: Keep bill value for the Stacked message
                _acceptorState = (byte)(BillAcceptorState.Idling | BillAcceptorState.Stacked);
                _noteCount++;
                // Don't clear bill value yet - master needs to see it
            }
            OnStateChanged(BillAcceptorState.Idling, _currentBillValue);

            // After one message transmission, clear the Stacked bit and bill value
            await Task.Delay(200); // Give time for message to be sent
            lock (_stateLock)
            {
                _acceptorState = (byte)BillAcceptorState.Idling;
                _currentBillValue = 0; // Clear after Stacked event is transmitted
            }
        }

        private async Task ReturnBill()
        {
            lock (_stateLock)
            {
                _acceptorState = (byte)BillAcceptorState.Returning;
            }
            OnStateChanged(BillAcceptorState.Returning, _currentBillValue);

            await Task.Delay(TimeSpan.FromSeconds(STATE_TRANSITION_TIME));

            lock (_stateLock)
            {
                // "Returned" event with Idle state (both bits set as per documentation)
                _acceptorState = (byte)(BillAcceptorState.Idling | BillAcceptorState.Returned);
                _currentBillValue = 0;
            }
            OnStateChanged(BillAcceptorState.Idling, 0);

            // After one message, clear the Returned bit and remain Idle
            await Task.Delay(100);
            lock (_stateLock)
            {
                _acceptorState = (byte)BillAcceptorState.Idling;
            }
        }

        private byte[] BuildResponseMessage()
        {
            lock (_stateLock)
            {
                // Build message according to specification
                var message = new byte[11];
                
                message[0] = STX;           // STX
                message[1] = MSG_LENGTH;    // Length (11 bytes total)
                
                // MSG Type and ACK Number byte
                // MSG Type = 2 (bits 4-6), ACK = echo master's ACK (bits 0-3)
                message[2] = (byte)((MSG_TYPE_SLAVE << 4) | (_lastMasterAck & 0x0F));

                // Data fields
                byte state = _acceptorState;
                byte statusEvents = _statusEvents;
                byte powerValue = _powerValue;

                // Add one-time events
                if (_oneTimeStateEvents.TryDequeue(out byte oneTimeState))
                    state |= oneTimeState;
                if (_oneTimeStatusEvents.TryDequeue(out byte oneTimeStatus))
                    statusEvents |= oneTimeStatus;
                if (_oneTimePowerEvents.TryDequeue(out byte oneTimePower))
                    powerValue |= oneTimePower;

                // Set bill value in bits 3-5 of powerValue byte
                if (_currentBillValue > 0)
                    powerValue |= (byte)((_currentBillValue & 0x07) << 3);

                message[3] = state;                    // Byte 0 - Acceptor State
                message[4] = statusEvents;             // Byte 1 - Status/Events  
                message[5] = powerValue;               // Byte 2 - Power & Value
                message[6] = _reserved;                // Byte 3 - Reserved
                message[7] = MODEL_NUMBER;             // Byte 4 - Model
                message[8] = FIRMWARE_REVISION;        // Byte 5 - Firmware
                message[9] = ETX;                      // ETX

                // Calculate checksum (XOR of all bytes except STX, ETX, and checksum)
                byte checksum = 0;
                for (int i = 1; i <= 8; i++)  // Length through Firmware bytes
                {
                    checksum ^= message[i];
                }
                message[10] = checksum;

                return message;
            }
        }

        private void OnMessageReceived(byte[] message, bool sent)
        {
            MessageReceived?.Invoke(message, sent);
        }

        private void OnStateChanged(BillAcceptorState state, int billValue)
        {
            StateChanged?.Invoke(state, billValue);
        }
    }
}