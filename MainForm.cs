using System.Diagnostics;
using System.Drawing.Drawing2D;
using MiniWebCounter.Models;
using MiniWebCounter.Services;

namespace MiniWebCounter;

public sealed class MainForm : Form
{
    private const int BaseCardWidth = 180;
    private const int BaseCardHeight = 96;
    private const int BaseCardGap = 8;
    private const int BasePadding = 8;

    private readonly SettingsService _settingsService = new();
    private readonly StatusPollingService _pollingService = new();
    private readonly AudioPlaybackService _audioPlaybackService = new();
    private readonly System.Windows.Forms.Timer _pollingTimer = new();
    private readonly System.Windows.Forms.Timer _flashTimer = new() { Interval = 150 };
    private readonly FlowLayoutPanel _cardsPanel = new();
    private readonly NotifyIcon _notifyIcon = new();
    private readonly ToolStripMenuItem _trayTitleItem = new();
    private readonly ToolStripMenuItem _displayItemsMenu = new("표시 항목");
    private readonly Dictionary<string, StatusCard> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _lastValues = new(StringComparer.Ordinal);
    private readonly HashSet<string> _mutedItems = new(StringComparer.Ordinal);
    private readonly Icon _appIcon = AppIconFactory.Create();

    private AppSettings _settings;
    private StatusData _statusData = new();
    private bool _isPolling;
    private bool _exitRequested;
    private bool _flashPhase;

    public MainForm()
    {
        _settings = _settingsService.Load();

        Text = GetProgramName();
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        TopMost = true;
        ShowInTaskbar = true;
        ClientSize = new Size(196, 112);
        Font = new Font("Segoe UI", 9F);
        Icon = _appIcon;

        BuildUi();
        BuildTrayMenu();
        ApplySettingsToUi();
        ConfigurePolling();

        _pollingTimer.Tick += PollingTimer_Tick;
        _flashTimer.Tick += FlashTimer_Tick;
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
    }

    private void BuildUi()
    {
        _cardsPanel.Dock = DockStyle.Fill;
        _cardsPanel.WrapContents = false;
        _cardsPanel.AutoScroll = false;
        Controls.Add(_cardsPanel);
    }

    private void BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        _trayTitleItem.Enabled = false;
        menu.Items.Add(_trayTitleItem);
        menu.Items.Add(new ToolStripSeparator());

        var showItem = new ToolStripMenuItem("화면 표시");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var settingsItem = new ToolStripMenuItem("설정");
        settingsItem.Click += async (_, _) => await OpenSettingsAsync();
        menu.Items.Add(settingsItem);

