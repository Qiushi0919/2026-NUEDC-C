using System.Diagnostics;
using System.Media;

namespace DigitalKeyLab;

public sealed class MainForm : Form
{
    private readonly SerialService _serial = new();
    private readonly ControlLinkService _controlLink = new();
    private readonly CsvLogger _logger = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly Queue<double> _distanceWindow = new();
    private readonly Queue<double> _angleWindow = new();
    private readonly Stopwatch _rateWatch = Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer _blinkTimer = new() { Interval = 420 };

    private readonly ComboBox _portCombo = new();
    private readonly Button _connectButton = new();
    private readonly ComboBox _controlPortCombo = new();
    private readonly Button _controlConnectButton = new();
    private readonly Label _connectionStatus = new();
    private readonly CheckBox _soundCheck = new();
    private readonly CheckBox _autoConnectCheck = new();
    private readonly CheckBox _recordCheck = new();
    private readonly RadarControl _radar = new();
    private readonly TableLayoutPanel _sidePanel = new();
    private readonly ListBox _eventList = new();

    private readonly Label _fourBitValue = ValueLabel(14f);
    private readonly Label _allowedIdValue = ValueLabel(12f);
    private readonly Label _verifyValue = ValueLabel(12f);
    private readonly Label _expectedIdCaption = SmallLabel("钥匙身份ID");
    private readonly Label _expectedIdPreview = ValueLabel(13f);
    private readonly Label _distanceValue = ValueLabel(15f);
    private readonly Label _angleValue = ValueLabel(15f);
    private readonly Label _xValue = ValueLabel(10f);
    private readonly Label _yValue = ValueLabel(10f);
    private readonly Label _rateValue = ValueLabel(9f);
    private readonly Label _lastTimeValue = ValueLabel(9f);
    private readonly Label _zoneValue = ValueLabel(12f);
    private readonly Label _actionValue = ValueLabel(10.5f);
    private readonly Label _lightIndicator = IndicatorLabel();
    private readonly Label _lockIndicator = IndicatorLabel();
    private readonly Label _dataQualityValue = ValueLabel(9f);

    private readonly NumericUpDown _expectedId = new();
    private readonly Button _changeIdButton = new();
    private readonly NumericUpDown _distanceOffset = new();
    private readonly NumericUpDown _angleOffset = new();
    private readonly CheckBox _medianCheck = new();

    private AnchorFrame? _latestFrame;
    private DoorDecision _decision = DoorLogic.Evaluate(false, 0, 0, 0, null);
    private DateTime _lastPositionAt = DateTime.MinValue;
    private DateTime _lastHeartbeatAt = DateTime.MinValue;
    private double _latestDistance;
    private double _latestAngle;
    private int _framesSinceRate;
    private double _currentRate;
    private string _lastEventKey = string.Empty;
    private bool _blinkOn;
    private bool _autoConnectEligible;
    private bool _autoConnectControlEligible;
    private uint? _stableTagId;
    private uint? _candidateTagId;
    private int _candidateTagCount;
    private int _ignoredFrames;
    private int _activeKeyIdentityId;
    private int? _dipAllowedId;
    private DateTime _lastDipIdAt = DateTime.MinValue;
    private DateTime _lastControlStatusAt = DateTime.MinValue;
    private DateTime _lastControlErrorAt = DateTime.MinValue;
    private string _identitySourceMode = string.Empty;
    private ControlZone _lastSentControlZone = ControlZone.None;
    private bool _lastSentControlHadKey;

    private static readonly TimeSpan DipIdTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ControlStatusInterval = TimeSpan.FromMilliseconds(100);

    public MainForm()
    {
        Text = "C题数字钥匙实验系统 · 基站定位 + 蓝牙声光/DIP";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 620);
        Size = new Size(1280, 672);
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        BackColor = Color.FromArgb(241, 245, 249);
        Font = new Font("Microsoft YaHei UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        BindEvents();
        LoadSettingsIntoUi();
        RefreshPorts();
        ApplyOfflineState();

        _uiTimer.Start();
        _blinkTimer.Start();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = Padding.Empty,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = BackColor
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        _radar.Dock = DockStyle.Fill;
        _radar.Margin = new Padding(0, 0, 7, 0);
        content.Controls.Add(_radar, 0, 0);
        content.Controls.Add(BuildSidePanel(), 1, 0);
        root.Controls.Add(content, 0, 2);
        root.Controls.Add(BuildLogPanel(), 0, 3);
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(11, 19, 43) };
        var title = new Label
        {
            AutoSize = true,
            Text = "基于无线通信的数字钥匙实验系统",
            Font = new Font("Microsoft YaHei UI", 17f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(16, 5)
        };
        var subtitle = new Label
        {
            AutoSize = true,
            Text = "C题 · 120°基站 + 信标 + 蓝牙声光模块/DIP拨码身份验证",
            Font = new Font("Microsoft YaHei UI", 9f),
            ForeColor = Color.FromArgb(186, 230, 253),
            Location = new Point(18, 34)
        };
        var thresholds = new Label
        {
            AutoSize = true,
            Text = "开锁 0–1 m   |   迎宾 1–2 m   |   感应 2–3 m   |   有效角度 ±45°",
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(125, 211, 252),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(750, 19)
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(thresholds);
        header.Resize += (_, _) => thresholds.Left = Math.Max(610, header.ClientSize.Width - thresholds.Width - 16);
        return header;
    }

    private Control BuildToolbar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(9, 8, 7, 5),
            WrapContents = false,
            AutoScroll = false
        };

        panel.Controls.Add(ToolbarLabel("基站"));
        _portCombo.Width = 76;
        _portCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _portCombo.Font = new Font(Font, FontStyle.Bold);
        panel.Controls.Add(_portCombo);

