using System.Drawing.Drawing2D;

namespace DigitalKeyLab;

public sealed class RadarControl : Control
{
    public double DistanceM { get; set; }
    public double AngleDeg { get; set; }
    public bool HasPosition { get; set; }
    public DoorDecision Decision { get; set; } = DoorLogic.Evaluate(false, 0, 0, 0, null);
    public string TagText { get; set; } = "等待信标";

    public RadarControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(8, 18, 36);
        ForeColor = Color.White;
        Font = new Font("Microsoft YaHei UI", 10f);
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var gridPen = new Pen(Color.FromArgb(95, 148, 163, 184), 1.2f);
        using var strongPen = new Pen(Color.FromArgb(190, 226, 232, 240), 1.8f);
        using var centerPen = new Pen(Color.FromArgb(190, 56, 189, 248), 2.2f) { DashStyle = DashStyle.Dash };
        using var labelBrush = new SolidBrush(Color.FromArgb(215, 226, 232, 240));
        using var subtleBrush = new SolidBrush(Color.FromArgb(180, 148, 163, 184));

        var cx = Width / 2f;
        var originY = 78f;
        var radius = Math.Max(120f, Math.Min((Width - 90f) / 2f, Height - 175f));

        DrawHeader(g, cx);
        FillSector(g, cx, originY, radius, Color.FromArgb(88, 14, 165, 233));
        FillSector(g, cx, originY, radius * 2f / 3f, Color.FromArgb(105, 245, 158, 11));
        FillSector(g, cx, originY, radius / 3f, Color.FromArgb(115, 34, 197, 94));

        for (var meter = 1; meter <= 3; meter++)
        {
            var r = radius * meter / 3f;
            var rect = CircleRect(cx, originY, r);
            g.DrawArc(meter == 3 ? strongPen : gridPen, rect, 45, 90);
            var label = $"{meter} m";
            var size = g.MeasureString(label, Font);
            g.FillRectangle(new SolidBrush(BackColor), cx - size.Width / 2f - 3, originY + r - size.Height / 2f, size.Width + 6, size.Height);
            g.DrawString(label, Font, labelBrush, cx - size.Width / 2f, originY + r - size.Height / 2f);
        }

        foreach (var angle in new[] { -45, -30, -15, 0, 15, 30, 45 })
        {
            var rad = DegreesToRadians(angle);
            var x = cx + radius * (float)Math.Sin(rad);
            var y = originY + radius * (float)Math.Cos(rad);
            g.DrawLine(angle == 0 ? centerPen : gridPen, cx, originY, x, y);
            var text = angle == 0 ? "0° 正前方" : $"{angle:+#;-#}°";
            var size = g.MeasureString(text, Font);
            var tx = x - size.Width / 2f;
            var ty = y + (angle == 0 ? 7 : 1);
            g.DrawString(text, Font, angle == 0 ? labelBrush : subtleBrush, tx, ty);
        }

        DrawLock(g, cx, originY);
        DrawZoneLabels(g, cx, originY, radius);
        if (HasPosition)
            DrawTag(g, cx, originY, radius);
        else
            DrawWaiting(g, cx, originY + radius * 0.56f);

        using var borderPen = new Pen(Color.FromArgb(70, 148, 163, 184));
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    private void DrawHeader(Graphics g, float cx)
    {
        using var titleFont = new Font("Microsoft YaHei UI", 16f, FontStyle.Bold);
        using var subFont = new Font("Microsoft YaHei UI", 9.5f);
        using var white = new SolidBrush(Color.White);
        using var gray = new SolidBrush(Color.FromArgb(185, 203, 213, 225));
        const string title = "120° 基站正面定位图";
        const string sub = "竞赛有效范围：正前方 ±45° / 0–3 m";
        var t = g.MeasureString(title, titleFont);
        var s = g.MeasureString(sub, subFont);
        g.DrawString(title, titleFont, white, cx - t.Width / 2f, 13);
        g.DrawString(sub, subFont, gray, cx - s.Width / 2f, 43);
    }