        menu.Items.Add(_displayItemsMenu);
        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        _notifyIcon.Icon = _appIcon;
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        ApplyProgramName();
    }

    private async Task OpenSettingsAsync()
    {
        var wasTopMost = TopMost;
        TopMost = false;

        try
        {
            using var dialog = new SettingsForm(_settings)
            {
                Icon = _appIcon
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _settings = dialog.ResultSettings;
            _settingsService.Save(_settings);
            ApplySettingsToUi();
            ConfigurePolling();
            await RefreshStatusAsync();
        }
        finally
        {
            TopMost = wasTopMost;
        }
    }

    private void SetStatusVisibility(string valueName, bool visible)
    {
        var item = _settings.Items.FirstOrDefault(candidate =>
            string.Equals(candidate.ValueName, valueName, StringComparison.Ordinal));

        if (item is null)
        {
            return;
        }

        item.Visible = visible;
        _settingsService.Save(_settings);
        ApplySettingsToUi();
    }

    private void ApplySettingsToUi()
    {
        _settings.Items ??= [];
        _settings.ScalePercent = Math.Clamp(_settings.ScalePercent, 50, 200);

        var programBackground = ParseColor(_settings.ProgramBackgroundColor, Color.White);
        BackColor = programBackground;
        _cardsPanel.BackColor = programBackground;

        ApplyProgramName();
        RebuildCards();
        RebuildTrayItems();
        UpdateValues();
        UpdateWindowSize();
    }

    private void ApplyProgramName()
    {
        var programName = GetProgramName();
        _settings.ProgramName = programName;
        Text = programName;
        _trayTitleItem.Text = programName;
        _notifyIcon.Text = GetNotifyIconText(programName);
    }

    private void RebuildCards()
    {
        var scale = GetScale();
        var gap = Scale(BaseCardGap, scale);
        var padding = Scale(BasePadding, scale);
        var horizontal = IsHorizontalLayout();

        _flashTimer.Stop();
        _flashPhase = false;

        _cardsPanel.SuspendLayout();
        _cardsPanel.Controls.Clear();
        _cards.Clear();

        _cardsPanel.FlowDirection = horizontal ? FlowDirection.LeftToRight : FlowDirection.TopDown;
        _cardsPanel.Padding = new Padding(padding);

        foreach (var item in _settings.Items.Where(item => item.Visible))
        {
            var card = CreateCard(item, scale, horizontal, gap);
            _cards[item.ValueName] = card;
            _cardsPanel.Controls.Add(card.Container);
        }

        _cardsPanel.ResumeLayout();
    }

    private StatusCard CreateCard(MonitoringItem item, float scale, bool horizontal, int gap)
    {
        var cardWidth = Scale(BaseCardWidth, scale);
        var cardHeight = Scale(BaseCardHeight, scale);
        var sidePadding = Scale(8, scale);
        var iconSize = Scale(22, scale);
        var iconGap = Scale(2, scale);
        var baseBackgroundColor = ParseColor(item.BackgroundColor, Color.DimGray);
        var textColor = ParseColor(item.TextColor, Color.White);
        var hasLink = !string.IsNullOrWhiteSpace(item.LinkUrl);
var hasSound = !string.IsNullOrWhiteSpace(item.SoundFile);
var iconCount = (hasLink ? 1 : 0) + (hasSound ? 1 : 0);
        var iconAreaWidth = iconCount * iconSize + Math.Max(0, iconCount - 1) * iconGap;

        var panel = new Panel
        {
            Size = new Size(cardWidth, cardHeight),
            Margin = horizontal
                ? new Padding(0, 0, gap, 0)
                : new Padding(0, 0, 0, gap),
            BackColor = baseBackgroundColor
        };

        var titleLabel = new Label
        {
            Text = GetDisplayName(item),
            ForeColor = textColor,
            Font = new Font("Segoe UI", 12.5F * scale, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Size = new Size(Math.Max(20, cardWidth - sidePadding * 2 - iconAreaWidth - Scale(4, scale)), Scale(28, scale)),
            Location = new Point(sidePadding, Scale(3, scale))
        };

        var iconX = cardWidth - sidePadding - iconSize;
if (hasLink)
{
    var linkIcon = CreateIconPanel(iconX, Scale(4, scale), iconSize);
    linkIcon.Cursor = Cursors.Hand;
    linkIcon.Paint += (_, e) => DrawLinkIcon(e.Graphics, linkIcon.ClientRectangle, textColor, scale);
    linkIcon.Click += (_, _) => OpenItemLink(item.LinkUrl);
    panel.Controls.Add(linkIcon);
}

Panel? soundIcon = null;
if (hasSound)
{
    if (hasLink)
    {
        iconX -= iconSize + iconGap;
    }
            soundIcon = CreateIconPanel(iconX, Scale(4, scale), iconSize);
            soundIcon.Cursor = Cursors.Hand;
            soundIcon.Paint += (_, e) => DrawSoundIcon(
                e.Graphics,
                soundIcon.ClientRectangle,
                textColor,
                _mutedItems.Contains(item.ValueName),
                scale);
            soundIcon.Click += (_, _) =>
            {
                if (!_mutedItems.Add(item.ValueName))
                {
                    _mutedItems.Remove(item.ValueName);
                }
                else
                {
                    _audioPlaybackService.Stop(item.ValueName);
                }

                soundIcon.Invalidate();
            };
            panel.Controls.Add(soundIcon);
        }

        var valueLabel = new Label
        {
            Text = "-",
            ForeColor = textColor,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 40F * scale, FontStyle.Bold),
            Size = new Size(cardWidth - sidePadding * 2, Scale(58, scale)),
            Location = new Point(sidePadding, Scale(28, scale))
        };

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(valueLabel);

        return new StatusCard(panel, valueLabel, baseBackgroundColor);
    }

    private static Panel CreateIconPanel(int x, int y, int size)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(size, size),
            BackColor = Color.Transparent,
            TabStop = false
        };
    }

    private static void DrawLinkIcon(Graphics graphics, Rectangle bounds, Color color, float scale)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, Math.Max(1.4F, 1.6F * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        var w = bounds.Width;
        var h = bounds.Height;
        var r1 = new RectangleF(w * 0.18F, h * 0.39F, w * 0.42F, h * 0.28F);
        var r2 = new RectangleF(w * 0.40F, h * 0.25F, w * 0.42F, h * 0.28F);
        graphics.DrawArc(pen, r1, 120, 240);
        graphics.DrawArc(pen, r2, 300, 240);
        graphics.DrawLine(pen, w * 0.40F, h * 0.55F, w * 0.61F, h * 0.42F);
    }

    private static void DrawSoundIcon(Graphics graphics, Rectangle bounds, Color color, bool muted, float scale)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, Math.Max(1.4F, 1.6F * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var brush = new SolidBrush(color);

        var w = bounds.Width;
        var h = bounds.Height;
        var speaker = new PointF[]
        {
            new(w * 0.20F, h * 0.43F),
            new(w * 0.36F, h * 0.43F),
            new(w * 0.54F, h * 0.28F),
            new(w * 0.54F, h * 0.72F),
            new(w * 0.36F, h * 0.57F),
            new(w * 0.20F, h * 0.57F)
        };
        graphics.FillPolygon(brush, speaker);

        if (muted)
        {
            graphics.DrawLine(pen, w * 0.66F, h * 0.36F, w * 0.86F, h * 0.64F);
            graphics.DrawLine(pen, w * 0.86F, h * 0.36F, w * 0.66F, h * 0.64F);
        }
        else
        {
            graphics.DrawArc(pen, w * 0.51F, h * 0.34F, w * 0.22F, h * 0.32F, -55, 110);
            graphics.DrawArc(pen, w * 0.50F, h * 0.24F, w * 0.38F, h * 0.52F, -55, 110);
        }
    }

    private void OpenItemLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url.Trim())
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // 링크를 열 수 없는 경우 아무 동작도 하지 않는다.
        }
    }

    private void RebuildTrayItems()
    {
        _displayItemsMenu.DropDownItems.Clear();

        if (_settings.Items.Count == 0)
        {
            _displayItemsMenu.DropDownItems.Add(new ToolStripMenuItem("(항목 없음)")
            {
                Enabled = false
            });
            return;
        }

        foreach (var item in _settings.Items)
        {
            var valueName = item.ValueName;
            var menuItem = new ToolStripMenuItem(GetDisplayName(item))
            {
                CheckOnClick = true,
                Checked = item.Visible
            };

            menuItem.Click += (_, _) => SetStatusVisibility(valueName, menuItem.Checked);
            _displayItemsMenu.DropDownItems.Add(menuItem);
        }
    }

    private void UpdateValues()
    {
        var hasActiveFlash = false;

        foreach (var pair in _cards)
        {
            var card = pair.Value;

            if (!_statusData.TryGetValue(pair.Key, out var value))
            {
                card.ValueLabel.Text = "-";
                card.CurrentValue = null;
                card.IsFlashing = false;
                card.Container.BackColor = card.BaseBackgroundColor;
                continue;
            }

            card.ValueLabel.Text = FormatValue(value);
            card.CurrentValue = value;

            if (_lastValues.TryGetValue(pair.Key, out var previousValue) && previousValue != value)
            {
                card.IsFlashing = true;
                card.FlashUntilUtc = DateTime.UtcNow.AddSeconds(1);
                hasActiveFlash = true;
            }

            _lastValues[pair.Key] = value;
            ApplyCardAppearance(card, false);
        }

        if (hasActiveFlash && !_flashTimer.Enabled)
        {
            _flashPhase = false;
            _flashTimer.Start();
        }
    }

    private void PlayActiveSounds()
    {
        foreach (var item in _settings.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SoundFile) || _mutedItems.Contains(item.ValueName))
            {
                continue;
            }

            if (!_statusData.TryGetValue(item.ValueName, out var value) || value == 0)
            {
                continue;
            }

            var path = ResolveSoundPath(item.SoundFile);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                _audioPlaybackService.PlayLooping(item.ValueName, path);
            }
            catch
            {
                // 알림음 재생 실패 시 다음 폴링에서 다시 시도한다.
            }
        }
    }

    private static string ResolveSoundPath(string soundFile)
    {
        var trimmed = soundFile.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return trimmed;
        }

        var baseDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, trimmed);
    }

    private void FlashTimer_Tick(object? sender, EventArgs e)
    {
        _flashPhase = !_flashPhase;
        var now = DateTime.UtcNow;
        var hasActiveFlash = false;

        foreach (var card in _cards.Values)
        {
            if (!card.IsFlashing)
            {
                continue;
            }

            if (now >= card.FlashUntilUtc)
            {
                card.IsFlashing = false;
                ApplyCardAppearance(card, false);
                continue;
            }

            hasActiveFlash = true;
            ApplyCardAppearance(card, _flashPhase);
        }

        if (!hasActiveFlash)
        {
            _flashTimer.Stop();
            _flashPhase = false;
        }
    }

    private static void ApplyCardAppearance(StatusCard card, bool flashOn)
    {
        var normalColor = card.CurrentValue == 0
            ? DarkenColor(card.BaseBackgroundColor, 0.82F)
            : card.BaseBackgroundColor;

        card.Container.BackColor = flashOn
            ? BlendColor(normalColor, Color.White, 0.32F)
            : normalColor;
    }

    private void UpdateWindowSize()
    {
        var scale = GetScale();
        var cardWidth = Scale(BaseCardWidth, scale);
        var cardHeight = Scale(BaseCardHeight, scale);
        var gap = Scale(BaseCardGap, scale);
        var padding = Scale(BasePadding, scale);
        var visibleCount = _settings.Items.Count(item => item.Visible);
        var gapCount = Math.Max(0, visibleCount - 1);

        int contentWidth;
        int contentHeight;

        if (IsHorizontalLayout())
        {
            contentWidth = padding * 2 + (visibleCount == 0 ? cardWidth : visibleCount * cardWidth + gapCount * gap);
            contentHeight = padding * 2 + cardHeight;
        }
        else
        {
            contentWidth = padding * 2 + cardWidth;
            contentHeight = padding * 2 + (visibleCount == 0 ? cardHeight : visibleCount * cardHeight + gapCount * gap);
        }

        ClientSize = new Size(contentWidth, contentHeight);
    }

    private void ConfigurePolling()
    {
        _pollingTimer.Stop();
        _pollingTimer.Interval = Math.Clamp(_settings.PollingSeconds, 1, 3600) * 1000;

        if (!string.IsNullOrWhiteSpace(_settings.DataUrl))
        {
            _pollingTimer.Start();
        }
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        await RefreshStatusAsync();
    }

    private async void PollingTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        if (_isPolling || string.IsNullOrWhiteSpace(_settings.DataUrl))
        {
            return;
        }

        _isPolling = true;
        try
        {
            var data = await _pollingService.GetStatusAsync(_settings.DataUrl);
            if (data is null)
            {
                return;
            }

            _statusData = data;
            _audioPlaybackService.StopAll();
            UpdateValues();
            PlayActiveSounds();
        }
        catch
        {
            // 조회 실패 시 현재 화면 값을 유지하고 다음 폴링에서 다시 시도한다.
        }
        finally
        {
            _isPolling = false;
        }
    }

    private void ShowMainWindow()
    {
        if (!Visible)
        {
            Show();
        }

        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        Close();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitRequested || e.CloseReason != CloseReason.UserClosing)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _pollingTimer.Stop();
        _pollingTimer.Dispose();
        _flashTimer.Stop();
        _flashTimer.Dispose();
        _audioPlaybackService.Dispose();
        _pollingService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _appIcon.Dispose();
    }

    private bool IsHorizontalLayout()
    {
        return string.Equals(_settings.Layout, "Horizontal", StringComparison.OrdinalIgnoreCase);
    }

    private float GetScale()
    {
        return Math.Clamp(_settings.ScalePercent, 50, 200) / 100F;
    }

    private static int Scale(int value, float scale)
    {
        return Math.Max(1, (int)Math.Round(value * scale));
    }

    private string GetProgramName()
    {
        return string.IsNullOrWhiteSpace(_settings.ProgramName)
            ? AppSettings.DefaultProgramName
            : _settings.ProgramName.Trim();
    }

    private static string GetNotifyIconText(string programName)
    {
        return programName.Length <= 63 ? programName : programName[..63];
    }

    private static string GetDisplayName(MonitoringItem item)
    {
        return string.IsNullOrWhiteSpace(item.DisplayName) ? item.ValueName : item.DisplayName;
    }

    private static string FormatValue(decimal value)
    {
        return value == decimal.Truncate(value)
            ? value.ToString("0")
            : value.ToString("0.##");
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    private static Color DarkenColor(Color color, float factor)
    {
        return Color.FromArgb(
            color.A,
            Math.Clamp((int)Math.Round(color.R * factor), 0, 255),
            Math.Clamp((int)Math.Round(color.G * factor), 0, 255),
            Math.Clamp((int)Math.Round(color.B * factor), 0, 255));
    }

    private static Color BlendColor(Color source, Color target, float amount)
    {
        return Color.FromArgb(
            source.A,
            Math.Clamp((int)Math.Round(source.R + (target.R - source.R) * amount), 0, 255),
            Math.Clamp((int)Math.Round(source.G + (target.G - source.G) * amount), 0, 255),
            Math.Clamp((int)Math.Round(source.B + (target.B - source.B) * amount), 0, 255));
    }

    private sealed class StatusCard
    {
        public StatusCard(Panel container, Label valueLabel, Color baseBackgroundColor)
        {
            Container = container;
            ValueLabel = valueLabel;
            BaseBackgroundColor = baseBackgroundColor;
        }

        public Panel Container { get; }
        public Label ValueLabel { get; }
        public Color BaseBackgroundColor { get; }
        public decimal? CurrentValue { get; set; }
        public bool IsFlashing { get; set; }
        public DateTime FlashUntilUtc { get; set; }
    }
}

