using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace OffDutyCounter;

internal sealed class Dashboard : Panel
{
    private static readonly CultureInfo Chinese = CultureInfo.GetCultureInfo("zh-CN");
    private readonly Button[] buttons = new Button[5];
    private Button? quoteButton;
    private Color ink, muted, accent, card, track, badge;
    private float ScaleFactor => Math.Max(0.1f, Math.Min(ClientSize.Width / (Compact ? 260f : 380f), ClientSize.Height / (Compact ? 42f : 572f)));
    private float LeftOffset => (ClientSize.Width - (Compact ? 260 : 380) * ScaleFactor) / 2;
    private readonly ToolTip tips = new();
    internal Button ThemeButton => buttons[2];
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Compact { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Pinned { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal DateTime Now { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Snapshot? Result { get; private set; }
    private string currentAmount = "¥0.00000";
    private string previousAmount = "¥0.00000";
    private long rollStarted;
    private readonly System.Windows.Forms.Timer rollTimer = new() { Interval = 15 };

    internal void SetResult(Snapshot? snapshot)
    {
        bool hadValue = Result is not null;
        Result = snapshot;
        if (Visible && Spinning) rollTimer.Start();
        string value = "¥" + Money(snapshot?.Amount ?? 0, "N5");
        if (value == currentAmount) return;
        previousAmount = hadValue && snapshot is not null && Visible ? currentAmount : value;
        currentAmount = value;
        rollStarted = Stopwatch.GetTimestamp();
        if (previousAmount != currentAmount) rollTimer.Start();
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string Quote { get; set; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string? RewardName { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string? RewardIcon { get; set; }

    public Dashboard()
    {
        DoubleBuffered = true;
        rollTimer.Tick += (_, _) =>
        {
            if (!Visible) { rollTimer.Stop(); return; }
            if (previousAmount != currentAmount)
            {
                if (RollProgress >= 1) previousAmount = currentAmount;
                Invalidate();
            }
            else if (Spinning)
            {
                float scale = ScaleFactor;
                Invalidate(Rectangle.Ceiling(new RectangleF(LeftOffset + 93 * scale, 45 * scale, 25 * scale, 25 * scale)));
            }
            else rollTimer.Stop();
        };
        SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);

    }

    internal bool IsTitleArea(int y) => y < 42 * ScaleFactor;

    public void Configure(params Action[] actions)
    {
        string[] labels = ["置顶", "设置", "主题", "展开或折叠", "收进托盘"];
        string[] glyphs = ["\uE718", "\uE713", "\uE790", "\uE740", "\uE739"];
        for (int i = 0; i < labels.Length; i++)
        {
            var button = new SymbolButton { Glyph = glyphs[i], AccessibleName = labels[i], Cursor = Cursors.Hand };
            var action = actions[i];
            button.Click += (_, _) => action();
            tips.SetToolTip(button, labels[i]);
            buttons[i] = button;
            Controls.Add(button);
        }
        quoteButton = new QuoteButton { AccessibleName = "换一句摸鱼语录", Cursor = Cursors.Hand };
        quoteButton.Click += (_, _) => actions[5]();
        tips.SetToolTip(quoteButton, "点击换一句，每五分钟自动切换");
        Controls.Add(quoteButton);
    }

    public void SetColors(Color background, Color foreground, Color tint, bool dark)
    {
        BackColor = background;
        ink = foreground;
        accent = tint;
        muted = dark ? Color.FromArgb(173, 183, 200) : Color.FromArgb(112, 116, 123);
        card = Blend(background, foreground, dark ? 0.08f : 0.045f);
        track = Blend(background, foreground, 0.1f);
        badge = Blend(card, accent, 0.12f);
        foreach (var button in buttons) { button.BackColor = background; button.ForeColor = tint; }
        if (quoteButton is not null) { quoteButton.BackColor = background; quoteButton.ForeColor = ink; }
        Invalidate(true);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (quoteButton is null) return;
        float scale = ScaleFactor;
        Rectangle At(int x, int y, int w, int h) => Rectangle.Round(new RectangleF(LeftOffset + x * scale, y * scale, w * scale, h * scale));
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Visible = !Compact;
            if (!Compact) buttons[i].Bounds = At(224 + 28 * i, 10, 25, 26);
        }
        tips.SetToolTip(this, Compact ? "双击展开" : null);
        quoteButton.Visible = !Compact;
        quoteButton.Bounds = At(22, 506, 336, 48);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float scale = ScaleFactor;
        var g = e.Graphics;
        g.TranslateTransform(LeftOffset, 0);
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var cycleBounds = Rectangle.Ceiling(new RectangleF(LeftOffset + 93 * scale, 45 * scale, 25 * scale, 25 * scale));
        if (Spinning && cycleBounds.Contains(e.ClipRectangle))
        {
            DrawCycle(g);
            return;
        }
        if (buttons[0] is SymbolButton pin) pin.Glyph = Pinned ? "\uE77A" : "\uE718";
        if (buttons[3] is SymbolButton expand) expand.Glyph = Compact ? "\uE740" : "\uE73F";
        if (quoteButton is QuoteButton quote) quote.UpdateContent(Quote, accent, card, muted);

        if (Compact)
        {
            DrawCompact(g);
            return;
        }

        TextAt(g, Now.ToString("yyyy-MM-dd dddd HH:mm:ss", Chinese), 22, 13, 200, 10.5f, muted);
        var s = Result;
        TextAt(g, s?.State ?? "请先设置", 22, 45, 225, 22, ink, true);
        if (Spinning) DrawCycle(g);
        else if (s is not null) TextAt(g, "zzz", 96, 47, 38, 16, muted, true);
        TextAt(g, "今日已赚", 22, 85, 336, 11, muted);
        DrawAmount(g, 22, 107, 336, 44);
        if (s is null) return;

        if (s.Next is DateTime next)
        {
            var left = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling((next - Now).TotalSeconds)));
            TextAt(g, $"距离{s.Event}还有 {(int)left.TotalHours} 小时 {left.Minutes} 分 {left.Seconds} 秒", 22, 175, 336, 12.5f, ink);
        }
        TextAt(g, $"当前赚钱速度 ¥{Money(s.Rate, "N5")}/秒 · ¥{Money(s.Rate * 60)}/分钟", 22, 204, 336, 11, muted);
        TextAt(g, $"本日还有 {Money(s.Remaining)} 元待领取", 22, 222, 336, 11, muted);
        double fraction = s.Elapsed / s.Planned;
        TextAt(g, $"今日工时进度 {fraction:P1}", 22, 249, 170, 11, muted);
        TextAt(g, $"{s.Elapsed / 3600:F2} / {s.Planned / 3600:F2} 小时", 190, 249, 168, 11, muted, right: true);
        Round(g, track, 22, 273, 336, 8, 4);
        if (fraction > 0) Round(g, accent, 22, 273, (float)(336 * fraction), 8, 4);
        using (var pen = new Pen(track)) g.DrawLine(pen, 22, 300, 358, 300);

        Round(g, card, 22, 313, 336, 83, 13);
        TextAt(g, "本日收获", 34, 325, 230, 15, ink, true);
        string reward = RewardName ?? $"{Money(s.Amount)}元";
        TextAt(g, $"今天已经赚到{reward}啦，继续加油", 34, 349, 264, 11, ink);
        Round(g, badge, 304, 324, 42, 42, 11);
        TextAt(g, RewardIcon ?? "✦", 313, 330, 28, 21, accent, family: "Segoe UI Symbol");
        TextAt(g, $"时薪 {Money(s.BaseRate * 3600)}元 · 分薪 {Money(s.BaseRate * 60)}元 · 带薪喝水 {Money(s.BaseRate * 300)}元/次 · 带薪拉屎 {Money(s.BaseRate * 1800)}元/次",
            34, 375, 312, 8.5f, muted);

        Round(g, card, 22, 409, 336, 83, 13);
        TextAt(g, "本月收获", 34, 421, 145, 15, ink, true);
        TextAt(g, $"¥{Money(s.Month)}", 179, 418, 167, 19, accent, true, right: true);
        string payday = s.Payday switch { null => "发薪日未设置", 0 => "今天发薪", int days => $"距离发薪日还有 {days} 天" };
        TextAt(g, $"已过的工作日按满勤计，本月满勤约 ¥{Money(s.FullMonth)}", 34, 450, 312, 10.5f, muted);
        TextAt(g, payday, 34, 467, 312, 10.5f, muted);
    }

    private bool Spinning => !Compact && Result?.State == "上班中";

    private static void DrawCycle(Graphics g)
    {
        var saved = g.Save();
        double seconds = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        g.TranslateTransform(105, 57);
        g.RotateTransform((float)(seconds % 3 * 120));
        var green = Color.FromArgb(44, 176, 86);
        using var pen = new Pen(green, 1.65f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(green);
        for (int i = 0; i < 2; i++)
        {
            g.DrawArc(pen, -7, -7, 14, 14, -147, 135);
            g.FillPolygon(brush, [new PointF(7.7f, 0.8f), new PointF(4.9f, -2.4f), new PointF(9.3f, -3.2f)]);
            g.RotateTransform(180);
        }
        g.Restore(saved);
    }

    private void DrawCompact(Graphics g)
    {
        var s = Result;
        string summary = s?.State ?? "请先设置";
        if (s?.Next is DateTime next)
        {
            int minutes = Math.Max(0, (int)Math.Ceiling((next - Now).TotalMinutes));
            summary = minutes >= 60 ? $"{minutes / 60} 小时 {minutes % 60} 分后{s.Event}" : $"{minutes} 分钟后{s.Event}";
        }
        Round(g, s?.State == "上班中" ? Color.FromArgb(38, 169, 90) : accent, 12, 18, 6, 6, 3);
        DrawAmount(g, 24, 7, 109, 21);
        TextAt(g, summary, 137, 13, 112, 10.5f, muted, right: true);
    }

    private float RollProgress => Math.Clamp((float)Stopwatch.GetElapsedTime(rollStarted).TotalSeconds / 0.24f, 0, 1);

    private void DrawAmount(Graphics g, float x, float y, float width, float size)
    {
        float fontSize = Math.Min(size, width / (currentAmount.Length * 0.62f));
        float cellWidth = fontSize * 0.62f;
        float cellHeight = size * 1.22f;
        float progress = RollProgress;
        float offset = 1 - (1 - progress) * (1 - progress);
        using var font = new Font("Consolas", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(accent);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
        };
        var saved = g.Save();
        g.SetClip(new RectangleF(x, y, width, cellHeight), CombineMode.Intersect);
        for (int i = 0; i < currentAmount.Length; i++)
        {
            char current = currentAmount[i];
            int oldIndex = i + previousAmount.Length - currentAmount.Length;
            char previous = oldIndex >= 0 ? previousAmount[oldIndex] : ' ';
            var cell = new RectangleF(x + i * cellWidth, y, cellWidth, cellHeight);
            if (progress < 1 && char.IsDigit(current) && previous != current)
            {
                var outgoing = cell;
                outgoing.Offset(0, -offset * cellHeight);
                g.DrawString(previous.ToString(), font, brush, outgoing, format);
                cell.Offset(0, (1 - offset) * cellHeight);
            }
            g.DrawString(current.ToString(), font, brush, cell, format);
        }
        g.Restore(saved);
    }

    internal static Region WindowRegion(Size size, float radius)
    {
        using var path = RoundedPath(0, 0, size.Width, size.Height, radius);
        return new Region(path);
    }

    private static string Money(decimal amount, string format = "N2") => amount.ToString(format, Chinese);

    private static Color Blend(Color a, Color b, float amount) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * amount), (int)(a.G + (b.G - a.G) * amount), (int)(a.B + (b.B - a.B) * amount));

    private static void TextAt(Graphics g, string text, float x, float y, float width, float size,
        Color color, bool bold = false, string family = "Microsoft YaHei UI", bool right = false)
    {
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = right ? StringAlignment.Far : StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip };
        using var initial = new Font(family, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        float measured = g.MeasureString(text, initial, int.MaxValue, format).Width;
        if (measured > width)
        {
            using var fitted = new Font(family, size * width / measured, initial.Style, GraphicsUnit.Pixel);
            g.DrawString(text, fitted, brush, new RectangleF(x, y, width, size * 2), format);
        }
        else g.DrawString(text, initial, brush, new RectangleF(x, y, width, size * 2), format);
    }

    private static void Round(Graphics g, Color color, float x, float y, float w, float h, float radius)
    {
        if (w <= 0 || h <= 0) return;
        using var path = RoundedPath(x, y, w, h, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    private static GraphicsPath RoundedPath(float x, float y, float w, float h, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(w, h));
        var path = new GraphicsPath();
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { rollTimer.Dispose(); tips.Dispose(); }
        base.Dispose(disposing);
    }

    private sealed class SymbolButton : Button
    {
        private string glyph = "";
        public SymbolButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string Glyph { get => glyph; set { if (glyph != value) { glyph = value; Invalidate(); } } }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            using var font = new Font("Segoe MDL2 Assets", Math.Max(6, Height * 0.56f), GraphicsUnit.Pixel);
            TextRenderer.DrawText(e.Graphics, Glyph, font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }

    private sealed class QuoteButton : Button
    {
        private Color accent, surface, muted;
        public QuoteButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; }
        public void UpdateContent(string text, Color tint, Color card, Color secondary)
        {
            if (Text == text && accent == tint && surface == card && muted == secondary) return;
            Text = text; accent = tint; surface = card; muted = secondary;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.ScaleTransform(Width / 336f, Height / 48f);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Round(g, surface, 0, 0, 336, 48, 13);
            TextAt(g, "❝", 12, 12, 23, 19, accent);
            TextAt(g, Text, 39, 16, 270, 11, ForeColor);
            TextAt(g, "↻", 313, 13, 17, 15, muted);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3, 330, 42));
        }
    }
}
