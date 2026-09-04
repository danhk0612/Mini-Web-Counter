using JCMS_Mini_Monitoring.Models;

namespace JCMS_Mini_Monitoring;

public sealed class SettingsForm : Form
{
    private readonly TextBox _programNameTextBox = new();
    private readonly TextBox _urlTextBox = new();
    private readonly TextBox _programBackgroundTextBox = new();
    private readonly NumericUpDown _pollingNumeric = new();
    private readonly ComboBox _layoutComboBox = new();
    private readonly NumericUpDown _scaleNumeric = new();
    private readonly DataGridView _itemsGrid = new();

    public AppSettings ResultSettings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        ResultSettings = CloneSettings(settings);
        Text = $"{GetProgramName(settings.ProgramName)} 설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(1160, 620);
        Font = new Font("Segoe UI", 9F);
        BuildUi();
        LoadSettings(settings);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 9 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        root.Controls.Add(new Label { Text = "프로그램 이름", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _programNameTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(_programNameTextBox, 0, 1);
        root.Controls.Add(new Label { Text = "데이터 URL", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        _urlTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(_urlTextBox, 0, 3);

        var optionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 9, 0, 0) };
        optionsPanel.Controls.Add(CreateOptionLabel("폴링 주기"));
        _pollingNumeric.Minimum = 1; _pollingNumeric.Maximum = 3600; _pollingNumeric.Width = 70;
        optionsPanel.Controls.Add(_pollingNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("초", 4, 5, 18));
        optionsPanel.Controls.Add(CreateOptionLabel("배치", 10, 5, 6));
        _layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _layoutComboBox.Items.AddRange(["세로형", "가로형"]);
        _layoutComboBox.Width = 90;
        optionsPanel.Controls.Add(_layoutComboBox);
        optionsPanel.Controls.Add(CreateOptionLabel("배율", 10, 5, 6));
        _scaleNumeric.Minimum = 50; _scaleNumeric.Maximum = 200; _scaleNumeric.Increment = 10; _scaleNumeric.Width = 70;
        optionsPanel.Controls.Add(_scaleNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("%", 4, 5, 18));
        optionsPanel.Controls.Add(CreateOptionLabel("프로그램 배경", 4, 5, 6));
        _programBackgroundTextBox.Width = 95;
        optionsPanel.Controls.Add(_programBackgroundTextBox);
        root.Controls.Add(optionsPanel, 0, 4);

        root.Controls.Add(new Label { Text = "색상은 #RRGGBB 형식으로 직접 입력합니다. 알림음은 WAV 또는 MP3 파일을 사용할 수 있습니다.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 5);
        ConfigureItemsGrid();
        root.Controls.Add(_itemsGrid, 0, 6);

        var itemButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        var addButton = new Button { Text = "항목 추가", Width = 90 };
        addButton.Click += (_, _) => AddItemRow();
        var deleteButton = new Button { Text = "선택 삭제", Width = 90 };
        deleteButton.Click += (_, _) => DeleteSelectedItem();
        var moveUpButton = new Button { Text = "위로", Width = 70 };
        moveUpButton.Click += (_, _) => MoveSelectedItem(-1);
        var moveDownButton = new Button { Text = "아래로", Width = 70 };
        moveDownButton.Click += (_, _) => MoveSelectedItem(1);
        itemButtons.Controls.Add(addButton);
        itemButtons.Controls.Add(deleteButton);
        itemButtons.Controls.Add(moveUpButton);
        itemButtons.Controls.Add(moveDownButton);
        root.Controls.Add(itemButtons, 0, 7);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        var cancelButton = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Width = 80 };
        var saveButton = new Button { Text = "저장", Width = 80 };
        saveButton.Click += SaveButton_Click;
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        root.Controls.Add(buttons, 0, 8);
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
        _itemsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Visible", HeaderText = "표시", Width = 50 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayName", HeaderText = "항목 이름", Width = 115 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ValueName", HeaderText = "값 이름", Width = 105 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BackgroundColor", HeaderText = "배경 색", Width = 90 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TextColor", HeaderText = "글자 색", Width = 90 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LinkUrl", HeaderText = "링크 URL", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 190 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoundFile", HeaderText = "알림음 파일", Width = 250 });
        _itemsGrid.Columns.Add(new DataGridViewButtonColumn { Name = "BrowseSound", HeaderText = "찾기", Text = "...", UseColumnTextForButtonValue = true, Width = 50, FlatStyle = FlatStyle.Popup });
        _itemsGrid.CellContentClick += ItemsGrid_CellContentClick;
    }

    private static Label CreateOptionLabel(string text, int left = 0, int top = 5, int right = 8) => new() { Text = text, AutoSize = true, Margin = new Padding(left, top, right, 0) };

    private void LoadSettings(AppSettings settings)
    {
        _programNameTextBox.Text = GetProgramName(settings.ProgramName);
        _urlTextBox.Text = settings.DataUrl;
        _pollingNumeric.Value = Math.Clamp(settings.PollingSeconds, 1, 3600);
        _layoutComboBox.SelectedIndex = string.Equals(settings.Layout, "Horizontal", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _scaleNumeric.Value = Math.Clamp(settings.ScalePercent, 50, 200);
        _programBackgroundTextBox.Text = string.IsNullOrWhiteSpace(settings.ProgramBackgroundColor) ? "#FFFFFF" : settings.ProgramBackgroundColor;
        _itemsGrid.Rows.Clear();
        foreach (var item in settings.Items ?? []) AddItemRow(item);
    }

    private void AddItemRow()
    {
        var number = 1;
        string valueName;
        do { valueName = $"value{number++}"; }
        while (_itemsGrid.Rows.Cast<DataGridViewRow>().Any(row => string.Equals(Convert.ToString(row.Cells["ValueName"].Value), valueName, StringComparison.Ordinal)));
        AddItemRow(new MonitoringItem { ValueName = valueName, DisplayName = valueName });
    }

    private void AddItemRow(MonitoringItem item)
    {
        var displayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.ValueName : item.DisplayName;
        _itemsGrid.Rows.Add(item.Visible, displayName, item.ValueName, item.BackgroundColor, item.TextColor, item.LinkUrl, item.SoundFile, "...");
    }

    private void DeleteSelectedItem()
    {
        if (_itemsGrid.CurrentRow is not null) _itemsGrid.Rows.Remove(_itemsGrid.CurrentRow);
    }

    private void MoveSelectedItem(int offset)
    {
        _itemsGrid.EndEdit();
        var row = _itemsGrid.CurrentRow;
        if (row is null) return;
        var oldIndex = row.Index;
        var newIndex = oldIndex + offset;
        if (newIndex < 0 || newIndex >= _itemsGrid.Rows.Count) return;
        _itemsGrid.Rows.RemoveAt(oldIndex);
        _itemsGrid.Rows.Insert(newIndex, row);
        _itemsGrid.ClearSelection();
        row.Selected = true;
        _itemsGrid.CurrentCell = row.Cells["DisplayName"];
    }

    private void ItemsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && _itemsGrid.Columns[e.ColumnIndex].Name == "BrowseSound") SelectSoundFile(e.RowIndex);
    }

    private void SelectSoundFile(int rowIndex)
    {
        using var dialog = new OpenFileDialog { Title = "알림음 파일 선택", Filter = "오디오 파일 (*.wav;*.mp3)|*.wav;*.mp3|WAV 파일 (*.wav)|*.wav|MP3 파일 (*.mp3)|*.mp3|모든 파일 (*.*)|*.*", CheckFileExists = true, Multiselect = false };
        var currentPath = Convert.ToString(_itemsGrid.Rows[rowIndex].Cells["SoundFile"].Value);
        if (!string.IsNullOrWhiteSpace(currentPath) && File.Exists(currentPath)) dialog.FileName = currentPath;
        if (dialog.ShowDialog(this) == DialogResult.OK) _itemsGrid.Rows[rowIndex].Cells["SoundFile"].Value = dialog.FileName;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        _itemsGrid.EndEdit();
        var items = new List<MonitoringItem>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (DataGridViewRow row in _itemsGrid.Rows)
        {
            var valueName = (Convert.ToString(row.Cells["ValueName"].Value) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(valueName)) { MessageBox.Show(this, "값 이름은 비워둘 수 없습니다.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (!names.Add(valueName)) { MessageBox.Show(this, $"같은 값 이름을 중복해서 사용할 수 없습니다: {valueName}", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var displayName = (Convert.ToString(row.Cells["DisplayName"].Value) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = valueName;
            items.Add(new MonitoringItem
            {
                Visible = Convert.ToBoolean(row.Cells["Visible"].Value ?? true),
                DisplayName = displayName,
                ValueName = valueName,
                BackgroundColor = (Convert.ToString(row.Cells["BackgroundColor"].Value) ?? "#666666").Trim(),
                TextColor = (Convert.ToString(row.Cells["TextColor"].Value) ?? "#FFFFFF").Trim(),
                LinkUrl = (Convert.ToString(row.Cells["LinkUrl"].Value) ?? string.Empty).Trim(),
                SoundFile = (Convert.ToString(row.Cells["SoundFile"].Value) ?? string.Empty).Trim()
            });
        }
        ResultSettings = new AppSettings
        {
            ProgramName = GetProgramName(_programNameTextBox.Text),
            ProgramBackgroundColor = string.IsNullOrWhiteSpace(_programBackgroundTextBox.Text) ? "#FFFFFF" : _programBackgroundTextBox.Text.Trim(),
            DataUrl = _urlTextBox.Text.Trim(),
            PollingSeconds = (int)_pollingNumeric.Value,
            Layout = _layoutComboBox.SelectedIndex == 1 ? "Horizontal" : "Vertical",
            ScalePercent = (int)_scaleNumeric.Value,
            Items = items
        };
        DialogResult = DialogResult.OK;
        Close();
    }

    private static AppSettings CloneSettings(AppSettings source) => new()
    {
        ProgramName = GetProgramName(source.ProgramName),
        ProgramBackgroundColor = string.IsNullOrWhiteSpace(source.ProgramBackgroundColor) ? "#FFFFFF" : source.ProgramBackgroundColor,
        DataUrl = source.DataUrl,
        PollingSeconds = source.PollingSeconds,
        Layout = source.Layout,
        ScalePercent = source.ScalePercent,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
        {
            ValueName = item.ValueName,
            DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.ValueName : item.DisplayName,
            BackgroundColor = item.BackgroundColor,
            TextColor = item.TextColor,
            LinkUrl = item.LinkUrl,
            SoundFile = item.SoundFile,
            Visible = item.Visible
        }).ToList()
    };

    private static string GetProgramName(string? programName) => string.IsNullOrWhiteSpace(programName) ? AppSettings.DefaultProgramName : programName.Trim();
}
