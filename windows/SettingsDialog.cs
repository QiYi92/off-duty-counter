using System.Globalization;

namespace OffDutyCounter;

internal sealed class SettingsDialog : ShadowForm
{
    private readonly Color accent;
    private readonly Color surface;
    private readonly Color secondary;
    private readonly Color border;
    private readonly List<Label> fieldLabels = [];
    private readonly List<Control> fields = [];
    private readonly TableLayoutPanel page;
    public Schedule? Result { get; private set; }

    public SettingsDialog(Schedule schedule, Color background, Color foreground, Color accent)
    {
        this.accent = accent;
        surface = Blend(background, foreground, 0.035f);
        border = Blend(background, foreground, 0.16f);
        secondary = Blend(foreground, background, 0.30f);
        Text = "牛马计数器 · 设置";
        Font = new Font("Microsoft YaHei UI", 9.5f);
        BackColor = background;
        ForeColor = foreground;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(390, 420);
        FormBorderStyle = FormBorderStyle.None;
        SizeChanged += (_, _) =>
        {
            if (ClientSize.Width > 0 && ClientSize.Height > 0)
                RoundControl(this, 12);
        };
        RoundControl(this, 12);
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        page = new TableLayoutPanel
        {
            Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(17, 15, 17, 5)
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.Controls.Add(page);
        Controls.Add(body);

        var heading = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var title = LabelFor("计薪设置", 15, bold: true);
        title.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        title.MouseDown += DragHeader;
        heading.MouseDown += DragHeader;
        heading.Controls.Add(title, 0, 0);
        var close = ActionButton("×", BackColor, secondary);
        close.AccessibleName = "关闭设置";
        close.DialogResult = DialogResult.Cancel;
        heading.Controls.Add(close, 1, 0);
        Add(page, heading, margin: new Padding(0, 0, 0, 3));
        Add(page, LabelFor("按你的排班估算收入，设置仅保存在本机。", color: secondary),
            margin: new Padding(0, 0, 0, 10));

        var pay = Section("薪资与发薪");
        var salary = new TextBox { Width = 166, Text = schedule.Salary.ToString("N2", CultureInfo.CurrentCulture) };
        salary.AccessibleName = "月薪（元）";
        Row(pay, "月薪（元）", EditField(salary));
        var fixedDays = DayChoice("按当月排班自动计算", schedule.FixedDays, day => $"{day} 天");
        Row(pay, "每月计薪天数", fixedDays);
        var payday = DayChoice("未设置", schedule.Payday, day => $"每月 {day} 日");
        Row(pay, "每月发薪日", payday);

        var restCard = Section("休息安排");
        var counts = SelectField(["单休", "双休"], schedule.Rest.Length - 1);
        Row(restCard, "每周休息", counts);

        string[] weekdays = ["周一", "周二", "周三", "周四", "周五", "周六", "周日"];
        DayOfWeek[] days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
        var rest = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
        var checks = days.Select((d, i) => new CheckBox
        {
            Text = weekdays[i][1..], AccessibleName = weekdays[i], Checked = schedule.Rest.Contains(d),
            Appearance = Appearance.Button,
            FlatStyle = FlatStyle.Flat, AutoSize = true, Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(0, 0, 4, 4), Cursor = Cursors.Hand
        }).ToArray();
        foreach (var check in checks)
        {
            check.FlatAppearance.BorderSize = 0;
            check.SizeChanged += (_, _) => RoundControl(check, 6);
            RoundControl(check, 6);
            check.FlatAppearance.CheckedBackColor = Blend(surface, accent, 0.16f);
            void UpdateColor() => check.ForeColor = check.Checked ? this.accent : secondary;
            check.CheckedChanged += (_, _) => UpdateColor();
            UpdateColor();
        }
        bool updatingRest = false;
        void LimitRest()
        {
            if (updatingRest) return;
            updatingRest = true;
            int target = counts.SelectedIndex + 1;
            foreach (var check in checks.Where(check => check.Checked).Skip(target))
                check.Checked = false;
            int selected = checks.Count(check => check.Checked);
            foreach (var check in checks)
                check.Enabled = check.Checked || selected < target;
            updatingRest = false;
        }
        foreach (var check in checks) check.CheckedChanged += (_, _) => LimitRest();
        counts.SelectionChanged += (_, _) => LimitRest();
        LimitRest();
        rest.Controls.AddRange(checks);
        Add(restCard, rest, span: 2, margin: new Padding(0, 4, 0, 0));

        var work = Section("工作时段");
        var times = schedule.Times.Select(minute => new MaskedTextBox("00:00")
        {
            Width = 80, Text = $"{minute / 60:00}:{minute % 60:00}", Culture = CultureInfo.InvariantCulture,
            TextAlign = HorizontalAlignment.Center, HidePromptOnLeave = true
        }).ToArray();

        Row(work, "上午", TimeRange(times[0], times[1], "上午上班", "上午下班"));
        Row(work, "下午", TimeRange(times[2], times[3], "下午上班", "下午下班"));
        var plannedHours = LabelFor("", color: secondary);
        Add(work, plannedHours, span: 2, margin: new Padding(0, 4, 0, 0));
        void UpdatePlannedHours()
        {
            var minutes = times.Select(time => TimeOnly.TryParseExact(time.Text, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                    ? value.Hour * 60 + value.Minute : -1).ToArray();
            if (minutes.Any(value => value < 0) || minutes[0] >= minutes[1]
                || minutes[1] > minutes[2] || minutes[2] >= minutes[3])
            {
                plannedHours.Text = "每日计薪工时：请确认工作时段";
                return;
            }
            int total = minutes[1] - minutes[0] + minutes[3] - minutes[2];
            string hours = total % 60 == 0 ? $"{total / 60} 小时" : $"{total / 60} 小时 {total % 60} 分钟";
            plannedHours.Text = $"每日计薪工时：{hours}（不含午休）";
        }
        foreach (var time in times) time.TextChanged += (_, _) => UpdatePlannedHours();
        UpdatePlannedHours();

        var error = LabelFor("", color: ForeColor);
        error.Visible = false;
        Add(page, error);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, AutoSize = true, Padding = new Padding(16, 7, 16, 12)
        };
        var save = ActionButton("保存设置", accent, accent.GetBrightness() > 0.65f ? Color.FromArgb(22, 27, 37) : Color.White);
        var cancel = ActionButton("取消", surface, ForeColor);
        cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.AddRange([save, cancel]);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = actions.PreferredSize.Height };
        var reset = ActionButton("重置设置", surface, ForeColor);
        reset.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        reset.Location = new Point(17, 9);
        footer.Controls.Add(reset);
        footer.Controls.Add(actions);
        var about = ActionButton("关于", surface, ForeColor);
        about.Click += (_, _) => ShowAbout();
        footer.Controls.Add(about);
        about.BringToFront();
        reset.BringToFront();
        Controls.Add(footer);
        body.BringToFront();
        reset.Click += (_, _) =>
        {
            var defaults = Schedule.Default;
            salary.Text = defaults.Salary.ToString("N2", CultureInfo.CurrentCulture);
            fixedDays.SelectOption(defaults.FixedDays);
            payday.SelectOption(defaults.Payday);
            updatingRest = true;
            counts.SelectOption(defaults.Rest.Length - 1);
            for (int i = 0; i < checks.Length; i++)
                checks[i].Checked = defaults.Rest.Contains(days[i]);
            updatingRest = false;
            LimitRest();
            for (int i = 0; i < times.Length; i++) SetTime(times[i], defaults.Times[i]);
            error.Visible = false;
        };
        AcceptButton = save;
        CancelButton = cancel;
        save.Click += (_, _) =>
        {
            var selected = days.Where((_, i) => checks[i].Checked).ToArray();
            if (!decimal.TryParse(salary.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal monthly)
                || monthly <= 0 || decimal.Round(monthly, 2) != monthly)
            {
                ShowError("请输入大于零的月薪，最多保留两位小数。");
                salary.Focus();
                return;
            }
            int[] minutes = new int[times.Length];
            for (int i = 0; i < times.Length; i++)
            {
                if (!TimeOnly.TryParseExact(times[i].Text, "HH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var time))
                {
                    ShowError("请输入有效的 24 小时时间，例如 09:00。");
                    times[i].Focus();
                    return;
                }
                minutes[i] = time.Hour * 60 + time.Minute;
            }
            var result = new Schedule(monthly, selected, minutes, fixedDays.SelectedIndex, payday.SelectedIndex);
            if (!result.Valid || selected.Length != counts.SelectedIndex + 1)
            {
                ShowError(selected.Length != counts.SelectedIndex + 1
                    ? $"请选中 {counts.SelectedIndex + 1} 个休息日。"
                    : "请确认两个工作时段顺序正确且不重叠。");
                return;
            }
            Result = result;
            DialogResult = DialogResult.OK;
        };

        void ShowError(string message)
        {
            error.Text = message;
            error.Visible = true;
            body.ScrollControlIntoView(error);
        }

        // Finish DPI-dependent sizing before WinForms shows the first frame.
        Load += (_, _) =>
        {
            SuspendLayout();
            page.SuspendLayout();
            var area = Screen.FromControl(Owner ?? this).WorkingArea;
            float scale = DeviceDpi / 96f;
            int width = Math.Min((int)(390 * scale), area.Width - 20);
            ClientSize = new Size(width, Math.Min((int)(430 * scale), area.Height - 20));
            int labelWidth = fieldLabels.Max(label => TextRenderer.MeasureText(label.Text, label.Font).Width);
            foreach (var label in fieldLabels) label.MinimumSize = new Size(labelWidth, 0);
            foreach (var field in fields) field.Size = field.GetPreferredSize(Size.Empty);
            page.ResumeLayout(performLayout: true);
            ResumeLayout(performLayout: true);
            footer.Height = actions.PreferredSize.Height;
            reset.Location = new Point((int)(17 * scale), Math.Max(0, (footer.Height - reset.PreferredSize.Height) / 2));
            about.Location = new Point(reset.Right + (int)(6 * scale), reset.Top);
            int height = page.PreferredSize.Height + footer.Height + (int)(7 * scale);
            ClientSize = new Size(width, Math.Min(height, area.Height - 20));
            RoundControl(this, 12);
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        };
    }

    private void ShowAbout()
    {
        using var about = new ShadowForm
        {
            Text = "关于牛马计数器", Font = Font, BackColor = BackColor, ForeColor = ForeColor,
            FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.CenterParent,
            ShowInTaskbar = false, AutoScaleMode = AutoScaleMode.Dpi
        };
        var details = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill
        };
        var title = LabelFor("关于牛马计数器", 14, bold: true);
        var developer = LabelFor("开发者：二进制的伽利略");
        var version = LabelFor("版本：V1.1", color: secondary);
        var close = ActionButton("关闭", surface, ForeColor);
        close.DialogResult = DialogResult.OK;
        details.Controls.AddRange([title, developer, version, close]);
        about.Controls.Add(details);
        about.AcceptButton = close;
        about.CancelButton = close;
        about.Load += (_, _) =>
        {
            int unit = Math.Max(1, about.DeviceDpi / 96);
            details.Padding = new Padding(16 * unit, 12 * unit, 16 * unit, 12 * unit);
            close.Margin = new Padding(3, 10 * unit, 3, 0);
            about.ClientSize = details.GetPreferredSize(Size.Empty);
            RoundControl(about, 12);
            about.Location = new Point(Left + (Width - about.Width) / 2, Top + (Height - about.Height) / 2);
        };
        about.ShowDialog(this);
    }

    private TableLayoutPanel Section(string title)
    {
        var section = new TableLayoutPanel
        {
            ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = surface, Padding = new Padding(12, 9, 12, 8)
        };
        section.SizeChanged += (_, _) => RoundControl(section, 9);
        section.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add(section, LabelFor(title, 11, bold: true), span: 2, margin: new Padding(0, 0, 0, 6));
        Add(page, section, margin: new Padding(0, 0, 0, 8));
        return section;
    }

    private Label LabelFor(string text, float size = 9.5f, bool bold = false, Color? color = null) => new()
    {
        Text = text, AutoSize = true, Anchor = AnchorStyles.Left,
        Font = new Font(Font.FontFamily, size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? ForeColor
    };

    private static void Add(TableLayoutPanel layout, Control control, int span = 1, Padding? margin = null)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.Margin = margin ?? Padding.Empty;
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(control, 0, row);
        if (span > 1) layout.SetColumnSpan(control, span);
    }

    private void Row(TableLayoutPanel layout, string text, Control input)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = LabelFor(text);
        label.Margin = new Padding(0, 4, 16, 4);
        fieldLabels.Add(label);
        input.Margin = new Padding(0, 2, 0, 2);
        input.Anchor = AnchorStyles.Left;
        if (string.IsNullOrEmpty(input.AccessibleName)) input.AccessibleName = text;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(input, 1, row);
    }

