using JCMS_Mini_Monitoring.Models;
using JCMS_Mini_Monitoring.Services;

namespace JCMS_Mini_Monitoring;

public sealed class MainForm : Form
{
    private const int WindowWidth = 280;
    private const int HeaderHeight = 42;
    private const int CardHeight = 92;
    private const int CardGap = 8;

    private readonly SettingsService _settingsService = new();
    private readonly StatusPollingService _pollingService = new();
    private readonly System.Windows.Forms.Timer _pollingTimer = new();
    private readonly FlowLayoutPanel _cardsPanel = new();
    private readonly NotifyIcon _notifyIcon = new();
    private readonly Dictionary<string, StatusCard> _cards = new();
    private readonly Dictionary<string, ToolStripMenuItem> _trayStatusItems = new();

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
        ClientSize = new Size(WindowWidth, 560);
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
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight,
            BackColor = Color.White
        };

        var titleLabel = new Label
        {
            Text = "상태 모니터",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Location = new Point(12, 12)
        };
        header.Controls.Add(titleLabel);

        var settingsButton = new Button
        {
            Text = "설정",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(58, 27),
            Location = new Point(WindowWidth - 70, 7),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        settingsButton.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 220);
        settingsButton.Click += async (_, _) => await OpenSettingsAsync();
        header.Controls.Add(settingsButton);

        _cardsPanel.Dock = DockStyle.Fill;
        _cardsPanel.FlowDirection = FlowDirection.TopDown;
        _cardsPanel.WrapContents = false;
        _cardsPanel.AutoScroll = false;
        _cardsPanel.Padding = new Padding(10, 10, 10, 0);
        _cardsPanel.BackColor = BackColor;

        Controls.Add(_cardsPanel);
        Controls.Add(header);

        _cards["fire"] = CreateCard("화재", "●", Color.FromArgb(208, 57, 57));
        _cards["facility"] = CreateCard("설비", "⚙", Color.FromArgb(49, 142, 90));
        _cards["fault"] = CreateCard("고장", "▲", Color.FromArgb(217, 143, 42));
        _cards["block"] = CreateCard("차단", "⊘", Color.FromArgb(47, 111, 191));
        _cards["spare"] = CreateCard("예비", "○", Color.FromArgb(105, 113, 124));

        foreach (var card in _cards.Values)
        {
            _cardsPanel.Controls.Add(card.Container);
        }
    }

    private StatusCard CreateCard(string title, string symbol, Color color)
    {
        var panel = new Panel
        {
            Size = new Size(WindowWidth - 20, CardHeight),
            Margin = new Padding(0, 0, 0, CardGap),
            BackColor = color
        };

        var titleLabel = new Label
        {
            Text = title,
            ForeColor = Color.White,
            AutoSize = true,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(12, 10)
        };

        var iconLabel = new Label
        {
            Text = symbol,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Symbol", 15F, FontStyle.Bold),
            Size = new Size(36, 28),
            Location = new Point(panel.Width - 48, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var valueLabel = new Label
        {
            Text = "0",
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 26F, FontStyle.Bold),
            Size = new Size(panel.Width - 24, 48),
            Location = new Point(12, 35),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(iconLabel);
        panel.Controls.Add(valueLabel);

        return new StatusCard(panel, valueLabel);
    }

    private void BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        var titleItem = new ToolStripMenuItem("상태 모니터")
        {
            Enabled = false
        };
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());

        var showItem = new ToolStripMenuItem("화면 표시");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var settingsItem = new ToolStripMenuItem("설정");
        settingsItem.Click += async (_, _) => await OpenSettingsAsync();
        menu.Items.Add(settingsItem);

        var displayItems = new ToolStripMenuItem("표시 항목");
        AddTrayStatusItem(displayItems, "fire", "화재");
        AddTrayStatusItem(displayItems, "facility", "설비");
        AddTrayStatusItem(displayItems, "fault", "고장");
        AddTrayStatusItem(displayItems, "block", "차단");
        AddTrayStatusItem(displayItems, "spare", "예비");
        menu.Items.Add(displayItems);

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

    private void AddTrayStatusItem(ToolStripMenuItem parent, string key, string text)
    {
        var item = new ToolStripMenuItem(text)
        {
            CheckOnClick = true
        };
        item.Click += (_, _) => SetStatusVisibility(key, item.Checked);
        parent.DropDownItems.Add(item);
        _trayStatusItems[key] = item;
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

    private void SetStatusVisibility(string key, bool visible)
    {
        switch (key)
        {
            case "fire":
                _settings.ShowFire = visible;
                break;
            case "facility":
                _settings.ShowFacility = visible;
                break;
            case "fault":
                _settings.ShowFault = visible;
                break;
            case "block":
                _settings.ShowBlock = visible;
                break;
            case "spare":
                _settings.ShowSpare = visible;
                break;
        }

        _settingsService.Save(_settings);
        ApplySettingsToUi();
    }

    private void ApplySettingsToUi()
    {
        SetCardState("fire", _settings.ShowFire);
        SetCardState("facility", _settings.ShowFacility);
        SetCardState("fault", _settings.ShowFault);
        SetCardState("block", _settings.ShowBlock);
        SetCardState("spare", _settings.ShowSpare);
        UpdateValues();
        UpdateWindowHeight();
    }

    private void SetCardState(string key, bool visible)
    {
        _cards[key].Container.Visible = visible;
        _trayStatusItems[key].Checked = visible;
    }

    private void UpdateValues()
    {
        _cards["fire"].ValueLabel.Text = _statusData.Fire.ToString("N0");
        _cards["facility"].ValueLabel.Text = _statusData.Facility.ToString("N0");
        _cards["fault"].ValueLabel.Text = _statusData.Fault.ToString("N0");
        _cards["block"].ValueLabel.Text = _statusData.Block.ToString("N0");
        _cards["spare"].ValueLabel.Text = _statusData.Spare.ToString("N0");
    }

    private void UpdateWindowHeight()
    {
        var visibleCount = 0;
        if (_settings.ShowFire) visibleCount++;
        if (_settings.ShowFacility) visibleCount++;
        if (_settings.ShowFault) visibleCount++;
        if (_settings.ShowBlock) visibleCount++;
        if (_settings.ShowSpare) visibleCount++;

        var cardAreaHeight = visibleCount == 0
            ? 20
            : 10 + visibleCount * (CardHeight + CardGap);

        ClientSize = new Size(WindowWidth, HeaderHeight + cardAreaHeight);
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

    private sealed record StatusCard(Panel Container, Label ValueLabel);
}
