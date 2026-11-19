using System.Drawing.Drawing2D;

namespace OffDutyCounter;

// Keep the native text editor for selection, keyboard shortcuts and assistive technology.
internal sealed class SettingsInput : Panel
{
    private readonly TextBoxBase editor;
    private readonly int textWidth;
    private readonly Color border;
    private readonly Color accent;
    private readonly Button? up, down;
    private int StepperWidth => up is null ? 0 : Math.Max(18, Font.Height + 2);

    internal SettingsInput(TextBoxBase editor, Color surface, Color border, Color accent, Action<int>? step = null)
    {
        this.editor = editor;
        textWidth = editor.Width;
        this.border = border;
        this.accent = accent;
        BackColor = surface;
        DoubleBuffered = true;
        TabStop = false;
        editor.BorderStyle = BorderStyle.None;
        editor.BackColor = surface;
        editor.Enter += (_, _) => Invalidate();
        editor.Leave += (_, _) => Invalidate();
        if (step is not null)
        {
            up = new StepArrow(true, () => step(30), editor.AccessibleName, surface, accent);
            down = new StepArrow(false, () => step(-30), editor.AccessibleName, surface, accent);
        }
        Controls.Add(editor);
        if (up is not null && down is not null) Controls.AddRange([up, down]);
        Size = GetPreferredSize(Size.Empty);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        int unit = Math.Max(1, Font.Height / 8);
        return new Size(Math.Max(textWidth, TextRenderer.MeasureText(editor.Text, editor.Font).Width) + unit * 8 + StepperWidth,
            editor.PreferredHeight + unit * 6);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int inset = Math.Max(6, Font.Height / 2);
        editor.SetBounds(inset, (Height - editor.PreferredHeight) / 2,
            Math.Max(1, Width - inset * 2 - StepperWidth), editor.PreferredHeight);
        if (up is not null && down is not null)
        {
            int x = Width - StepperWidth - 3;
            int half = Math.Max(1, (Height - 6) / 2);
            up.SetBounds(x, 3, StepperWidth, half);
            down.SetBounds(x, 3 + half, StepperWidth, half);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        SettingsChrome.DrawOutline(e.Graphics, ClientRectangle, BackColor, ContainsFocus ? accent : border);
        if (up is not null)
        {
            using var divider = new Pen(border);
            e.Graphics.DrawLine(divider, up.Left - 1, 6, up.Left - 1, Height - 6);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        editor.Focus();
        base.OnClick(e);
    }
    private sealed class StepArrow : Button
    {
        private readonly bool upward;
        private readonly Color accent;
        private bool hovered;

        internal StepArrow(bool upward, Action click, string? field, Color surface, Color tint)
        {
            this.upward = upward;
            accent = tint;
            BackColor = surface;
            ForeColor = SystemColors.GrayText;
            AccessibleName = $"{field}，{(upward ? "增加" : "减少")} 30 分钟";
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Click += (_, _) => click();
            MouseEnter += (_, _) => { hovered = true; Invalidate(); };
            MouseLeave += (_, _) => { hovered = false; Invalidate(); };
            Enter += (_, _) => Parent?.Invalidate();
            Leave += (_, _) => Parent?.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            bool active = hovered || Focused;
            e.Graphics.Clear(active ? SettingsChrome.Blend(BackColor, accent, 0.10f) : BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float x = Width / 2f, y = Height / 2f;
            float size = Math.Max(2.5f, DeviceDpi / 96f * 2.7f);
            float direction = upward ? -1 : 1;
            using var pen = new Pen(active ? accent : SettingsChrome.Blend(BackColor, accent, 0.78f), Math.Max(1, DeviceDpi / 96f))
                { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            e.Graphics.DrawLines(pen,
                [new PointF(x - size, y - direction * size / 2), new PointF(x, y + direction * size / 2),
                 new PointF(x + size, y - direction * size / 2)]);
        }
    }

}

internal sealed class SettingsChoice : Button
{
    private readonly ToolStripDropDown menu;
    private readonly ListBox list;
    private readonly Color accent;
    private readonly Color border;
    private readonly Color subdued;
    private readonly string[] items;
    internal int SelectedIndex { get; private set; }
    internal event EventHandler? SelectionChanged;

    internal SettingsChoice(IEnumerable<string> options, int selected, Color surface, Color ink,
        Color border, Color accent, Color subdued)
    {
        this.accent = accent;
        this.border = border;
        this.subdued = subdued;
        BackColor = surface;
        ForeColor = ink;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        items = options.ToArray();
        list = new ListBox { BackColor = surface, ForeColor = ink, BorderStyle = BorderStyle.None,
            IntegralHeight = false, ScrollAlwaysVisible = items.Length > 7 };
        list.Items.AddRange(items);
        menu = new ToolStripDropDown { Padding = new Padding(4), BackColor = surface };
        menu.Items.Add(new ToolStripControlHost(list) { AutoSize = false, Margin = Padding.Empty });
        list.MouseClick += (_, e) =>
        {
            int index = list.IndexFromPoint(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) Commit(index);
        };
        list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && list.SelectedIndex >= 0) Commit(list.SelectedIndex);
            else if (e.KeyCode == Keys.Escape) menu.Close();
            else return;
            e.Handled = e.SuppressKeyPress = true;
        };
        SelectOption(selected);
        Size = GetPreferredSize(Size.Empty);
    }

    private void Commit(int index)
    {
        SelectOption(index);
        menu.Close();
        Focus();
    }

    internal void SelectOption(int index)
    {
        bool changed = SelectedIndex != index;
        SelectedIndex = index;
        Text = items[index];
        AccessibleDescription = Text;
        if (changed) SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (items is not { Length: > 0 }) return base.GetPreferredSize(proposedSize);
        int widest = items.Max(item => TextRenderer.MeasureText(item, Font).Width);
        return new Size(widest + Font.Height * 2, Font.Height + Math.Max(12, Font.Height * 3 / 4));
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        list.Font = Font;
        menu.Items[0].Size = new Size(Width, list.ItemHeight * Math.Min(7, items.Length));
        list.SelectedIndex = SelectedIndex;
        menu.Show(this, new Point(0, Height + 2));
        list.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && e.Alt)
        {
            OnClick(EventArgs.Empty);
            e.Handled = e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        SettingsChrome.DrawOutline(e.Graphics, ClientRectangle, BackColor, Focused ? accent : border);
        int inset = Math.Max(7, Font.Height / 2);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(inset, 0, Width - inset * 3, Height),
            ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        int x = Width - inset - 4, y = Height / 2;
        using var pen = new Pen(subdued, Math.Max(1f, DeviceDpi / 96f)) { LineJoin = LineJoin.Round };
        e.Graphics.DrawLines(pen, [new Point(x - 3, y - 1), new Point(x, y + 2), new Point(x + 3, y - 1)]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) menu?.Dispose();
        base.Dispose(disposing);
    }


}

internal static class SettingsChrome
{
    internal static void DrawOutline(Graphics graphics, Rectangle bounds, Color surface, Color border)
    {
        float width = bounds.Width - 1, height = bounds.Height - 1;
        if (width <= 1 || height <= 1) return;
        float d = Math.Min(width, height) * 0.4f;
        using var path = new GraphicsPath();
        path.AddArc(0.5f, 0.5f, d, d, 180, 90);
        path.AddArc(width - d, 0.5f, d, d, 270, 90);
        path.AddArc(width - d, height - d, d, d, 0, 90);
        path.AddArc(0.5f, height - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(surface);
        graphics.FillPath(brush, path);
        using var pen = new Pen(border);
        graphics.DrawPath(pen, path);
    }

    internal static Color Blend(Color a, Color b, float weight) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * weight), (int)(a.G + (b.G - a.G) * weight), (int)(a.B + (b.B - a.B) * weight));
}
