using System.Runtime.InteropServices;

namespace OffDutyCounter;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var instance = new Mutex(true, "Local\\QiYi92.OffDutyCounter", out bool first);
        if (!first)
        {
            MessageBox.Show("牛马计数器已在运行。可从任务栏右下角的通知区域打开。", "牛马计数器");
            return;
        }
        Application.Run(new CounterWindow());
    }
}

internal sealed class CounterWindow : ShadowForm
{
    private static readonly string[] Themes = ["默认", "跟随系统", "黑夜", "深海蓝", "松林绿", "薄荷青",
        "琥珀橙", "蔷薇红", "薰衣草紫", "雾岩灰", "可可棕"];
    private static readonly (int Background, int Accent)[] Palette = [
        (0xFFFFFF,0x006ACC), (0xFFFFFF,0x006ACC), (0x171B24,0x9AB9FF), (0xEFF6FF,0x1D568E),
        (0xEFF6EC,0x2D6246), (0xEBF9F4,0x006B60), (0xFFF4E2,0x984B09),
        (0xFFF0F3,0xA63457), (0xF5EFFF,0x6543A5), (0xEEF2F5,0x495768), (0xFAF1E9,0x78513D)];
    private static readonly (int Price, string Name, string Icon)[] Rewards = [
        (3,"一瓶可乐","🥤"), (10,"一杯瑞幸","☕"), (20,"一餐外卖","🍱"), (50,"一份KFC疯狂星期四","🍗"),
        (99,"一件T恤","👕"), (150,"一顿火锅","🍲"), (200,"一箱 92#汽油","⛽"),
        (300,"一张远门动车票","🚄"), (400,"一张特价机票","✈")];
    private static readonly string[] Quotes = [
        "早安，打工人！ (๑•̀ㅂ•́)و✧", "打工是不可能打工的。 ┐(￣ヘ￣)┌",
        "只有‘摸鱼’得来的工资，才算是意外之财。 (✧ω✧)", "打工人，打工魂，打工都是人上人！ ᕦ(ò_óˇ)ᕤ",
        "认认真真上班不叫赚钱，摸鱼才是。 (￣▽￣)ノ", "老板假装给我们发工资，我们假装给老板干活。 (¬‿¬)",
        "带薪摸鱼，快乐工作。 ヽ(・∀・)ﾉ"];

    private Preferences preferences = Preferences.Load();
    private readonly NotifyIcon tray;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private readonly Dashboard dashboard = new() { Dock = DockStyle.Fill };
    private DateTime nextQuote = DateTime.Now.AddMinutes(5);
    private int quoteIndex;
    private bool exiting;
    private int appliedPalette = -1;
    private Color themeAccent;