        var refresh = ToolbarButton("刷新", 56);
        refresh.Click += (_, _) => RefreshPorts();
        panel.Controls.Add(refresh);

        panel.Controls.Add(ToolbarLabel("波特率"));
        var baud = new ComboBox { Width = 82, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(Font, FontStyle.Bold) };
        baud.Items.Add("115200");
        baud.SelectedIndex = 0;
        panel.Controls.Add(baud);

        _connectButton.Text = "连接基站";
        _connectButton.Width = 92;
        StylePrimaryButton(_connectButton, Color.FromArgb(2, 132, 199));
        panel.Controls.Add(_connectButton);

        _soundCheck.Text = "声音提示";
        _soundCheck.AutoSize = true;
        _soundCheck.Margin = new Padding(10, 7, 3, 0);
        panel.Controls.Add(_soundCheck);

        _recordCheck.Text = "记录CSV";
        _recordCheck.Checked = true;
        _recordCheck.AutoSize = true;
        _recordCheck.Margin = new Padding(7, 7, 3, 0);
        panel.Controls.Add(_recordCheck);

        _autoConnectCheck.Text = "启动自动连接";
        _autoConnectCheck.AutoSize = true;
        _autoConnectCheck.Margin = new Padding(7, 7, 3, 0);
        panel.Controls.Add(_autoConnectCheck);

        _connectionStatus.AutoSize = false;
        _connectionStatus.Size = new Size(195, 30);
        _connectionStatus.TextAlign = ContentAlignment.MiddleCenter;
        _connectionStatus.Margin = new Padding(10, 1, 0, 0);
        _connectionStatus.Font = new Font(Font, FontStyle.Bold);
        panel.Controls.Add(_connectionStatus);

        panel.Controls.Add(ToolbarLabel("蓝牙"));
        _controlPortCombo.Width = 64;
        _controlPortCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _controlPortCombo.Font = new Font(Font, FontStyle.Bold);
        panel.Controls.Add(_controlPortCombo);