    private void DrawLock(Graphics g, float cx, float originY)
    {
        using var fill = new SolidBrush(Color.FromArgb(30, 41, 59));
        using var border = new Pen(Color.FromArgb(125, 211, 252), 2f);
        using var arrow = new Pen(Color.FromArgb(56, 189, 248), 3f) { EndCap = LineCap.ArrowAnchor };
        var body = new RectangleF(cx - 25, originY - 25, 50, 35);
        g.FillRoundedRectangle(fill, body, 9);
        g.DrawRoundedRectangle(border, body, 9);
        g.DrawArc(border, cx - 13, originY - 43, 26, 30, 180, 180);
        g.DrawLine(arrow, cx, originY + 14, cx, originY + 46);
    }

    private void DrawZoneLabels(Graphics g, float cx, float originY, float radius)
    {
        using var font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold);
        DrawPill(g, "开锁区", cx, originY + radius * 0.18f, Color.FromArgb(34, 197, 94), font);
        DrawPill(g, "迎宾区", cx, originY + radius * 0.50f, Color.FromArgb(245, 158, 11), font);
        DrawPill(g, "感应区", cx, originY + radius * 0.83f, Color.FromArgb(14, 165, 233), font);
    }

    private void DrawTag(Graphics g, float cx, float originY, float radius)
    {
        var plottedDistance = Math.Min(3.12, Math.Max(0, DistanceM));
        var plottedAngle = Math.Max(-55, Math.Min(55, AngleDeg));
        var r = radius * (float)(plottedDistance / 3.0);
        var rad = DegreesToRadians(plottedAngle);
        var x = cx + r * (float)Math.Sin(rad);
        var y = originY + r * (float)Math.Cos(rad);
        var color = Decision.IdentityMatched ? Decision.ZoneColor : Color.FromArgb(239, 68, 68);

        using var glow = new SolidBrush(Color.FromArgb(60, color));
        using var fill = new SolidBrush(color);
        using var whitePen = new Pen(Color.White, 2.4f);
        g.FillEllipse(glow, x - 20, y - 20, 40, 40);
        g.FillEllipse(fill, x - 10, y - 10, 20, 20);
        g.DrawEllipse(whitePen, x - 10, y - 10, 20, 20);

        using var tagFont = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
        var text = $"{TagText}\n{DistanceM:F2} m  {AngleDeg:+0.0;-0.0;0.0}°";
        var size = g.MeasureString(text, tagFont);
        var box = new RectangleF(x + 15, y - size.Height / 2f - 7, size.Width + 16, size.Height + 14);
        if (box.Right > Width - 8)
            box.X = x - 15 - box.Width;
        using var boxBrush = new SolidBrush(Color.FromArgb(225, 15, 23, 42));
        using var boxPen = new Pen(Color.FromArgb(140, color), 1.2f);
        g.FillRoundedRectangle(boxBrush, box, 8);
        g.DrawRoundedRectangle(boxPen, box, 8);
        g.DrawString(text, tagFont, Brushes.White, box.X + 8, box.Y + 7);
    }

    private static void DrawWaiting(Graphics g, float cx, float y)
    {
        using var font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(160, 148, 163, 184));
        const string text = "等待 0x2001 定位帧…";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, cx - size.Width / 2, y);
    }

    private static void DrawPill(Graphics g, string text, float cx, float y, Color color, Font font)
    {
        var size = g.MeasureString(text, font);
        var rect = new RectangleF(cx - size.Width / 2f - 10, y - size.Height / 2f - 4, size.Width + 20, size.Height + 8);
        using var fill = new SolidBrush(Color.FromArgb(205, 15, 23, 42));
        using var pen = new Pen(Color.FromArgb(150, color), 1.2f);
        g.FillRoundedRectangle(fill, rect, 10);
        g.DrawRoundedRectangle(pen, rect, 10);
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, rect.X + 10, rect.Y + 4);
    }

    private static void FillSector(Graphics g, float cx, float cy, float radius, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillPie(brush, CircleRect(cx, cy, radius), 45, 90);
    }

    private static RectangleF CircleRect(float cx, float cy, float radius) =>
        new(cx - radius, cy - radius, radius * 2, radius * 2);

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedPath(rect, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF rect, float radius)
    {
        using var path = RoundedPath(rect, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedPath(RectangleF rect, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
