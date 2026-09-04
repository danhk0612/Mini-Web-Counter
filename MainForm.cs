using JCMS_Mini_Monitoring.Models;
using JCMS_Mini_Monitoring.Services;

namespace JCMS_Mini_Monitoring;

public sealed class MainForm : Form
{
    private const int BaseCardWidth = 260;
    private const int BaseCardHeight = 92;
    private const int BaseCardGap = 8;
    private const int BasePadding = 10;
    private const int BaseHeaderHeight = 42;

    private readonly SettingsService _settingsService = new();
    private readonly StatusPollingService _pollingService = new();
    private readonly System.Windows.Forms.Timer _pollingTimer = new();
    private readonly Panel _header = new();
    private readonly Label _titleLabel = new();
    private readonly Button _settingsButton = new();
    private readonly FlowLayoutPanel _cardsPanel = new();
    private readonly NotifyIcon _notifyIcon = new();
    private readonly ToolStripMenuItem _displayItemsMenu = new("표시 항목");
    private readonly Dictionary<string, StatusCard> _cards = new(StringComparer.Ordinal);

    private AppSettings _settings;
    private StatusData _statusData = new();
    private bool _isPolling;
    private bool _exitRequested;

    public MainForm()
    {
        _settings = _settingsService.Load();

        Text = "JCMS Mini Monitoring";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        TopMost = true;
        ShowInTaskbar = true;
        ClientSize = new Size(280, 560);
        BackColor = Color.FromArgb(242, 244, 247);
        Font = new Font("Segoe UI", 9F);
        Icon = SystemIcons.Application;

        BuildUi();
        BuildTrayMenu();
        ApplySettingsToUi();
        ConfigurePolling();

        _pollingTimer.Tick += PollingTimer_Tick;
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
    }

    private void BuildUi()
    {
        _header.Dock = DockStyle.Top;
        _header.BackColor = Color.White;

        _titleLabel.Text = "상태 모니터";
        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _header.Controls.Add(_titleLabel);

        _settingsButton.Text = "설정";
        _settingsButton.FlatStyle = FlatStyle.Flat;
        _settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _settingsButton.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 220);
        _settingsButton.Click += async (_, _) => await OpenSettingsAsync();
        _header.Controls.Add(_settingsButton);

        _cardsPanel.Dock = DockStyle.Fill;
        _cardsPanel.WrapContents = false;
        _cardsPanel.AutoScroll = false;
        _cardsPanel.BackColor = BackColor;

        Controls.Add(_cardsPanel);
        Controls.Add(_header);
    }

    private void BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(new ToolStripMenuItem("상태 모니터")
        {
            Enabled = false
        });
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

        _notifyIcon.Text = "JCMS Mini Monitoring";
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private async Task OpenSettingsAsync()
    {
        using var dialog = new SettingsForm(_settings);
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

        ApplyScaleToHeader();
        RebuildCards();
        RebuildTrayItems();
        UpdateValues();
        UpdateWindowSize();
    }

    private void ApplyScaleToHeader()
    {
        var scale = GetScale();
        var headerHeight = Scale(BaseHeaderHeight, scale);

        _header.Height = headerHeight;
        _titleLabel.Font = new Font("Segoe UI", 10F * scale, FontStyle.Bold);
        _titleLabel.Location = new Point(Scale(12, scale), Scale(11, scale));

        _settingsButton.Size = new Size(Scale(58, scale), Scale(27, scale));
        _settingsButton.Location = new Point(ClientSize.Width - _settingsButton.Width - Scale(12, scale), Scale(7, scale));
        _settingsButton.Font = new Font("Segoe UI", 9F * scale);
    }

    private void RebuildCards()
    {
        var scale = GetScale();
        var gap = Scale(BaseCardGap, scale);
        var padding = Scale(BasePadding, scale);
        var horizontal = IsHorizontalLayout();

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
        var sidePadding = Scale(12, scale);

        var panel = new Panel
        {
            Size = new Size(cardWidth, cardHeight),
            Margin = horizontal
                ? new Padding(0, 0, gap, 0)
                : new Padding(0, 0, 0, gap),
            BackColor = ParseColor(item.BackgroundColor, Color.DimGray)
        };

        var textColor = ParseColor(item.TextColor, Color.White);

        var titleLabel = new Label
        {
            Text = item.ValueName,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 11.5F * scale, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Size = new Size(cardWidth - sidePadding * 2, Scale(28, scale)),
            Location = new Point(sidePadding, Scale(7, scale))
        };

        var valueLabel = new Label
        {
            Text = "-",
            ForeColor = textColor,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 27F * scale, FontStyle.Bold),
            Size = new Size(cardWidth - sidePadding * 2, Scale(50, scale)),
            Location = new Point(sidePadding, Scale(33, scale))
        };

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(valueLabel);

        return new StatusCard(panel, valueLabel);
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
            var menuItem = new ToolStripMenuItem(valueName)
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
        foreach (var pair in _cards)
        {
            pair.Value.ValueLabel.Text = _statusData.TryGetValue(pair.Key, out var value)
                ? value.ToString("#,##0.##")
                : "-";
        }
    }

    private void UpdateWindowSize()
    {
        var scale = GetScale();
        var cardWidth = Scale(BaseCardWidth, scale);
        var cardHeight = Scale(BaseCardHeight, scale);
        var gap = Scale(BaseCardGap, scale);
        var padding = Scale(BasePadding, scale);
        var headerHeight = Scale(BaseHeaderHeight, scale);
        var visibleCount = _settings.Items.Count(item => item.Visible);

        int contentWidth;
        int contentHeight;

        if (IsHorizontalLayout())
        {
            contentWidth = padding * 2 + (visibleCount == 0 ? cardWidth : visibleCount * (cardWidth + gap));
            contentHeight = padding * 2 + (visibleCount == 0 ? 0 : cardHeight);
        }
        else
        {
            contentWidth = padding * 2 + cardWidth;
            contentHeight = padding * 2 + visibleCount * (cardHeight + gap);
        }

        ClientSize = new Size(contentWidth, headerHeight + contentHeight);
        _settingsButton.Location = new Point(ClientSize.Width - _settingsButton.Width - Scale(12, scale), Scale(7, scale));
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
            UpdateValues();
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
        _pollingService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
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

    private static Color ParseColor(string value, Color fallback)
    {
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    private sealed record StatusCard(Panel Container, Label ValueLabel);
}