        _controlConnectButton.Text = "连接蓝牙";
        _controlConnectButton.Width = 88;
        StylePrimaryButton(_controlConnectButton, Color.FromArgb(124, 58, 237));
        panel.Controls.Add(_controlConnectButton);
        return panel;
    }

    private Control BuildSidePanel()
    {
        _sidePanel.Dock = DockStyle.Fill;
        _sidePanel.ColumnCount = 1;
        _sidePanel.RowCount = 4;
        _sidePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _sidePanel.Padding = Padding.Empty;
        _sidePanel.BackColor = BackColor;

        _sidePanel.Controls.Add(BuildIdentityCard(), 0, 0);
        _sidePanel.Controls.Add(BuildPositionCard(), 0, 1);
        _sidePanel.Controls.Add(BuildDecisionCard(), 0, 2);
        _sidePanel.Controls.Add(BuildCalibrationCard(), 0, 3);
        return _sidePanel;
    }

    private Control BuildIdentityCard()
    {
        var card = Card("身份识别 · 要求1/2/6", 100, out var body);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(7, 2, 7, 2)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        grid.Controls.Add(SmallLabel("当前钥匙ID"), 0, 0);
        _fourBitValue.AutoSize = false;
        _fourBitValue.Dock = DockStyle.Fill;
        _fourBitValue.Margin = Padding.Empty;
        _fourBitValue.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(_fourBitValue, 1, 0);
        grid.Controls.Add(SmallLabel("门锁允许ID"), 2, 0);
        _allowedIdValue.AutoSize = false;
        _allowedIdValue.Dock = DockStyle.Fill;
        _allowedIdValue.Margin = Padding.Empty;
        _allowedIdValue.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(_allowedIdValue, 3, 0);
        _verifyValue.Dock = DockStyle.Fill;
        _verifyValue.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(_verifyValue, 4, 0);
        grid.SetColumnSpan(_verifyValue, 2);

        _expectedId.Minimum = 0;
        _expectedId.Maximum = 15;
        _expectedId.Width = 58;
        _expectedId.Font = new Font(Font, FontStyle.Bold);
        grid.Controls.Add(_expectedIdCaption, 0, 1);
        grid.Controls.Add(_expectedId, 1, 1);
        _expectedIdPreview.Dock = DockStyle.Fill;
        _expectedIdPreview.TextAlign = ContentAlignment.MiddleCenter;
        grid.Controls.Add(_expectedIdPreview, 2, 1);
        _changeIdButton.Text = "修改钥匙ID";
        _changeIdButton.Dock = DockStyle.Fill;
        _changeIdButton.Margin = new Padding(3, 1, 3, 1);
        StylePrimaryButton(_changeIdButton, Color.FromArgb(2, 132, 199));
        grid.Controls.Add(_changeIdButton, 3, 1);
        grid.SetColumnSpan(_changeIdButton, 3);
        body.Controls.Add(grid);
        return card;
    }

    private Control BuildPositionCard()
    {
        var card = Card("实时定位 · 要求3", 124, out var body);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        top.Controls.Add(Metric("径向距离", _distanceValue), 0, 0);
        top.Controls.Add(Metric("方位角 α", _angleValue), 1, 0);
        layout.Controls.Add(top, 0, 0);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(5, 0, 5, 0) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottom.Controls.Add(PairPanel(_xValue, _yValue), 0, 0);
        _rateValue.Dock = DockStyle.Fill;
        _rateValue.TextAlign = ContentAlignment.MiddleCenter;
        bottom.Controls.Add(_rateValue, 1, 0);
        _lastTimeValue.Dock = DockStyle.Fill;
        _lastTimeValue.TextAlign = ContentAlignment.MiddleCenter;
        bottom.Controls.Add(_lastTimeValue, 2, 0);
        layout.Controls.Add(bottom, 0, 1);
        body.Controls.Add(layout);
        return card;
    }

    private Control BuildDecisionCard()
    {
        var card = Card("门锁判决 · 要求4/5", 116, out var body);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 15));
        _zoneValue.AutoSize = false;
        _zoneValue.Dock = DockStyle.Fill;
        _zoneValue.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_zoneValue, 0, 0);

        _actionValue.AutoSize = false;
        _actionValue.Dock = DockStyle.Fill;
        _actionValue.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_actionValue, 0, 1);

        var indicators = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(7, 1, 7, 1) };
        indicators.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        indicators.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        indicators.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        indicators.Controls.Add(_lightIndicator, 0, 0);
        indicators.Controls.Add(_lockIndicator, 1, 0);
        layout.Controls.Add(indicators, 0, 2);

        _dataQualityValue.AutoSize = false;
        _dataQualityValue.Dock = DockStyle.Fill;
        _dataQualityValue.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_dataQualityValue, 0, 3);
        body.Controls.Add(layout);
        return card;
    }

    private Control BuildCalibrationCard()
    {
        var card = Card("定位校准", 104, out var body);
        _distanceOffset.Minimum = -2;
        _distanceOffset.Maximum = 2;
        _distanceOffset.DecimalPlaces = 2;
        _distanceOffset.Increment = 0.01m;
        _distanceOffset.Width = 68;

        _angleOffset.Minimum = -45;
        _angleOffset.Maximum = 45;
        _angleOffset.DecimalPlaces = 1;
        _angleOffset.Increment = 0.5m;
        _angleOffset.Width = 68;

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, Padding = new Padding(7, 2, 7, 2) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        grid.Controls.Add(SmallLabel("距离修正"), 0, 0);
        grid.Controls.Add(_distanceOffset, 1, 0);
        grid.Controls.Add(SmallLabel("m"), 2, 0);

        grid.Controls.Add(SmallLabel("角度修正"), 3, 0);
        grid.Controls.Add(_angleOffset, 4, 0);
        grid.Controls.Add(SmallLabel("°"), 5, 0);

        _medianCheck.Text = "5点中值滤波";
        _medianCheck.AutoSize = true;
        _medianCheck.Margin = new Padding(3, 5, 3, 0);
        grid.Controls.Add(_medianCheck, 0, 1);
        grid.SetColumnSpan(_medianCheck, 2);

        var threshold = SmallLabel("固定边界：1.00 / 2.00 / 3.00 m；有效角度 ±45°");
        threshold.ForeColor = Color.FromArgb(2, 132, 199);
        threshold.Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold);
        grid.Controls.Add(threshold, 2, 1);
        grid.SetColumnSpan(threshold, 4);
        body.Controls.Add(grid);
        return card;
    }

    private Control BuildLogPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(8, 2, 8, 6)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        header.Controls.Add(new Label
        {
            Text = "事件记录",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 5, 12, 0)
        });
        var clear = ToolbarButton("清空", 60);
        clear.Click += (_, _) => _eventList.Items.Clear();
        header.Controls.Add(clear);
        var openLog = ToolbarButton("打开记录目录", 105);
        openLog.Click += (_, _) => OpenLogFolder();
        header.Controls.Add(openLog);
        panel.Controls.Add(header, 0, 0);

        _eventList.Dock = DockStyle.Fill;
        _eventList.BorderStyle = BorderStyle.FixedSingle;
        _eventList.BackColor = Color.FromArgb(248, 250, 252);
        _eventList.ForeColor = Color.FromArgb(30, 41, 59);
        _eventList.Font = new Font("Consolas", 8.5f);
        panel.Controls.Add(_eventList, 0, 1);
        return panel;
    }

    private void BindEvents()
    {
        _connectButton.Click += (_, _) => ToggleConnection();
        _controlConnectButton.Click += (_, _) => ToggleControlConnection();
        _changeIdButton.Click += (_, _) => ApplyExpectedId();
        _serial.FrameReceived += frame =>
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() => HandleFrame(frame, "实机")));
        };
        _serial.ErrorOccurred += error =>
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() => PostEvent("串口错误", error)));
        };
        _controlLink.DipIdReceived += id =>
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() => HandleDipId(id)));
        };
        _controlLink.ErrorOccurred += error =>
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(() => PostEvent("蓝牙错误", error)));
        };
        _uiTimer.Tick += (_, _) => OnUiTick();
        _blinkTimer.Tick += (_, _) => OnBlinkTick();
        _expectedId.ValueChanged += (_, _) => UpdateExpectedIdPreview();
        _distanceOffset.ValueChanged += (_, _) => RecomputeCurrent();
        _angleOffset.ValueChanged += (_, _) => RecomputeCurrent();
        _medianCheck.CheckedChanged += (_, _) => RecomputeCurrent();
        FormClosing += (_, _) => Shutdown();
        Shown += (_, _) =>
        {
            if (!_autoConnectCheck.Checked)
                return;
            BeginInvoke(new Action(() =>
            {
                if (_autoConnectEligible && _portCombo.Items.Count > 0)
                    ConnectSelectedPort();
                if (_autoConnectControlEligible && _controlPortCombo.Items.Count > 0)
                    ConnectControlPort();
            }));
        };
    }

    private void LoadSettingsIntoUi()
    {
        _activeKeyIdentityId = Math.Clamp(_settings.KeyIdentityId, 0, 15);
        _expectedId.Value = _activeKeyIdentityId;
        UpdateExpectedIdPreview();
        _distanceOffset.Value = Math.Clamp(_settings.DistanceOffsetM, _distanceOffset.Minimum, _distanceOffset.Maximum);
        _angleOffset.Value = Math.Clamp(_settings.AngleOffsetDeg, _angleOffset.Minimum, _angleOffset.Maximum);
        _medianCheck.Checked = _settings.MedianFilterEnabled;
        _soundCheck.Checked = _settings.SoundEnabled;
        _autoConnectCheck.Checked = _settings.AutoConnect;
    }

    private void RefreshPorts()
    {
        var selected = _serial.IsConnected
            ? _serial.PortName
            : _portCombo.SelectedItem?.ToString() ?? _settings.PreferredPort;
        var controlSelected = _controlLink.IsConnected
            ? _controlLink.PortName
            : _controlPortCombo.SelectedItem?.ToString() ?? _settings.PreferredControlPort;
        var devices = SerialService.GetPortDevices();
        var ports = devices.Select(device => device.PortName).ToArray();
        _portCombo.Items.Clear();
        _controlPortCombo.Items.Clear();
        _portCombo.Items.AddRange(ports);
        _controlPortCombo.Items.AddRange(ports);
        if (ports.Length == 0)
        {
            SetConnectionBadge("未发现串口", Color.FromArgb(220, 38, 38));
            SetControlButton("无蓝牙串口", Color.FromArgb(100, 116, 139), false);
            return;
        }

        var selectedDevice = devices.FirstOrDefault(device =>
            device.PortName.Equals(selected, StringComparison.OrdinalIgnoreCase));
        var usbCandidate = devices.FirstOrDefault(device => device.IsUsb)
            ?? devices.FirstOrDefault(device => PortNumber(device.PortName) > 6 && !device.IsBluetooth);
        var preferred = selectedDevice?.PortName ?? usbCandidate?.PortName ?? ports[0];
        var preferredDevice = devices.First(device =>
            device.PortName.Equals(preferred, StringComparison.OrdinalIgnoreCase));
        _autoConnectEligible = preferredDevice.IsUsb || PortNumber(preferred) > 6;
        _portCombo.SelectedItem = preferred;
        var selectedControlDevice = devices.FirstOrDefault(device =>
            !device.PortName.Equals(preferred, StringComparison.OrdinalIgnoreCase) &&
            device.PortName.Equals(controlSelected, StringComparison.OrdinalIgnoreCase));
        var bluetoothCandidate = devices.FirstOrDefault(device =>
            device.IsBluetooth && !device.PortName.Equals(preferred, StringComparison.OrdinalIgnoreCase));
        var preferredControlDevice = selectedControlDevice?.IsBluetooth == true
            ? selectedControlDevice
            : bluetoothCandidate ?? selectedControlDevice
                ?? devices.FirstOrDefault(device => !device.PortName.Equals(preferred, StringComparison.OrdinalIgnoreCase));
        var preferredControl = preferredControlDevice?.PortName;
        _autoConnectControlEligible = preferredControl is not null &&
            devices.Any(device => device.PortName.Equals(preferredControl, StringComparison.OrdinalIgnoreCase) && device.IsBluetooth);
        if (preferredControl is not null)
            _controlPortCombo.SelectedItem = preferredControl;

        if (!_serial.IsConnected)
        {
            SetConnectionBadge(
                _autoConnectEligible ? $"待连接 · {preferred}" : "未发现USB串口 · 请检查CH340",
                _autoConnectEligible ? Color.FromArgb(100, 116, 139) : Color.FromArgb(220, 38, 38));
        }
        if (!_controlLink.IsConnected)
            SetControlButton(preferredControl is null ? "无蓝牙串口" : "连接蓝牙",
                preferredControl is null ? Color.FromArgb(100, 116, 139) : Color.FromArgb(124, 58, 237),
                preferredControl is not null);
    }

    private void ToggleConnection()
    {
        if (_serial.IsConnected)
            DisconnectSerial("用户断开");
        else
            ConnectSelectedPort();
    }

    private void ConnectSelectedPort()
    {
        if (_portCombo.SelectedItem is not string portName)
        {
            RefreshPorts();
            return;
        }
        if (_controlLink.IsConnected &&
            portName.Equals(_controlLink.PortName, StringComparison.OrdinalIgnoreCase))
        {
            PostEvent("连接失败", $"{portName} 已被蓝牙声光模块占用");
            return;
        }

        try
        {
            _serial.Connect(portName, 115200);
            ResetIdentityFilter();
            _settings.PreferredPort = portName;
            _connectButton.Text = "断开连接";
            _connectButton.BackColor = Color.FromArgb(220, 38, 38);
            SetConnectionBadge($"已连接 {portName} · 等待信标", Color.FromArgb(2, 132, 199));
            PostEvent("串口连接", $"{portName} @ 115200, 8N1");
            if (_recordCheck.Checked)
            {
                _logger.Start();
                PostEvent("数据记录", _logger.FilePath);
            }
        }
        catch (Exception ex)
        {
            SetConnectionBadge($"连接失败 · {portName}", Color.FromArgb(220, 38, 38));
            PostEvent("连接失败", ex.Message);
        }
    }

    private void DisconnectSerial(string reason)
    {
        if (_serial.IsConnected)
        {
            var port = _serial.PortName;
            _serial.Disconnect();
            PostEvent("串口断开", $"{port} · {reason}");
        }
        _logger.Stop();
        _connectButton.Text = "连接基站";
        _connectButton.BackColor = Color.FromArgb(2, 132, 199);
        SetConnectionBadge("串口未连接", Color.FromArgb(100, 116, 139));
        ApplyOfflineState();
    }

    private void ToggleControlConnection()
    {
        if (_controlLink.IsConnected)
            DisconnectControl("用户断开");
        else
            ConnectControlPort();
    }

    private void ConnectControlPort()
    {
        if (_controlPortCombo.SelectedItem is not string portName)
        {
            RefreshPorts();
            return;
        }
        if (_serial.IsConnected &&
            portName.Equals(_serial.PortName, StringComparison.OrdinalIgnoreCase))
        {
            PostEvent("蓝牙连接", $"{portName} 已被定位基站占用，请选择另一个串口");
            return;
        }

        try
        {
            _controlLink.Connect(portName);
            _settings.PreferredControlPort = portName;
            _dipAllowedId = null;
            _lastDipIdAt = DateTime.MinValue;
            _lastControlStatusAt = DateTime.MinValue;
            _lastSentControlZone = ControlZone.None;
            _lastSentControlHadKey = false;
            SetControlButton("断开蓝牙", Color.FromArgb(245, 158, 11), true);
            PostEvent("蓝牙连接", $"{portName} @ 115200, 8N1 · 等待门锁DIP允许ID");
            UpdateIdentitySource(DateTime.Now, true);
        }
        catch (Exception ex)
        {
            SetControlButton("连接失败", Color.FromArgb(220, 38, 38), true);
            PostEvent("蓝牙失败", $"{portName} · {ex.Message}");
        }
    }

    private void DisconnectControl(string reason)
    {
        if (_controlLink.IsConnected)
        {
            var port = _controlLink.PortName;
            _controlLink.Disconnect();
            PostEvent("蓝牙断开", $"{port} · {reason}");
        }
        _dipAllowedId = null;
        _lastDipIdAt = DateTime.MinValue;
        SetControlButton("连接蓝牙", Color.FromArgb(124, 58, 237), _controlPortCombo.Items.Count > 0);
        UpdateIdentitySource(DateTime.Now, true);
    }

    private void OnUiTick()
    {
        var now = DateTime.Now;
        if (_lastPositionAt != DateTime.MinValue && now - _lastPositionAt > TimeSpan.FromMilliseconds(850))
        {
            ApplyOfflineState();
            if (_serial.IsConnected)
            {
                if (now - _lastHeartbeatAt < TimeSpan.FromSeconds(3))
                    SetConnectionBadge($"{_serial.PortName} 基站在线 · 等待信标", Color.FromArgb(245, 158, 11));
                else
                    SetConnectionBadge($"{_serial.PortName} 已连接 · 无有效数据", Color.FromArgb(220, 38, 38));
            }
        }

        if (_rateWatch.ElapsedMilliseconds >= 1000)
        {
            _currentRate = _framesSinceRate * 1000.0 / _rateWatch.ElapsedMilliseconds;
            _framesSinceRate = 0;
            _rateWatch.Restart();
            _rateValue.Text = $"{_currentRate:F1} 帧/秒";
        }

        UpdateIdentitySource(now);
        SendControlStatusIfDue(now);
    }

    private void HandleFrame(AnchorFrame frame, string mode)
    {
        if (frame.Kind == FrameKind.Heartbeat)
        {
            _lastHeartbeatAt = DateTime.Now;
            if (_serial.IsConnected && DateTime.Now - _lastPositionAt > TimeSpan.FromMilliseconds(850))
                SetConnectionBadge($"{_serial.PortName} 基站在线 · 等待信标", Color.FromArgb(245, 158, 11));
            return;
        }

        if (frame.Kind != FrameKind.Position)
            return;

        if (!frame.ChecksumValid || frame.DistanceCm > 1000 || Math.Abs(frame.AzimuthDeg) > 180 ||
            frame.TagId is 0 or 0xFFFFFFFF || !AcceptStableTag(frame.TagId))
        {
            _ignoredFrames++;
            return;
        }

        _latestFrame = frame;
        _lastPositionAt = DateTime.Now;
        _lastHeartbeatAt = DateTime.Now;
        _framesSinceRate++;
        EnqueueSample(_distanceWindow, frame.DistanceM, 5);
        EnqueueSample(_angleWindow, frame.AzimuthDeg, 5);
        RecomputeCurrent(mode);

        if (_serial.IsConnected)
            SetConnectionBadge($"{_serial.PortName} 实时定位 · {_currentRate:F0} Hz", Color.FromArgb(22, 163, 74));
    }

    private void RecomputeCurrent(string? mode = null)
    {
        if (_latestFrame is null || _lastPositionAt == DateTime.MinValue)
            return;

        mode ??= "实机";

        var baseDistance = _medianCheck.Checked ? Median(_distanceWindow) : _latestFrame.DistanceM;
        var baseAngle = _medianCheck.Checked ? Median(_angleWindow) : _latestFrame.AzimuthDeg;
        _latestDistance = Math.Max(0, baseDistance + (double)_distanceOffset.Value);
        _latestAngle = baseAngle + (double)_angleOffset.Value;
        var allowedKeyId = CurrentAllowedKeyId(DateTime.Now);
        var nextDecision = DoorLogic.Evaluate(true, _latestDistance, _latestAngle,
            _activeKeyIdentityId, allowedKeyId);
        UpdateLiveUi(_latestFrame, mode, nextDecision);
        ApplyDecision(nextDecision);
        if (mode == "实机" && _recordCheck.Checked)
            _logger.Write(_latestFrame, mode, _latestDistance, _latestAngle,
                _activeKeyIdentityId, allowedKeyId, nextDecision);
    }

    private void UpdateLiveUi(AnchorFrame frame, string mode, DoorDecision decision)
    {
        var x = _latestDistance * Math.Sin(_latestAngle * Math.PI / 180.0);
        var y = _latestDistance * Math.Cos(_latestAngle * Math.PI / 180.0);
        var bits = FormatFourBit(_activeKeyIdentityId);

        _fourBitValue.Text = $"{bits}₂";
        _distanceValue.Text = $"{_latestDistance:F2} m";
        _angleValue.Text = $"{_latestAngle:+0.0;-0.0;0.0}°";
        _xValue.Text = $"X = {x:+0.00;-0.00;0.00} m";
        _yValue.Text = $"Y = {y:0.00} m";
        _lastTimeValue.Text = $"{frame.ReceivedAt:HH:mm:ss.fff} · {mode}";
        _dataQualityValue.Text = _ignoredFrames == 0
            ? "数据校验：正常"
            : $"数据校验：正常 · 已过滤 {_ignoredFrames} 个瞬时异常帧";
        _dataQualityValue.ForeColor = frame.ChecksumValid ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

        _radar.DistanceM = _latestDistance;
        _radar.AngleDeg = _latestAngle;
        _radar.HasPosition = true;
        _radar.Decision = decision;
        _radar.TagText = $"钥匙ID {bits}₂";
        _radar.Invalidate();
    }

    private void ApplyDecision(DoorDecision decision)
    {
        var previous = _decision;
        if (!HasFreshDipId(DateTime.Now))
        {
            _verifyValue.Text = "等待放行ID";
            _verifyValue.ForeColor = Color.FromArgb(245, 158, 11);
        }
        else
        {
            _verifyValue.Text = decision.IdentityMatched
                ? "✓ 身份通过"
                : "✕ 身份不匹配";
            _verifyValue.ForeColor = decision.IdentityMatched ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
        }
        _zoneValue.Text = decision.ZoneText;
        _zoneValue.ForeColor = decision.ZoneColor;
        _actionValue.Text = decision.ActionText;
        _actionValue.ForeColor = decision.IdentityMatched ? Color.FromArgb(15, 23, 42) : Color.FromArgb(220, 38, 38);

        _lightIndicator.Text = decision.LightOn ? "迎宾声光：开启" : "迎宾声光：关闭";
        _lightIndicator.BackColor = decision.LightOn ? Color.FromArgb(245, 158, 11) : Color.FromArgb(71, 85, 105);
        _lightIndicator.ForeColor = Color.White;
        _lockIndicator.Text = decision.Unlocked ? "门锁：已开锁" : "门锁：已闭锁";
        _lockIndicator.BackColor = decision.Unlocked ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
        _lockIndicator.ForeColor = Color.White;

        _decision = decision;
        if (decision.EventKey == _lastEventKey)
            return;

        if (!string.IsNullOrEmpty(_lastEventKey))
        {
            PostEvent("状态变化", $"{decision.ZoneText} | {decision.ActionText}");
            if (_soundCheck.Checked)
            {
                if (!previous.Unlocked && decision.Unlocked)
                    SystemSounds.Exclamation.Play();
                else if (previous.Unlocked && !decision.Unlocked)
                    SystemSounds.Beep.Play();
                else if (!previous.LightOn && decision.LightOn)
                    SystemSounds.Asterisk.Play();
                else if (previous.LightOn && !decision.LightOn)
                    SystemSounds.Beep.Play();
            }
        }
        _lastEventKey = decision.EventKey;
    }

    private void ApplyOfflineState()
    {
        _lastPositionAt = DateTime.MinValue;
        _decision = DoorLogic.Evaluate(false, 0, 0, _activeKeyIdentityId, CurrentAllowedKeyId(DateTime.Now));
        _radar.HasPosition = false;
        _radar.Decision = _decision;
        _radar.Invalidate();

        _fourBitValue.Text = "—";
        _verifyValue.Text = HasFreshDipId(DateTime.Now) ? "等待钥匙定位" : "等待身份数据";
        _verifyValue.ForeColor = HasFreshDipId(DateTime.Now)
            ? Color.FromArgb(14, 116, 144)
            : Color.FromArgb(100, 116, 139);
        _distanceValue.Text = "— m";
        _angleValue.Text = "— °";
        _xValue.Text = "X = — m";
        _yValue.Text = "Y = — m";
        _lastTimeValue.Text = "—";
        _dataQualityValue.Text = "未收到定位帧 0x2001";
        _dataQualityValue.ForeColor = Color.FromArgb(100, 116, 139);
        _zoneValue.Text = _decision.ZoneText;
        _zoneValue.ForeColor = _decision.ZoneColor;
        _actionValue.Text = _decision.ActionText;
        _lightIndicator.Text = "迎宾声光：关闭";
        _lightIndicator.BackColor = Color.FromArgb(71, 85, 105);
        _lockIndicator.Text = "门锁：已闭锁";
        _lockIndicator.BackColor = Color.FromArgb(220, 38, 38);
        _lastEventKey = _decision.EventKey;
    }

    private void OnBlinkTick()
    {
        if (!_decision.LightOn)
            return;
        _blinkOn = !_blinkOn;
        _lightIndicator.BackColor = _blinkOn ? Color.FromArgb(250, 204, 21) : Color.FromArgb(245, 158, 11);
    }

    private void UpdateExpectedIdPreview()
    {
        _expectedIdPreview.Text = $"{FormatFourBit((int)_expectedId.Value)}₂";
    }

    private void ApplyExpectedId()
    {
        _activeKeyIdentityId = (int)_expectedId.Value;
        _settings.KeyIdentityId = _activeKeyIdentityId;
        _settings.Save();
        _fourBitValue.Text = _latestFrame is null ? "—" : $"{FormatFourBit(_activeKeyIdentityId)}₂";
        PostEvent("身份设置", $"钥匙身份ID已修改为 {FormatFourBit(_activeKeyIdentityId)}₂");
        RecomputeCurrent();
    }

    private void HandleDipId(int id)
    {
        id &= 0x0F;
        var changed = _dipAllowedId != id;
        _dipAllowedId = id;
        _lastDipIdAt = DateTime.Now;
        UpdateIdentitySource(_lastDipIdAt, true);
        if (changed)
            PostEvent("门锁权限", $"DIP允许放行的钥匙ID：{FormatFourBit(id)}₂");
    }

    private bool HasFreshDipId(DateTime now) =>
        _controlLink.IsConnected && _dipAllowedId.HasValue &&
        now - _lastDipIdAt <= DipIdTimeout;

    private int? CurrentAllowedKeyId(DateTime now) =>
        HasFreshDipId(now) ? _dipAllowedId : null;

    private void UpdateIdentitySource(DateTime now, bool force = false)
    {
        var nextMode = !_controlLink.IsConnected
            ? "software"
            : HasFreshDipId(now) ? "dip" : "waiting";
        if (!force && nextMode == _identitySourceMode)
            return;

        var previousMode = _identitySourceMode;
        _identitySourceMode = nextMode;
        _expectedIdCaption.Text = "钥匙身份ID";
        _expectedId.Enabled = true;
        _expectedIdPreview.Text = $"{FormatFourBit((int)_expectedId.Value)}₂";
        _changeIdButton.Text = "修改钥匙ID";
        _changeIdButton.Enabled = true;
        switch (nextMode)
        {
            case "dip":
                _allowedIdValue.Text = $"{FormatFourBit(_dipAllowedId!.Value)}₂";
                _allowedIdValue.ForeColor = Color.FromArgb(22, 163, 74);
                if (_lastPositionAt == DateTime.MinValue)
                {
                    _verifyValue.Text = "等待钥匙定位";
                    _verifyValue.ForeColor = Color.FromArgb(14, 116, 144);
                }
                SetControlButton("断开蓝牙", Color.FromArgb(22, 163, 74), true);
                break;

            case "waiting":
                _allowedIdValue.Text = "等待DIP";
                _allowedIdValue.ForeColor = Color.FromArgb(245, 158, 11);
                _verifyValue.Text = "等待放行ID";
                _verifyValue.ForeColor = Color.FromArgb(245, 158, 11);
                SetControlButton("断开蓝牙", Color.FromArgb(245, 158, 11), true);
                if (previousMode == "dip")
                    PostEvent("蓝牙超时", "超过500 ms未收到门锁DIP允许ID，已进入安全闭锁状态");
                break;

            default:
                _allowedIdValue.Text = "—";
                _allowedIdValue.ForeColor = Color.FromArgb(100, 116, 139);
                if (_lastPositionAt == DateTime.MinValue)
                {
                    _verifyValue.Text = "等待门锁连接";
                    _verifyValue.ForeColor = Color.FromArgb(100, 116, 139);
                }
                break;
        }
        RecomputeCurrent();
    }

    private void SendControlStatusIfDue(DateTime now)
    {
        if (!_controlLink.IsConnected || now - _lastControlStatusAt < ControlStatusInterval)
            return;

        var hasKey = _latestFrame is not null && _lastPositionAt != DateTime.MinValue &&
            now - _lastPositionAt <= TimeSpan.FromMilliseconds(850);
        var allowedKeyId = CurrentAllowedKeyId(now);
        var zone = hasKey ? ToControlZone(_decision.Zone) : ControlZone.None;
        var eventCode = DetermineControlEvent(_lastSentControlHadKey, _lastSentControlZone, hasKey, zone);
        var auth = !hasKey || !allowedKeyId.HasValue
            ? ControlAuth.Unknown
            : _decision.IdentityMatched ? ControlAuth.Passed : ControlAuth.Failed;

        var state = ControlStateFlags.Locked;
        if (hasKey)
            state |= ControlStateFlags.KeyPresent;
        if (hasKey && _decision.IdentityMatched)
            state |= ControlStateFlags.IdMatched;
        if (hasKey && _decision.LightOn)
            state |= ControlStateFlags.WelcomeActive;
        if (hasKey && _decision.Unlocked)
        {
            state &= ~ControlStateFlags.Locked;
            state |= ControlStateFlags.Unlocked;
        }

        var keyId = hasKey ? (byte)_activeKeyIdentityId : (byte)0;
        var distanceMm = hasKey
            ? (ushort)Math.Clamp((int)Math.Round(_latestDistance * 1000.0), 0, ushort.MaxValue)
            : (ushort)0;
        var angleDeg = hasKey
            ? (sbyte)Math.Clamp((int)Math.Round(_latestAngle), -90, 90)
            : (sbyte)0;
        var status = new ControlLockStatus(
            keyId,
            auth,
            zone,
            eventCode,
            distanceMm,
            angleDeg,
            state,
            hasKey ? (byte)100 : (byte)0);

        try
        {
            _controlLink.SendLockStatus(status);
            _lastControlStatusAt = now;
            _lastSentControlHadKey = hasKey;
            _lastSentControlZone = zone;
        }
        catch (Exception ex)
        {
            if (now - _lastControlErrorAt > TimeSpan.FromSeconds(2))
            {
                _lastControlErrorAt = now;
                PostEvent("蓝牙发送", ex.Message);
            }
        }
    }

    private static ControlZone ToControlZone(RangeZone zone) => zone switch
    {
        RangeZone.Sensing => ControlZone.Sensing,
        RangeZone.Welcome => ControlZone.Welcome,
        RangeZone.Unlock => ControlZone.Unlock,
        _ => ControlZone.None
    };

    private static ControlEvent DetermineControlEvent(
        bool previousHadKey, ControlZone previousZone, bool hasKey, ControlZone zone)
    {
        if (!previousHadKey && hasKey)
            return ControlEvent.Discovered;
        if (previousHadKey && !hasKey)
            return ControlEvent.Lost;
        if (previousZone == zone)
            return ControlEvent.Steady;
        if (zone == ControlZone.Unlock)
            return ControlEvent.EnterUnlock;
        if (previousZone == ControlZone.Unlock)
            return ControlEvent.LeaveUnlock;
        if (zone == ControlZone.Welcome)
            return ControlEvent.EnterWelcome;
        if (previousZone == ControlZone.Welcome)
            return ControlEvent.LeaveWelcome;
        return zone == ControlZone.None ? ControlEvent.Lost : ControlEvent.Discovered;
    }

    private bool AcceptStableTag(uint tagId)
    {
        if (_stableTagId == tagId)
        {
            _candidateTagId = null;
            _candidateTagCount = 0;
            return true;
        }

        if (_candidateTagId == tagId)
            _candidateTagCount++;
        else
        {
            _candidateTagId = tagId;
            _candidateTagCount = 1;
        }

        if (_candidateTagCount < 3)
            return false;

        _stableTagId = tagId;
        _candidateTagId = null;
        _candidateTagCount = 0;
        PostEvent("身份稳定", $"连续 3 帧确认信标 0x{tagId:X8}");
        return true;
    }

    private void ResetIdentityFilter()
    {
        _stableTagId = null;
        _candidateTagId = null;
        _candidateTagCount = 0;
        _ignoredFrames = 0;
    }

    private void PostEvent(string category, string message)
    {
        _eventList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] {category,-8} {message}");
        while (_eventList.Items.Count > 200)
            _eventList.Items.RemoveAt(_eventList.Items.Count - 1);
    }

    private void SetConnectionBadge(string text, Color color)
    {
        _connectionStatus.Text = text;
        _connectionStatus.BackColor = color;
        _connectionStatus.ForeColor = Color.White;
    }

    private void SetControlButton(string text, Color color, bool enabled)
    {
        _controlConnectButton.Text = text;
        _controlConnectButton.BackColor = color;
        _controlConnectButton.Enabled = enabled;
    }

    private void OpenLogFolder()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "C题数字钥匙实验记录");
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
    }

    private void Shutdown()
    {
        _uiTimer.Stop();
        _blinkTimer.Stop();
        _serial.Dispose();
        _controlLink.Dispose();
        _logger.Dispose();
        _settings.KeyIdentityId = _activeKeyIdentityId;
        _settings.DistanceOffsetM = _distanceOffset.Value;
        _settings.AngleOffsetDeg = _angleOffset.Value;
        _settings.MedianFilterEnabled = _medianCheck.Checked;
        _settings.SoundEnabled = _soundCheck.Checked;
        _settings.AutoConnect = _autoConnectCheck.Checked;
        _settings.Save();
    }

    private static Panel Card(string title, int height, out Panel body)
    {
        var card = new Panel
        {
            Height = height,
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(1),
            BorderStyle = BorderStyle.FixedSingle
        };
        var header = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 27,
            Padding = new Padding(8, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            BackColor = Color.FromArgb(241, 245, 249)
        };
        body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(2) };
        card.Controls.Add(body);
        card.Controls.Add(header);
        return card;
    }

    private static TableLayoutPanel InfoGrid(int rows)
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = rows, Padding = new Padding(7, 3, 7, 3) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < rows; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
        return grid;
    }

    private static void AddInfoRow(TableLayoutPanel grid, int row, string name, Control value)
    {
        grid.Controls.Add(SmallLabel(name), 0, row);
        value.Dock = DockStyle.Fill;
        if (value is Label label)
            label.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(value, 1, row);
    }

    private static Control PairPanel(Control first, Control second)
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        first.AutoSize = true;
        second.AutoSize = true;
        first.Margin = new Padding(0, 2, 10, 0);
        second.Margin = new Padding(0, 2, 0, 0);
        flow.Controls.Add(first);
        flow.Controls.Add(second);
        return flow;
    }

    private static Control Metric(string caption, Label value)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(2) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(100, 116, 139)
        }, 0, 0);
        value.AutoSize = false;
        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleCenter;
        panel.Controls.Add(value, 0, 1);
        return panel;
    }

    private static Label ValueLabel(float size = 12f) => new()
    {
        Text = "—",
        AutoSize = true,
        Font = new Font("Microsoft YaHei UI", size, FontStyle.Bold),
        ForeColor = Color.FromArgb(15, 23, 42),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label IndicatorLabel() => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Margin = new Padding(3, 1, 3, 1),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.White
    };

    private static Label SmallLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(71, 85, 105),
        Font = new Font("Microsoft YaHei UI", 9f),
        AutoEllipsis = true
    };

    private static Label ToolbarLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(4, 7, 4, 0),
        ForeColor = Color.FromArgb(51, 65, 85)
    };

    private static Button ToolbarButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 26,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(30, 41, 59),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static void StylePrimaryButton(Button button, Color color)
    {
        button.Height = 30;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    private static void EnqueueSample(Queue<double> queue, double value, int max)
    {
        queue.Enqueue(value);
        while (queue.Count > max)
            queue.Dequeue();
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        if (ordered.Length == 0)
            return 0;
        return ordered.Length % 2 == 1
            ? ordered[ordered.Length / 2]
            : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2.0;
    }

    private static int PortNumber(string name) =>
        name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[3..], out var number)
            ? number
            : int.MaxValue;

    private static string FormatFourBit(int id) => Convert.ToString(id & 0xF, 2).PadLeft(4, '0');
}
