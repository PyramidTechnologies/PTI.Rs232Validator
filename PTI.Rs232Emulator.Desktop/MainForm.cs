using System.IO.Ports;

namespace PTI.Rs232Emulator.Desktop
{
    public partial class MainForm : Form
    {
        private BillAcceptor? _acceptor;
        private AutoPilot? _autoPilot;
        private ComboBox _comPortComboBox = null!;
        private Button _connectButton = null!;
        private Button _disconnectButton = null!;
        private GroupBox _billControlsGroupBox = null!;
        private GroupBox _statusGroupBox = null!;
        private GroupBox _eventsGroupBox = null!;
        private GroupBox _protocolGroupBox = null!;
        private TextBox _logTextBox = null!;
        private Label _connectionStatusLabel = null!;
        private Label _acceptorStateLabel = null!;
        private Label _billValueLabel = null!;
        private CheckBox _autoPilotCheckBox = null!;
        private Button _resetButton = null!;
        private Button _clearLogButton = null!;

        public MainForm ()
        {
            InitializeComponent();
            InitializeComPorts();
        }

        private void InitializeComponent ()
        {
            SuspendLayout();

            // Form
            Text = "Bill Acceptor Emulator";
            Size = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // Connection controls
            var connectionGroupBox = new GroupBox
            {
                Text = "Connection",
                Location = new Point(10, 10),
                Size = new Size(300, 60)
            };

            var comPortLabel = new Label
            {
                Text = "COM Port:",
                Location = new Point(10, 25),
                Size = new Size(60, 20)
            };

            _comPortComboBox = new ComboBox
            {
                Location = new Point(75, 22),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _connectButton = new Button
            {
                Text = "Connect",
                Location = new Point(165, 20),
                Size = new Size(60, 25)
            };
            _connectButton.Click += ConnectButton_Click;

            _disconnectButton = new Button
            {
                Text = "Disconnect",
                Location = new Point(230, 20),
                Size = new Size(65, 25),
                Enabled = false
            };
            _disconnectButton.Click += DisconnectButton_Click;

            connectionGroupBox.Controls.AddRange(new Control[] { comPortLabel, _comPortComboBox, _connectButton, _disconnectButton });

            // Status display
            _statusGroupBox = new GroupBox
            {
                Text = "Status",
                Location = new Point(320, 10),
                Size = new Size(250, 100)
            };

            _connectionStatusLabel = new Label
            {
                Text = "Status: Disconnected",
                Location = new Point(10, 25),
                Size = new Size(200, 20),
                ForeColor = Color.Red
            };

            _acceptorStateLabel = new Label
            {
                Text = "State: Unknown",
                Location = new Point(10, 45),
                Size = new Size(200, 20)
            };

            _billValueLabel = new Label
            {
                Text = "Bill Value: None",
                Location = new Point(10, 65),
                Size = new Size(200, 20)
            };

            _statusGroupBox.Controls.AddRange(new Control[] { _connectionStatusLabel, _acceptorStateLabel, _billValueLabel });

            // Bill controls
            _billControlsGroupBox = new GroupBox
            {
                Text = "Bill Insertion",
                Location = new Point(10, 80),
                Size = new Size(300, 120),
                Enabled = false
            };

            var billButtons = new Button[7];
            var billValues = new[] { "$1", "$2", "$5", "$10", "$20", "$50", "$100" };

            for ( int i = 0; i < 7; i++ )
            {
                int billIndex = i + 1;
                billButtons[i] = new Button
                {
                    Text = $"{billIndex} - {billValues[i]}",
                    Location = new Point(10 + (i % 4) * 70, 25 + (i / 4) * 30),
                    Size = new Size(65, 25),
                    Tag = billIndex
                };
                billButtons[i].Click += ( s, e ) => BillButton_Click(billIndex);
                _billControlsGroupBox.Controls.Add(billButtons[i]);
            }

            // Event controls
            _eventsGroupBox = new GroupBox
            {
                Text = "Events & Controls",
                Location = new Point(10, 210),
                Size = new Size(300, 150),
                Enabled = false
            };

            var eventButtons = new[]
            {
                new { Text = "Cheat (C)", Command = "C", Pos = new Point(10, 25) },
                new { Text = "Reject (R)", Command = "R", Pos = new Point(80, 25) },
                new { Text = "Jam (J)", Command = "J", Pos = new Point(150, 25) },
                new { Text = "Stack Full (F)", Command = "F", Pos = new Point(220, 25) },
                new { Text = "Cassette (P)", Command = "P", Pos = new Point(10, 55) },
                new { Text = "Power Up (W)", Command = "W", Pos = new Point(80, 55) },
                new { Text = "Invalid Cmd (I)", Command = "I", Pos = new Point(150, 55) },
                new { Text = "Failure (X)", Command = "X", Pos = new Point(220, 55) },
                new { Text = "Empty Box (Y)", Command = "Y", Pos = new Point(10, 85) }
            };

            foreach ( var btn in eventButtons )
            {
                var button = new Button
                {
                    Text = btn.Text,
                    Location = btn.Pos,
                    Size = new Size(65, 25),
                    Tag = btn.Command
                };
                button.Click += EventButton_Click;
                _eventsGroupBox.Controls.Add(button);
            }

            _autoPilotCheckBox = new CheckBox
            {
                Text = "AutoPilot",
                Location = new Point(10, 115),
                Size = new Size(80, 25)
            };
            _autoPilotCheckBox.CheckedChanged += AutoPilotCheckBox_CheckedChanged;
            _eventsGroupBox.Controls.Add(_autoPilotCheckBox);

            // Protocol controls
            _protocolGroupBox = new GroupBox
            {
                Text = "Protocol Testing",
                Location = new Point(320, 120),
                Size = new Size(250, 80),
                Enabled = false
            };

            _resetButton = new Button
            {
                Text = "Send Reset (0x7F 0x7F 0x7F)",
                Location = new Point(10, 25),
                Size = new Size(150, 25)
            };
            _resetButton.Click += ResetButton_Click;

            var enableAllButton = new Button
            {
                Text = "Enable All",
                Location = new Point(170, 25),
                Size = new Size(70, 25)
            };
            enableAllButton.Click += ( s, e ) => SendCommand("E1,E2,E3,E4,E5,E6,E7");

            var disableAllButton = new Button
            {
                Text = "Disable All",
                Location = new Point(170, 50),
                Size = new Size(70, 25)
            };
            disableAllButton.Click += ( s, e ) => SendCommand("D1,D2,D3,D4,D5,D6,D7");

            _protocolGroupBox.Controls.AddRange(new Control[] { _resetButton, enableAllButton, disableAllButton });

            // Log display
            var logGroupBox = new GroupBox
            {
                Text = "Communication Log",
                Location = new Point(10, 370),
                Size = new Size(860, 280)
            };

            _logTextBox = new TextBox
            {
                Location = new Point(10, 25),
                Size = new Size(840, 220),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };

            _clearLogButton = new Button
            {
                Text = "Clear Log",
                Location = new Point(10, 250),
                Size = new Size(80, 25)
            };
            _clearLogButton.Click += ( s, e ) => _logTextBox.Clear();

            logGroupBox.Controls.AddRange(new Control[] { _logTextBox, _clearLogButton });

            // Add all controls to form
            Controls.AddRange(new Control[]
            {
                connectionGroupBox,
                _statusGroupBox,
                _billControlsGroupBox,
                _eventsGroupBox,
                _protocolGroupBox,
                logGroupBox
            });

            ResumeLayout();
        }

        private void InitializeComPorts ()
        {
            _comPortComboBox.Items.Clear();
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            _comPortComboBox.Items.AddRange(ports);

            if ( ports.Length > 0 )
            {
                var defaultPort = ports.FirstOrDefault(x => x.EndsWith("3"));
                int index = Array.IndexOf(ports, defaultPort);
                _comPortComboBox.SelectedIndex = index >= 0 ? index : 0;
            }
        }

        private void ConnectButton_Click ( object? sender, EventArgs e )
        {
            if ( _comPortComboBox.SelectedItem == null )
            {
                MessageBox.Show("Please select a COM port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string portName = _comPortComboBox.SelectedItem.ToString()!;
                _acceptor = new BillAcceptor();
                _acceptor.MessageReceived += OnMessageReceived;
                _acceptor.StateChanged += OnStateChanged;
                _acceptor.Start(portName);

                _autoPilot = new AutoPilot(_acceptor);

                _connectButton.Enabled = false;
                _disconnectButton.Enabled = true;
                _billControlsGroupBox.Enabled = true;
                _eventsGroupBox.Enabled = true;
                _protocolGroupBox.Enabled = true;
                _comPortComboBox.Enabled = false;

                _connectionStatusLabel.Text = $"Status: Connected to {portName}";
                _connectionStatusLabel.ForeColor = Color.Green;

                LogMessage($"Connected to {portName}");
            }
            catch ( Exception ex )
            {
                MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisconnectButton_Click ( object? sender, EventArgs e )
        {
            try
            {
                _autoPilot?.Stop();
                _acceptor?.Stop();

                _connectButton.Enabled = true;
                _disconnectButton.Enabled = false;
                _billControlsGroupBox.Enabled = false;
                _eventsGroupBox.Enabled = false;
                _protocolGroupBox.Enabled = false;
                _comPortComboBox.Enabled = true;
                _autoPilotCheckBox.Checked = false;

                _connectionStatusLabel.Text = "Status: Disconnected";
                _connectionStatusLabel.ForeColor = Color.Red;
                _acceptorStateLabel.Text = "State: Unknown";
                _billValueLabel.Text = "Bill Value: None";

                LogMessage("Disconnected");
            }
            catch ( Exception ex )
            {
                MessageBox.Show($"Error during disconnect: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BillButton_Click ( int billValue )
        {
            SendCommand(billValue.ToString());
        }

        private void EventButton_Click ( object? sender, EventArgs e )
        {
            if ( sender is Button button && button.Tag is string command )
            {
                SendCommand(command);
            }
        }

        private void AutoPilotCheckBox_CheckedChanged ( object? sender, EventArgs e )
        {
            if ( _autoPilot != null )
            {
                if ( _autoPilotCheckBox.Checked )
                    _autoPilot.Start();
                else
                    _autoPilot.Stop();
            }
        }

        private void ResetButton_Click ( object? sender, EventArgs e )
        {
            _acceptor?.SendReset();
            LogMessage("Reset command sent (0x7F 0x7F 0x7F)");
        }

        private void SendCommand ( string command )
        {
            if ( _acceptor != null )
            {
                var commands = command.Split(',');
                foreach ( var cmd in commands )
                {
                    _acceptor.ParseCommand(cmd.Trim());
                    LogMessage($"Command sent: {cmd.Trim()}");
                }
            }
        }

        private void OnMessageReceived ( byte[] message, bool sent )
        {
            if ( InvokeRequired )
            {
                Invoke(new Action<byte[], bool>(OnMessageReceived), message, sent);
                return;
            }

            string direction = sent ? "TX" : "RX";
            string hex = BitConverter.ToString(message).Replace("-", " ");
            string decoded = DecodeMessage(message);
            LogMessage($"{direction}: {hex} - {decoded}");
        }

        private void OnStateChanged ( BillAcceptorState state, int billValue )
        {
            if ( InvokeRequired )
            {
                Invoke(new Action<BillAcceptorState, int>(OnStateChanged), state, billValue);
                return;
            }

            _acceptorStateLabel.Text = $"State: {state}";
            _billValueLabel.Text = billValue > 0 ? $"Bill Value: ${GetBillValueText(billValue)}" : "Bill Value: None";
        }

        private string GetBillValueText ( int value )
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

        private string DecodeMessage ( byte[] message )
        {
            if ( message.Length < 6 )
                return "Invalid message length";

            try
            {
                var parts = new List<string>();

                // Handle both master and slave messages
                if ( message.Length >= 8 && message[1] == 0x08 ) // Master message
                {
                    parts.Add("MASTER");
                    byte msgType = (byte) ((message[2] >> 4) & 0x07);
                    byte ack = (byte) (message[2] & 0x0F);
                    parts.Add($"Type:{msgType}");
                    parts.Add($"ACK:{ack}");

                    if ( message.Length > 3 )
                        parts.Add($"Cmd:0x{message[3]:X2}");
                }
                else if ( message.Length >= 11 && message[1] == 0x0B ) // Slave message
                {
                    parts.Add("SLAVE");
                    byte msgType = (byte) ((message[2] >> 4) & 0x07);
                    byte ack = (byte) (message[2] & 0x0F);
                    parts.Add($"Type:{msgType}");
                    parts.Add($"ACK:{ack}");

                    byte state = message[3];
                    byte status = message[4];
                    byte powerValue = message[5];

                    // Decode state
                    if ( (state & 0x01) != 0 )
                        parts.Add("Idling");
                    if ( (state & 0x02) != 0 )
                        parts.Add("Accepting");
                    if ( (state & 0x04) != 0 )
                        parts.Add("Escrowed");
                    if ( (state & 0x08) != 0 )
                        parts.Add("Stacking");
                    if ( (state & 0x10) != 0 )
                        parts.Add("Stacked");
                    if ( (state & 0x20) != 0 )
                        parts.Add("Returning");
                    if ( (state & 0x40) != 0 )
                        parts.Add("Returned");

                    // Decode status/events
                    if ( (status & 0x01) != 0 )
                        parts.Add("Cheat");
                    if ( (status & 0x02) != 0 )
                        parts.Add("Reject");
                    if ( (status & 0x04) != 0 )
                        parts.Add("Jam");
                    if ( (status & 0x08) != 0 )
                        parts.Add("StackFull");
                    if ( (status & 0x10) != 0 )
                        parts.Add("Cassette");

                    // Decode power/value
                    if ( (powerValue & 0x01) != 0 )
                        parts.Add("PowerUp");
                    if ( (powerValue & 0x02) != 0 )
                        parts.Add("InvalidCmd");
                    if ( (powerValue & 0x04) != 0 )
                        parts.Add("Failure");

                    int billValue = (powerValue >> 3) & 0x07;
                    if ( billValue > 0 )
                        parts.Add($"Bill${GetBillValueText(billValue)}");
                }

                return string.Join(", ", parts);
            }
            catch
            {
                return "Decode error";
            }
        }

        private void LogMessage ( string message )
        {
            if ( InvokeRequired )
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            _logTextBox.SelectionStart = _logTextBox.Text.Length;
            _logTextBox.ScrollToCaret();
        }

        protected override void OnFormClosing ( FormClosingEventArgs e )
        {
            _autoPilot?.Stop();
            _acceptor?.Stop();
            base.OnFormClosing(e);
        }
    }
}