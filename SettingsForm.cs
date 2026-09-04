using JCMS_Mini_Monitoring.Models;

namespace JCMS_Mini_Monitoring;

public sealed class SettingsForm : Form
{
    private readonly TextBox _urlTextBox = new();
    private readonly NumericUpDown _pollingNumeric = new();
    private readonly ComboBox _layoutComboBox = new();
    private readonly NumericUpDown _scaleNumeric = new();
    private readonly DataGridView _itemsGrid = new();

    public AppSettings ResultSettings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        ResultSettings = CloneSettings(settings);

        Text = "설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(720, 540);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        LoadSettings(settings);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 7
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "데이터 URL",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _urlTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(_urlTextBox, 0, 1);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 9, 0, 0)
        };

        optionsPanel.Controls.Add(CreateOptionLabel("폴링 주기"));
        _pollingNumeric.Minimum = 1;
        _pollingNumeric.Maximum = 3600;
        _pollingNumeric.Width = 70;
        optionsPanel.Controls.Add(_pollingNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("초", 4, 5, 18));

        optionsPanel.Controls.Add(CreateOptionLabel("배치", 10, 5, 6));
        _layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _layoutComboBox.Items.AddRange(["세로형", "가로형"]);
        _layoutComboBox.Width = 90;
        optionsPanel.Controls.Add(_layoutComboBox);

        optionsPanel.Controls.Add(CreateOptionLabel("배율", 10, 5, 6));
        _scaleNumeric.Minimum = 50;
        _scaleNumeric.Maximum = 200;
        _scaleNumeric.Increment = 10;
        _scaleNumeric.Width = 70;
        optionsPanel.Controls.Add(_scaleNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("%", 4, 5, 0));
        root.Controls.Add(optionsPanel, 0, 2);

        root.Controls.Add(new Label
        {
            Text = "표시 항목  ·  색상 칸을 클릭하면 색을 선택할 수 있습니다.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);

        ConfigureItemsGrid();
        root.Controls.Add(_itemsGrid, 0, 4);

        var itemButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };

        var addButton = new Button
        {
            Text = "항목 추가",
            Width = 90
        };
        addButton.Click += (_, _) => AddItemRow();

        var deleteButton = new Button
        {
            Text = "선택 삭제",
            Width = 90
        };
        deleteButton.Click += (_, _) => DeleteSelectedItem();

        itemButtons.Controls.Add(addButton);
        itemButtons.Controls.Add(deleteButton);
        root.Controls.Add(itemButtons, 0, 5);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        var cancelButton = new Button
        {
            Text = "취소",
            DialogResult = DialogResult.Cancel,
            Width = 80
        };

        var saveButton = new Button
        {
            Text = "저장",
            Width = 80
        };
        saveButton.Click += SaveButton_Click;

        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        root.Controls.Add(buttons, 0, 6);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void ConfigureItemsGrid()
    {
        _itemsGrid.Dock = DockStyle.Fill;
        _itemsGrid.AllowUserToAddRows = false;
        _itemsGrid.AllowUserToDeleteRows = false;
        _itemsGrid.AllowUserToResizeRows = false;
        _itemsGrid.RowHeadersVisible = false;
        _itemsGrid.MultiSelect = false;
        _itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _itemsGrid.AutoGenerateColumns = false;
        _itemsGrid.ColumnHeadersHeight = 30;

        _itemsGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Visible",
            HeaderText = "표시",
            Width = 55
        });

        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ValueName",
            HeaderText = "값 이름 (JSON 키)",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220
        });

        _itemsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "BackgroundColor",
            HeaderText = "배경 색",
            Width = 120,
            FlatStyle = FlatStyle.Popup,
            UseColumnTextForButtonValue = false
        });

        _itemsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "TextColor",
            HeaderText = "글자 색",
            Width = 120,
            FlatStyle = FlatStyle.Popup,
            UseColumnTextForButtonValue = false
        });

        _itemsGrid.CellContentClick += ItemsGrid_CellContentClick;
    }

    private static Label CreateOptionLabel(string text, int left = 0, int top = 5, int right = 8)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(left, top, right, 0)
        };
    }

    private void LoadSettings(AppSettings settings)
    {
        _urlTextBox.Text = settings.DataUrl;
        _pollingNumeric.Value = Math.Clamp(settings.PollingSeconds, 1, 3600);
        _layoutComboBox.SelectedIndex = string.Equals(settings.Layout, "Horizontal", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _scaleNumeric.Value = Math.Clamp(settings.ScalePercent, 50, 200);

        _itemsGrid.Rows.Clear();
        foreach (var item in settings.Items ?? [])
        {
            AddItemRow(item);
        }
    }

    private void AddItemRow()
    {
        var number = 1;
        string valueName;

        do
        {
            valueName = $"value{number++}";
        }
        while (_itemsGrid.Rows.Cast<DataGridViewRow>()
            .Any(row => string.Equals(Convert.ToString(row.Cells["ValueName"].Value), valueName, StringComparison.Ordinal)));

        AddItemRow(new MonitoringItem { ValueName = valueName });
    }

    private void AddItemRow(MonitoringItem item)
    {
        var rowIndex = _itemsGrid.Rows.Add(item.Visible, item.ValueName, item.BackgroundColor, item.TextColor);
        ApplyColorCellStyle(rowIndex, "BackgroundColor", item.BackgroundColor);
        ApplyColorCellStyle(rowIndex, "TextColor", item.TextColor);
    }

    private void DeleteSelectedItem()
    {
        if (_itemsGrid.CurrentRow is not null)
        {
            _itemsGrid.Rows.Remove(_itemsGrid.CurrentRow);
        }
    }

    private void ItemsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        var columnName = _itemsGrid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("BackgroundColor" or "TextColor"))
        {
            return;
        }

        var cell = _itemsGrid.Rows[e.RowIndex].Cells[columnName];
        var currentHex = Convert.ToString(cell.Value) ?? "#FFFFFF";

        using var dialog = new ColorDialog
        {
            FullOpen = true,
            Color = ParseColor(currentHex, Color.White)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var newHex = ToHex(dialog.Color);
        cell.Value = newHex;
        ApplyColorCellStyle(e.RowIndex, columnName, newHex);
    }

    private void ApplyColorCellStyle(int rowIndex, string columnName, string hex)
    {
        var color = ParseColor(hex, Color.White);
        var cell = _itemsGrid.Rows[rowIndex].Cells[columnName];
        cell.Style.BackColor = color;
        cell.Style.SelectionBackColor = color;

        var textColor = GetContrastingColor(color);
        cell.Style.ForeColor = textColor;
        cell.Style.SelectionForeColor = textColor;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        _itemsGrid.EndEdit();

        var items = new List<MonitoringItem>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (DataGridViewRow row in _itemsGrid.Rows)
        {
            var valueName = (Convert.ToString(row.Cells["ValueName"].Value) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(valueName))
            {
                MessageBox.Show(this, "값 이름은 비워둘 수 없습니다.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!names.Add(valueName))
            {
                MessageBox.Show(this, $"같은 값 이름을 중복해서 사용할 수 없습니다: {valueName}", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            items.Add(new MonitoringItem
            {
                Visible = Convert.ToBoolean(row.Cells["Visible"].Value ?? true),
                ValueName = valueName,
                BackgroundColor = Convert.ToString(row.Cells["BackgroundColor"].Value) ?? "#666666",
                TextColor = Convert.ToString(row.Cells["TextColor"].Value) ?? "#FFFFFF"
            });
        }

        ResultSettings = new AppSettings
        {
            DataUrl = _urlTextBox.Text.Trim(),
            PollingSeconds = (int)_pollingNumeric.Value,
            Layout = _layoutComboBox.SelectedIndex == 1 ? "Horizontal" : "Vertical",
            ScalePercent = (int)_scaleNumeric.Value,
            Items = items
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static AppSettings CloneSettings(AppSettings source)
    {
        return new AppSettings
        {
            DataUrl = source.DataUrl,
            PollingSeconds = source.PollingSeconds,
            Layout = source.Layout,
            ScalePercent = source.ScalePercent,
            Items = (source.Items ?? []).Select(item => new MonitoringItem
            {
                ValueName = item.ValueName,
                BackgroundColor = item.BackgroundColor,
                TextColor = item.TextColor,
                Visible = item.Visible
            }).ToList()
        };
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

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static Color GetContrastingColor(Color color)
    {
        var brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        return brightness >= 150 ? Color.Black : Color.White;
    }
}