    private FlowLayoutPanel TimeRange(TextBoxBase start, TextBoxBase end, string startName, string endName)
    {
        start.AccessibleName = startName;
        end.AccessibleName = endName;
        var range = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
        var separator = LabelFor("至", color: secondary);
        separator.Margin = new Padding(3, 6, 3, 0);
        range.Controls.AddRange([TimeField(start), separator, TimeField(end)]);
        return range;
    }

    private void DragHeader(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) CounterWindow.BeginWindowDrag(Handle);
    }

    private SettingsInput TimeField(TextBoxBase editor) => EditField(editor, delta =>
    {
        if (TimeOnly.TryParseExact(editor.Text, "HH:mm", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out TimeOnly time))
            SetTime(editor, Math.Clamp(time.Hour * 60 + time.Minute + delta, 0, 1439));
        else editor.Focus();
    });

    private static void SetTime(TextBoxBase editor, int minutes)
        => editor.Text = $"{minutes / 60:00}:{minutes % 60:00}";

    private SettingsInput EditField(TextBoxBase editor, Action<int>? step = null)
    {
        editor.ForeColor = ForeColor;
        var field = new SettingsInput(editor, BackColor, border, accent, step);
        fields.Add(field);
        return field;
    }

    private SettingsChoice SelectField(IEnumerable<string> options, int selection)
    {
        var field = new SettingsChoice(options, selection, BackColor, ForeColor, border, accent, secondary);
        fields.Add(field);
        return field;
    }

    private SettingsChoice DayChoice(string empty, int selected, Func<int, string> text)
        => SelectField(Enumerable.Range(1, 31).Select(text).Prepend(empty), selected);

    private static Button ActionButton(string text, Color background, Color foreground)
    {
        var button = new Button { Text = text, BackColor = background, ForeColor = foreground, FlatStyle = FlatStyle.Flat,
            AutoSize = true, Padding = new Padding(10, 3, 10, 3), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        button.SizeChanged += (_, _) => RoundControl(button, 7);
        RoundControl(button, 7);
        return button;
    }

    private static void RoundControl(Control control, float radius)
    {
        if (control.Width <= 0 || control.Height <= 0) return;
        var previous = control.Region;
        control.Region = Dashboard.WindowRegion(control.Size, radius * control.DeviceDpi / 96f);
        previous?.Dispose();
    }

    private static Color Blend(Color a, Color b, float weight) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * weight), (int)(a.G + (b.G - a.G) * weight), (int)(a.B + (b.B - a.B) * weight));
}
