namespace DigitalKeyLab;

public sealed class GlobalCalibrationForm : Form
{
    private readonly List<GlobalCalibrationAdjustment> _adjustments;
    private readonly Action _adjustmentsChanged;
    private readonly DataGridView _grid = new();
    private readonly Label _rawDistance = MetricValue();
    private readonly Label _rawAngle = MetricValue();
    private readonly Label _calibratedDistance = MetricValue();
    private readonly Label _calibratedAngle = MetricValue();
    private int _activeRowIndex = -1;

    public GlobalCalibrationForm(
        List<GlobalCalibrationAdjustment> adjustments,
        Action adjustmentsChanged)
    {
        _adjustments = adjustments;
        _adjustmentsChanged = adjustmentsChanged;
        Text = "全局校准";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1120, 720);
        MinimumSize = new Size(980, 600);
        BackColor = Color.FromArgb(241, 245, 249);
        Font = new Font("Microsoft YaHei UI", 9f);
        TopMost = true;
        BuildUi();
        Shown += (_, _) => ClearGridSelection();
    }

    public void UpdateMeasurement(
        double rawDistanceM,
        double rawAngleDeg,
        GlobalCalibrationResult calibrated)
    {
        _rawDistance.Text = $"{rawDistanceM:F2} m";
        _rawAngle.Text = $"{rawAngleDeg:+0.0;-0.0;0.0}°";
        _calibratedDistance.Text = $"{calibrated.DistanceM:F2} m";
        _calibratedAngle.Text = $"{calibrated.AngleDeg:+0.0;-0.0;0.0}°";
        var nextActiveRow = calibrated.HasMatchingRange
            ? calibrated.AngleRangeIndex * GlobalCalibrationModel.DistanceRanges.Count +
              calibrated.DistanceRangeIndex
            : -1;
        if (nextActiveRow != _activeRowIndex)
        {
            _activeRowIndex = nextActiveRow;
            RefreshRowColors();
        }
    }

    public void SetUnavailable()
    {
        _rawDistance.Text = "— m";
        _rawAngle.Text = "— °";
        _calibratedDistance.Text = "— m";
        _calibratedAngle.Text = "— °";
        _activeRowIndex = -1;
        RefreshRowColors();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(10),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var metrics = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 0, 8)
        };
        for (var index = 0; index < 4; index++)
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        metrics.Controls.Add(Metric("基站原始距离", _rawDistance), 0, 0);
        metrics.Controls.Add(Metric("基站原始角度", _rawAngle), 1, 0);
        metrics.Controls.Add(Metric("全局校准后距离", _calibratedDistance), 2, 0);
        metrics.Controls.Add(Metric("全局校准后角度", _calibratedAngle), 3, 0);
        root.Controls.Add(metrics, 0, 0);

        ConfigureGrid();
        root.Controls.Add(_grid, 0, 1);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AllowUserToOrderColumns = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeadersHeight = 38;
        _grid.RowTemplate.Height = 30;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(187, 247, 208);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

        AddTextColumn("angleRange", "原始角度区间/°", 115);
        AddTextColumn("distanceRange", "原始距离区间/m", 125);
        AddTextColumn("distanceValue", "距离修正/m", 90);
        AddButtonColumn("distanceMinusOne", "-1.0", 56);
        AddButtonColumn("distanceMinusPointOne", "-0.1", 56);
        AddButtonColumn("distancePlusPointOne", "+0.1", 56);
        AddButtonColumn("distancePlusOne", "+1.0", 56);
        AddTextColumn("angleValue", "角度修正/°", 90);
        AddButtonColumn("angleMinusOne", "-1.0", 56);
        AddButtonColumn("angleMinusPointOne", "-0.1", 56);
        AddButtonColumn("anglePlusPointOne", "+0.1", 56);
        AddButtonColumn("anglePlusOne", "+1.0", 56);

        foreach (var adjustment in _adjustments)
        {
            var angleText = adjustment.DistanceRangeIndex == 0
                ? GlobalCalibrationModel.FormatAngleRange(adjustment.AngleRangeIndex)
                : string.Empty;
            var rowIndex = _grid.Rows.Add(
                angleText,
                GlobalCalibrationModel.FormatDistanceRange(adjustment.DistanceRangeIndex),
                FormatCorrection(adjustment.DistanceCorrectionM),
                "-1.0", "-0.1", "+0.1", "+1.0",
                FormatCorrection(adjustment.AngleCorrectionDeg),
                "-1.0", "-0.1", "+0.1", "+1.0");
            _grid.Rows[rowIndex].Tag = adjustment;
        }
        _grid.CellContentClick += HandleCellContentClick;
        ClearGridSelection();
        RefreshRowColors();
    }

    private void HandleCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            _grid.Rows[e.RowIndex].Tag is not GlobalCalibrationAdjustment adjustment)
            return;

        decimal delta;
        var distance = false;
        switch (e.ColumnIndex)
        {
            case 3: distance = true; delta = -1.0m; break;
            case 4: distance = true; delta = -0.1m; break;
            case 5: distance = true; delta = 0.1m; break;
            case 6: distance = true; delta = 1.0m; break;
            case 8: delta = -1.0m; break;
            case 9: delta = -0.1m; break;
            case 10: delta = 0.1m; break;
            case 11: delta = 1.0m; break;
            default: return;
        }

        if (distance)
        {
            adjustment.DistanceCorrectionM = GlobalCalibrationModel.AddCorrection(
                adjustment.DistanceCorrectionM, delta);
            _grid.Rows[e.RowIndex].Cells[2].Value = FormatCorrection(adjustment.DistanceCorrectionM);
        }
        else
        {
            adjustment.AngleCorrectionDeg = GlobalCalibrationModel.AddCorrection(
                adjustment.AngleCorrectionDeg, delta);
            _grid.Rows[e.RowIndex].Cells[7].Value = FormatCorrection(adjustment.AngleCorrectionDeg);
        }
        _adjustmentsChanged();
    }

    private void RefreshRowColors()
    {
        for (var index = 0; index < _grid.Rows.Count; index++)
        {
            var row = _grid.Rows[index];
            var angleGroup = index / GlobalCalibrationModel.DistanceRanges.Count;
            row.DefaultCellStyle.BackColor = index == _activeRowIndex
                ? Color.FromArgb(220, 252, 231)
                : angleGroup % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
        }
        ClearGridSelection();
    }

    private void AddTextColumn(string name, string header, int width) =>
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            Width = width,
            FillWeight = width,
            MinimumWidth = Math.Min(width, 70),
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

    private void AddButtonColumn(string name, string header, int width) =>
        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = name,
            HeaderText = header,
            Width = width,
            FillWeight = width,
            MinimumWidth = 46,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            UseColumnTextForButtonValue = false
        });

    private static Control Metric(string caption, Label value)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(71, 85, 105)
        }, 0, 0);
        value.Dock = DockStyle.Fill;
        panel.Controls.Add(value, 0, 1);
        return panel;
    }

    private static Label MetricValue() => new()
    {
        Text = "—",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Microsoft YaHei UI", 16f, FontStyle.Bold),
        ForeColor = Color.FromArgb(15, 23, 42)
    };

    private static string FormatCorrection(decimal value) =>
        value.ToString("+0.0;-0.0;0.0");

    private void ClearGridSelection()
    {
        _grid.ClearSelection();
        _grid.CurrentCell = null;
    }
}
