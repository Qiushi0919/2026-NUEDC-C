using System.Diagnostics;
using DigitalKeyLab;

namespace CalibrationCollector;

public sealed class MainForm : Form
{
    private const string DataFolderName = "数据集_v3_场地分段校准30点中值";
    private readonly SerialEndpoint _bluetooth = new("COM21", "bluetooth");
    private readonly SerialEndpoint _anchor = new("COM22", "anchor");
    private readonly BluetoothProtocolParser _bluetoothParser = new();
    private readonly AnchorProtocolParser _anchorParser = new();
    private readonly object _bluetoothParserLock = new();
    private readonly object _anchorParserLock = new();
    private readonly object _measurementProcessorLock = new();
    private readonly MeasurementProcessor _measurementProcessor = new();
    private readonly System.Windows.Forms.Timer _reconnectTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 100 };

    private readonly Label _bluetoothStatus = new();
    private readonly Label _anchorStatus = new();
    private readonly NumericUpDown _actualDistance = new();
    private readonly NumericUpDown _actualAngle = new();
    private readonly NumericUpDown _duration = new();
    private readonly Label _distanceMeaning = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _openFolderButton = new();
    private readonly Label _recordStatus = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _rawDistanceValue = new();
    private readonly Label _correctedDistanceValue = new();
    private readonly Label _rawAngleValue = new();
    private readonly Label _correctedAngleValue = new();
    private readonly Label _tagIdValue = new();
    private readonly Label _allowedIdValue = new();
    private readonly Label _frameCountValue = new();
    private readonly Label _savePathValue = new();
    private readonly DataGridView _grid = new();

    private SessionRecorder? _recorder;
    private string? _lastSessionFolder;
    private int _latestAllowedId = -1;
    private long _positionFramesSeen;
    private long _validPositionFramesSeen;

    public MainForm()
    {
        Text = "C题 - 矫正数据集采集软件";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(940, 680);
        ClientSize = new Size(1080, 760);
        Font = new Font("Microsoft YaHei UI", 10F);
        BackColor = Color.FromArgb(243, 246, 250);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildInterface();
        WireEvents();
        UpdateDistanceMeaning();
        UpdateConnectionStatus();
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildConnectionPanel(), 0, 1);
        root.Controls.Add(BuildSetupPanel(), 0, 2);
        root.Controls.Add(BuildLivePanel(), 0, 3);
        root.Controls.Add(BuildRecordingPanel(), 0, 4);
        root.Controls.Add(BuildGrid(), 0, 5);
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(new Label
        {
            Text = "矫正数据集采集",
            Font = new Font(Font.FontFamily, 19F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize = true,
            Location = new Point(2, 2)
        });
        panel.Controls.Add(new Label
        {
            Text = "只采集、不控制门锁；COM21 与 COM22 会在程序运行期间持续自动重连",
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Location = new Point(4, 38)
        });
        return panel;
    }

    private Control BuildConnectionPanel()
    {
        var group = NewGroup("串口连接（固定 115200、8N1）");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8, 2, 8, 4)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        table.Controls.Add(StatusTitle("蓝牙/声光板  COM21（只接收）"), 0, 0);
        table.Controls.Add(StatusTitle("120°基站  COM22（只接收）"), 1, 0);
        ConfigureStatusLabel(_bluetoothStatus);
        ConfigureStatusLabel(_anchorStatus);
        table.Controls.Add(_bluetoothStatus, 0, 1);
        table.Controls.Add(_anchorStatus, 1, 1);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildSetupPanel()
    {
        var group = NewGroup("标定点设置");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 2,
            Padding = new Padding(8, 5, 8, 4)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        table.Controls.Add(FieldLabel("题目实际距离（m）"), 0, 0);
        ConfigureNumber(_actualDistance, 0, 20, 2, 0.10M, 1.00M);
        table.Controls.Add(_actualDistance, 1, 0);
        table.Controls.Add(FieldLabel("实际角度（°）"), 2, 0);
        ConfigureNumber(_actualAngle, -180, 180, 1, 1M, 0M);
        table.Controls.Add(_actualAngle, 3, 0);
        table.Controls.Add(FieldLabel("采集时长（s）"), 4, 0);
        ConfigureNumber(_duration, 1, 300, 1, 1M, 20M);
        table.Controls.Add(_duration, 5, 0);

        _startButton.Text = "开始记录";
        StyleButton(_startButton, Color.FromArgb(2, 132, 199));
        _startButton.Dock = DockStyle.Fill;
        table.Controls.Add(_startButton, 6, 0);

        _distanceMeaning.AutoSize = true;
        _distanceMeaning.ForeColor = Color.FromArgb(3, 105, 161);
        _distanceMeaning.Font = new Font(Font, FontStyle.Bold);
        _distanceMeaning.Anchor = AnchorStyles.Left;
        table.Controls.Add(_distanceMeaning, 0, 1);
        table.SetColumnSpan(_distanceMeaning, 7);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildLivePanel()
    {
        var group = NewGroup("实时数据（记录开始前也可预览）");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(8, 3, 8, 3)
        };
        for (var i = 0; i < 6; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666F));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titles = new[] { "原始中心距", "标定+30点中值", "原始方位角", "标定+30点中值", "信标4位ID", "DIP放行ID" };
        var values = new[] { _rawDistanceValue, _correctedDistanceValue, _rawAngleValue, _correctedAngleValue, _tagIdValue, _allowedIdValue };
        for (var i = 0; i < titles.Length; i++)
        {
            table.Controls.Add(StatusTitle(titles[i]), i, 0);
            values[i].Text = "—";
            values[i].Dock = DockStyle.Fill;
            values[i].TextAlign = ContentAlignment.MiddleCenter;
            values[i].Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            values[i].ForeColor = Color.FromArgb(15, 23, 42);
            table.Controls.Add(values[i], i, 1);
        }
        group.Controls.Add(table);
        return group;
    }

    private Control BuildRecordingPanel()
    {
        var group = NewGroup("记录状态");
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Padding = new Padding(8, 3, 8, 4)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _recordStatus.Text = "尚未开始记录";
        _recordStatus.Dock = DockStyle.Fill;
        _recordStatus.TextAlign = ContentAlignment.MiddleLeft;
        _recordStatus.Font = new Font(Font, FontStyle.Bold);
        _recordStatus.ForeColor = Color.FromArgb(71, 85, 105);
        table.Controls.Add(_recordStatus, 0, 0);

        _progress.Dock = DockStyle.Fill;
        _progress.Minimum = 0;
        _progress.Maximum = 1000;
        _progress.Margin = new Padding(6, 8, 10, 8);
        table.Controls.Add(_progress, 1, 0);

        _frameCountValue.Text = "定位帧：0";
        _frameCountValue.Dock = DockStyle.Fill;
        _frameCountValue.TextAlign = ContentAlignment.MiddleCenter;
        table.Controls.Add(_frameCountValue, 2, 0);

        _stopButton.Text = "停止记录";
        _stopButton.Enabled = false;
        StyleButton(_stopButton, Color.FromArgb(220, 38, 38));
        _stopButton.Dock = DockStyle.Fill;
        table.Controls.Add(_stopButton, 3, 0);

        _openFolderButton.Text = "打开数据文件夹";
        _openFolderButton.Enabled = false;
        StyleButton(_openFolderButton, Color.FromArgb(71, 85, 105));
        _openFolderButton.Dock = DockStyle.Fill;
        table.Controls.Add(_openFolderButton, 4, 0);

        _savePathValue.Text = $"数据保存位置：{DataRoot}";
        _savePathValue.Dock = DockStyle.Fill;
        _savePathValue.AutoEllipsis = true;
        _savePathValue.ForeColor = Color.FromArgb(71, 85, 105);
        _savePathValue.TextAlign = ContentAlignment.MiddleLeft;
        table.Controls.Add(_savePathValue, 0, 1);
        table.SetColumnSpan(_savePathValue, 5);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildGrid()
    {
        var group = NewGroup("最近收到的定位帧");
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.Columns.Add("time", "时间");
        _grid.Columns.Add("rawDistance", "原始中心距/m");
        _grid.Columns.Add("correctedDistance", "标定+30中值/m");
        _grid.Columns.Add("rawAngle", "原始角度/°");
        _grid.Columns.Add("correctedAngle", "标定+30中值/°");
        _grid.Columns.Add("tag", "信标4位ID");
        _grid.Columns.Add("checksum", "校验");
        group.Controls.Add(_grid);
        return group;
    }

    private void WireEvents()
    {
        Shown += (_, _) =>
        {
            _reconnectTimer.Start();
            _uiTimer.Start();
            _ = _bluetooth.EnsureConnectedAsync();
            _ = _anchor.EnsureConnectedAsync();
        };
        FormClosing += (_, _) => StopRecording("程序关闭", false);
        FormClosed += (_, _) =>
        {
            _reconnectTimer.Stop();
            _uiTimer.Stop();
            _bluetooth.Dispose();
            _anchor.Dispose();
        };

        _reconnectTimer.Tick += (_, _) =>
        {
            _ = _bluetooth.EnsureConnectedAsync();
            _ = _anchor.EnsureConnectedAsync();
        };
        _uiTimer.Tick += (_, _) => OnUiTick();
        _actualDistance.ValueChanged += (_, _) => UpdateDistanceMeaning();
        _startButton.Click += (_, _) => StartRecording();
        _stopButton.Click += (_, _) => StopRecording("手动停止", true);
        _openFolderButton.Click += (_, _) => OpenFolder(_lastSessionFolder ?? DataRoot);

        _bluetooth.BytesReceived += HandleBluetoothRead;
        _anchor.BytesReceived += HandleAnchorRead;
        _bluetooth.StateChanged += HandleConnectionStateChanged;
        _anchor.StateChanged += HandleConnectionStateChanged;
    }

    private void HandleBluetoothRead(SerialRead read)
    {
        _recorder?.WriteRaw(read);
        IReadOnlyList<BluetoothFrame> frames;
        lock (_bluetoothParserLock)
            frames = _bluetoothParser.Append(read.Bytes, read.ReceivedAt);

        foreach (var frame in frames)
        {
            _recorder?.WriteBluetooth(frame);
            if (!frame.CrcValid || !frame.AllowedKeyId.HasValue)
                continue;
            Interlocked.Exchange(ref _latestAllowedId, frame.AllowedKeyId.Value);
            InvokeUi(() => _allowedIdValue.Text = Convert.ToString(frame.AllowedKeyId.Value, 2).PadLeft(4, '0'));
        }
    }

    private void HandleAnchorRead(SerialRead read)
    {
        _recorder?.WriteRaw(read);
        IReadOnlyList<AnchorFrame> frames;
        lock (_anchorParserLock)
            frames = _anchorParser.Append(read.Bytes, read.ReceivedAt);

        foreach (var frame in frames)
        {
            if (frame.Kind != AnchorFrameKind.Position)
                continue;
            ProcessedMeasurement? processed = null;
            if (frame.ChecksumValid && frame.DistanceCm <= 1000 && Math.Abs(frame.AzimuthDeg) <= 180)
            {
                lock (_measurementProcessorLock)
                    processed = _measurementProcessor.Add(frame.RawDistanceM, frame.AzimuthDeg);
            }
            var allowedId = Interlocked.CompareExchange(ref _latestAllowedId, -1, -1);
            _recorder?.WriteMeasurement(frame, processed, allowedId >= 0 ? allowedId : null);
            Interlocked.Increment(ref _positionFramesSeen);
            if (frame.ChecksumValid)
                Interlocked.Increment(ref _validPositionFramesSeen);
            InvokeUi(() => DisplayFrame(frame, processed));
        }
    }

    private void HandleConnectionStateChanged(SerialEndpoint endpoint)
    {
        _recorder?.WriteEvent(
            endpoint.IsConnected ? "serial_connected" : "serial_disconnected",
            $"{endpoint.Role}/{endpoint.PortName}: {(endpoint.IsConnected ? "connected" : endpoint.LastError)}");
        InvokeUi(UpdateConnectionStatus);
    }

    private void StartRecording()
    {
        if (_recorder is not null)
            return;

        Directory.CreateDirectory(DataRoot);
        lock (_anchorParserLock)
            _anchorParser.Reset();
        lock (_bluetoothParserLock)
            _bluetoothParser.Reset();
        lock (_measurementProcessorLock)
            _measurementProcessor.Clear();
        Interlocked.Exchange(ref _positionFramesSeen, 0);
        Interlocked.Exchange(ref _validPositionFramesSeen, 0);

        _recorder = new SessionRecorder(
            DataRoot,
            (double)_actualDistance.Value,
            (double)_actualAngle.Value,
            (double)_duration.Value);
        _lastSessionFolder = _recorder.SessionFolder;
        _recorder.WriteEvent("serial_state", $"COM21={_bluetooth.IsConnected}; COM22={_anchor.IsConnected}");

        _actualDistance.Enabled = false;
        _actualAngle.Enabled = false;
        _duration.Enabled = false;
        _startButton.Enabled = false;
        _stopButton.Enabled = true;
        _openFolderButton.Enabled = false;
        _recordStatus.Text = "● 正在记录";
        _recordStatus.ForeColor = Color.FromArgb(220, 38, 38);
        _savePathValue.Text = $"本次保存位置：{_lastSessionFolder}";
        _progress.Value = 0;
    }

    private void StopRecording(string reason, bool showCompletion)
    {
        var recorder = _recorder;
        if (recorder is null)
            return;
        _recorder = null;
        recorder.Stop(reason);

        _actualDistance.Enabled = true;
        _actualAngle.Enabled = true;
        _duration.Enabled = true;
        _startButton.Enabled = true;
        _stopButton.Enabled = false;
        _openFolderButton.Enabled = true;
        _recordStatus.Text = $"已停止 · 有效定位帧 {recorder.ValidPositionFrameCount}";
        _recordStatus.ForeColor = Color.FromArgb(22, 163, 74);
        _progress.Value = _progress.Maximum;
        _savePathValue.Text = $"已保存：{recorder.SessionFolder}";

        if (showCompletion)
            System.Media.SystemSounds.Asterisk.Play();
    }

    private void OnUiTick()
    {
        UpdateConnectionStatus();
        _frameCountValue.Text = $"定位帧：{Interlocked.Read(ref _validPositionFramesSeen)}/{Interlocked.Read(ref _positionFramesSeen)}";
        var recorder = _recorder;
        if (recorder is null)
            return;

        var elapsed = (DateTimeOffset.Now - recorder.StartedAt).TotalSeconds;
        var duration = (double)_duration.Value;
        var ratio = Math.Clamp(elapsed / duration, 0, 1);
        _progress.Value = (int)Math.Round(ratio * _progress.Maximum);
        _recordStatus.Text = $"● 正在记录 {elapsed:F1}/{duration:F1} s";
        if (elapsed >= duration)
            StopRecording("达到设定时长，自动停止", true);
    }

    private void DisplayFrame(AnchorFrame frame, ProcessedMeasurement? processed)
    {
        _rawDistanceValue.Text = $"{frame.RawDistanceM:F2} m";
        _correctedDistanceValue.Text = processed.HasValue
            ? $"{processed.Value.SmoothedDistanceM:F2} m · {processed.Value.SmoothingSampleCount}/30"
            : "—";
        _rawAngleValue.Text = $"{frame.AzimuthDeg:+0.0;-0.0;0.0}°";
        _correctedAngleValue.Text = processed.HasValue
            ? $"{processed.Value.SmoothedAngleDeg:+0.0;-0.0;0.0}° · {processed.Value.SmoothingSampleCount}/30"
            : "—";
        _tagIdValue.Text = Convert.ToString(frame.FourBitId, 2).PadLeft(4, '0');

        _grid.Rows.Insert(0,
            frame.ReceivedAt.ToString("HH:mm:ss.fff"),
            frame.RawDistanceM.ToString("F2"),
            processed?.SmoothedDistanceM.ToString("F3") ?? string.Empty,
            frame.AzimuthDeg.ToString("+0.0;-0.0;0.0"),
            processed?.SmoothedAngleDeg.ToString("+0.0;-0.0;0.0") ?? string.Empty,
            Convert.ToString(frame.FourBitId, 2).PadLeft(4, '0'),
            frame.ChecksumValid
                ? processed?.SmoothingReady == true ? "正常·已满30点" : $"预热 {processed?.SmoothingSampleCount ?? 0}/30"
                : "失败");
        if (!frame.ChecksumValid)
            _grid.Rows[0].DefaultCellStyle.ForeColor = Color.FromArgb(220, 38, 38);
        while (_grid.Rows.Count > 60)
            _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
    }

    private void UpdateConnectionStatus()
    {
        SetEndpointStatus(_bluetoothStatus, _bluetooth);
        SetEndpointStatus(_anchorStatus, _anchor);
    }

    private static void SetEndpointStatus(Label label, SerialEndpoint endpoint)
    {
        if (endpoint.IsConnected)
        {
            label.Text = $"● 已连接 · 已收 {endpoint.BytesReceivedCount:N0} 字节";
            label.ForeColor = Color.FromArgb(22, 163, 74);
        }
        else
        {
            label.Text = $"● 持续重试中（第 {endpoint.ConnectAttempts} 次）· {endpoint.LastError}";
            label.ForeColor = Color.FromArgb(220, 38, 38);
        }
    }

    private void UpdateDistanceMeaning()
    {
        var subject = (double)_actualDistance.Value;
        _distanceMeaning.Text =
            $"距离定义：输入 {subject:F2} m；按现场结果，期望基站中心距 {subject + CalibrationModel.DistanceCorrectionM:F2} m。CSV同时保存原始值、场地分段校准值和30点中值结果。";
    }

    private static string DataRoot
    {
        get
        {
            var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(Path.GetFileName(baseDirectory), "发布版", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(Directory.GetParent(baseDirectory)!.FullName, DataFolderName);
            return Path.Combine(baseDirectory, DataFolderName);
        }
    }

    private void InvokeUi(Action action)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
            return;
        try { BeginInvoke(action); } catch (InvalidOperationException) { }
    }

    private static void OpenFolder(string folder)
    {
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }

    private static GroupBox NewGroup(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(30, 41, 59),
        Padding = new Padding(8)
    };

    private static Label StatusTitle(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(71, 85, 105)
    };

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        ForeColor = Color.FromArgb(51, 65, 85)
    };

    private static void ConfigureStatusLabel(Label label)
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Font = new Font(label.Font, FontStyle.Bold);
    }

    private static void ConfigureNumber(
        NumericUpDown number, decimal minimum, decimal maximum, int decimals, decimal increment, decimal value)
    {
        number.Minimum = minimum;
        number.Maximum = maximum;
        number.DecimalPlaces = decimals;
        number.Increment = increment;
        number.Value = value;
        number.Dock = DockStyle.Fill;
        number.Margin = new Padding(5, 6, 8, 6);
        number.TextAlign = HorizontalAlignment.Center;
    }

    private static void StyleButton(Button button, Color color)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.Font = new Font(button.Font, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(5, 4, 5, 4);
    }
}