    public CounterWindow()
    {
        Text = "牛马计数器";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Information;
        Font = new Font("Microsoft YaHei UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        TopMost = preferences.Topmost;
        Controls.Add(dashboard);
        Point? compactDragStart = null;
        dashboard.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (preferences.Compact) compactDragStart = e.Location;
            else if (dashboard.IsTitleArea(e.Y)) DragWindow();
        };
        dashboard.MouseUp += (_, _) => compactDragStart = null;
        dashboard.MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || compactDragStart is not Point start) return;
            var area = new Rectangle(start.X - SystemInformation.DragSize.Width / 2,
                start.Y - SystemInformation.DragSize.Height / 2,
                SystemInformation.DragSize.Width, SystemInformation.DragSize.Height);
            if (area.Contains(e.Location)) return;
            compactDragStart = null;
            DragWindow();
        };
        dashboard.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && preferences.Compact) ToggleCompact();
        };
        dashboard.Configure(
            () => { preferences = preferences with { Topmost = !TopMost }; TopMost = preferences.Topmost; preferences.Save(); RefreshDisplay(); },
            EditSettings, () => ShowThemes(),
            ToggleCompact,
            HideToTray, AdvanceQuote);
        for (int i = 0; i < Themes.Length; i++)
        {
            int index = i;
            themeMenu.Items.Add(Themes[i], null, (_, _) =>
            {
                preferences = preferences with { Theme = index };
                preferences.Save();
                ApplyTheme();
            });
        }
        themeMenu.Opening += (_, _) =>
        {
            for (int i = 0; i < Themes.Length; i++)
                ((ToolStripMenuItem)themeMenu.Items[i]).Checked = i == preferences.Theme;
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("打开牛马计数器", null, (_, _) => RestoreWindow());
        menu.Items.Add("设置", null, (_, _) => { RestoreWindow(); EditSettings(); });
        menu.Items.Add("退出", null, (_, _) => { exiting = true; Close(); });
        tray = new NotifyIcon { Icon = Icon, Text = "牛马计数器", ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) RestoreWindow(); };
        FormClosing += (_, e) =>
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; HideToTray(); }
        };
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized) HideToTray(); };
        Shown += (_, _) =>
        {
            ApplyLayout();
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
            if (preferences.Schedule?.Valid != true) EditSettings();
            else QueueSettings();
        };
        DpiChanged += (_, _) => BeginInvoke(ApplyLayout);
        timer.Tick += (_, _) => RefreshDisplay();
        ApplyLayout();
        ApplyTheme();
        RefreshDisplay();
        timer.Start();
    }

    private readonly ContextMenuStrip themeMenu = new();

    private void ShowThemes() => themeMenu.Show(dashboard.ThemeButton, new Point(0, dashboard.ThemeButton.Height));

    private void ToggleCompact()
    {
        preferences = preferences with { Compact = !preferences.Compact };
        preferences.Save();
        ApplyLayout();
        RefreshDisplay();
    }

    private void DragWindow() => BeginWindowDrag(Handle);

    internal static void BeginWindowDrag(nint handle)
    {
        ReleaseCapture();
        SendMessage(handle, 0x00A1, 2, 0); // WM_NCLBUTTONDOWN, HTCAPTION
    }

    private void ApplyLayout()
    {
        dashboard.Compact = preferences.Compact;
        var area = Screen.FromControl(this).WorkingArea;
        double dpi = DeviceDpi / 96.0;
        ClientSize = new Size(Math.Min((int)((preferences.Compact ? 260 : 380) * dpi), area.Width - 20),
            Math.Min((int)((preferences.Compact ? 42 : 572) * dpi), area.Height - 55));
        if (Visible)
            Location = new Point(Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width)),
                Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        var previousRegion = Region;
        Region = Dashboard.WindowRegion(ClientSize, (float)(12 * dpi));
        previousRegion?.Dispose();
        dashboard.PerformLayout();
        dashboard.Invalidate();
    }

    private void ApplyTheme()
    {
        int selected = preferences.Theme;
        if (selected < 0 || selected >= Palette.Length) selected = 0;
        if (selected == 1 && Microsoft.Win32.Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1) is 0) selected = 2;
        if (appliedPalette == selected) return;
        appliedPalette = selected;
        var (background, accent) = Palette[selected];
        BackColor = Color.FromArgb(255, Color.FromArgb(background));
        ForeColor = selected == 2 ? Color.WhiteSmoke : Color.FromArgb(33, 39, 48);
        var tint = Color.FromArgb(255, Color.FromArgb(accent));
        themeAccent = tint;
        dashboard.SetColors(BackColor, ForeColor, tint, selected == 2);
        preparedSettings?.Dispose();
        preparedSettings = null;
        if (Visible) QueueSettings();
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    private void HideToTray() => Hide();

    private void RestoreWindow()
    {
        WindowState = FormWindowState.Normal;
        Show();
        ApplyLayout();
        RefreshDisplay();
        Activate();
    }

    private SettingsDialog? preparedSettings;
    private bool settingsOpen;
    private bool settingsQueued;

    private SettingsDialog CreateSettings() => new(
        preferences.Schedule?.Valid == true ? preferences.Schedule : Schedule.Default,
        BackColor, ForeColor, themeAccent);

    private void QueueSettings()
    {
        if (!IsHandleCreated || settingsQueued || settingsOpen || preparedSettings is not null) return;
        settingsQueued = true;
        BeginInvoke(() =>
        {
            settingsQueued = false;
            if (!IsDisposed && !settingsOpen && preparedSettings is null)
                preparedSettings = CreateSettings();
        });
    }

    private void EditSettings()
    {
        if (settingsOpen) return;
        settingsOpen = true;
        using var dialog = preparedSettings ?? CreateSettings();
        preparedSettings = null;
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Result is { } schedule)
        {
            preferences = preferences with { Schedule = schedule };
            preferences.Save();
            RefreshDisplay();
        }
        settingsOpen = false;
        QueueSettings();
    }

    private void AdvanceQuote()
    {
        quoteIndex = (quoteIndex + 1) % Quotes.Length;
        nextQuote = DateTime.Now.AddMinutes(5);
        dashboard.Quote = Quotes[quoteIndex];
        dashboard.Invalidate();
    }

    private void RefreshDisplay()
    {
        if (!Visible) return;
        ApplyTheme();
        var now = DateTime.Now;
        if (now >= nextQuote) AdvanceQuote();
        dashboard.Quote = Quotes[quoteIndex];
        dashboard.Now = now;
        dashboard.Pinned = TopMost;
        dashboard.SetResult(preferences.Schedule is { Valid: true } schedule ? schedule.At(now) : null);
        var reward = Rewards.LastOrDefault(item => (dashboard.Result?.Amount ?? 0) >= item.Price);
        dashboard.RewardName = reward.Name;
        dashboard.RewardIcon = reward.Icon;
        dashboard.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); tray.Dispose(); themeMenu.Dispose(); preparedSettings?.Dispose(); }
        base.Dispose(disposing);
    }
}
