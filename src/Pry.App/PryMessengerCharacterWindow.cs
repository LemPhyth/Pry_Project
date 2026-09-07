using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App;

internal sealed class PryMessengerCharacterWindow : UserControl
{
    private readonly PryBackendClient _api;
    private readonly ListBox _list = new() { Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
    private readonly TextBox _name = Field("角色名称");
    private readonly TextBox _cardName = Field("卡片名称");
    private readonly TextBox _userName = Field("角色对你的称呼");
    private readonly TextBox _identity = Area("身份设定");
    private readonly TextBox _personality = Area("性格设定");
    private readonly TextBox _speech = Area("说话风格");
    private readonly TextBox _greeting = Area("初次问候");
    private readonly TextBox _rules = Area("行为规则，每行一条");
    private readonly TextBox _facts = Area("世界设定，每行一条");
    private readonly TextBox _legacyPrompt = Area("完整 System Prompt（原样作为角色设定）", 260, 32000);
    private readonly StackPanel _structuredPanel = new() { Spacing = 9 };
    private readonly StackPanel _legacyPanel = new() { Spacing = 9, IsVisible = false };
    private readonly TextBlock _status = new() { Foreground = Brush.Parse("#8492A8"), TextWrapping = TextWrapping.Wrap };
    private readonly Image _avatarPreview = new() { Width = 72, Height = 72, Stretch = Stretch.UniformToFill, IsVisible = false };
    private CharacterResponse? _selected;
    private string? _avatarMediaId;
    private bool _clearAvatar;
    private CharacterPromptMode _promptMode = CharacterPromptMode.Structured;

    public PryMessengerCharacterWindow(PryBackendClient api)
    {
        _api = api; Background = Brush.Parse("#101827");
        var add = MakeButton("新建角色", NewCharacter);
        var chooseAvatar = MakeButton("选择头像", ChooseAvatarAsync);
        var clearAvatar = MakeButton("移除头像", ClearAvatar);
        var save = MakeButton("保存角色", () => SaveAsync(false), true);
        var saveAndUse = MakeButton("保存并使用", () => SaveAsync(true));
        var remove = MakeButton("删除角色", DeleteAsync);
        var close = MakeButton("完成", () => CloseRequested?.Invoke());
        _list.SelectionChanged += async (_, _) =>
        {
            if ((_list.SelectedItem as ListBoxItem)?.Tag is CharacterSummaryResponse item) await LoadCharacterAsync(item.Id);
        };
        _list.DoubleTapped += (_, _) => { if ((_list.SelectedItem as ListBoxItem)?.Tag is CharacterSummaryResponse item) CharacterActivated?.Invoke(item); };
        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 12, Margin = new Thickness(18), Children = { add, _list } };
        Grid.SetRow(_list, 1);
        var structured = MakeButton("结构化", () => SetPromptMode(CharacterPromptMode.Structured));
        var legacy = MakeButton("Legacy", () => SetPromptMode(CharacterPromptMode.Legacy));
        _structuredPanel.Children.Add(_userName); _structuredPanel.Children.Add(_identity); _structuredPanel.Children.Add(_personality);
        _structuredPanel.Children.Add(_speech); _structuredPanel.Children.Add(_rules); _structuredPanel.Children.Add(_facts);
        _legacyPanel.Children.Add(new TextBlock { Text = "完整 System Prompt", FontWeight = FontWeight.SemiBold });
        _legacyPanel.Children.Add(new TextBlock { Text = "内容原样作为角色设定；应用只追加记忆、当前状态和回复格式要求。", Foreground = Brush.Parse("#8492A8"), TextWrapping = TextWrapping.Wrap });
        _legacyPanel.Children.Add(_legacyPrompt);
        var form = new StackPanel { Spacing = 9, Margin = new Thickness(20), Children =
        {
            new TextBlock { Text = "角色卡", FontSize = 22, FontWeight = FontWeight.SemiBold },
            new Border { Width = 76, Height = 76, HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(22), Background = Brush.Parse("#26344D"), ClipToBounds = true, Child = _avatarPreview },
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { chooseAvatar, clearAvatar } },
            _name, _cardName,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { structured, legacy } },
            _structuredPanel, _legacyPanel, _greeting, _status,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { remove, saveAndUse, save, close } }
        }};
        var right = new ScrollViewer { Content = form };
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("280,*"), Children = { left, new Border { BorderBrush = Brush.Parse("#263249"), BorderThickness = new Thickness(1,0,0,0), Child = right } } };
        Grid.SetColumn(root.Children[1], 1); Content = root;
        AttachedToVisualTree += async (_, _) => await ReloadAsync();
    }

    public bool Changed { get; private set; }
    public event Action? CloseRequested;
    public event Action<CharacterSummaryResponse>? CharacterActivated;
    private static TextBox Field(string watermark) => new() { Watermark = watermark, MaxLength = 200 };
    private static TextBox Area(string watermark, int minHeight = 62, int maxLength = 4000) => new() { Watermark = watermark, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = minHeight, MaxLength = maxLength };
    private static Button MakeButton(string text, Action action) { var button = new Button { Content = text }; button.Click += (_, _) => action(); return button; }
    private static Button MakeButton(string text, Func<Task> action, bool primary = false) { var button = new Button { Content = text, Background = primary ? Brush.Parse("#6C63FF") : null }; button.Click += async (_, _) => await action(); return button; }

    private async Task ReloadAsync(string? selectId = null)
    {
        try
        {
            var items = await _api.GetCharactersAsync();
            _list.ItemsSource = items.Select(CreateCharacterItem).ToArray();
            _list.SelectedItem = _list.Items.Cast<ListBoxItem>().FirstOrDefault(item => (item.Tag as CharacterSummaryResponse)?.Id == selectId) ?? _list.Items.Cast<ListBoxItem>().FirstOrDefault();
        }
        catch (Exception ex) { _status.Text = $"角色读取失败：{ex.Message}"; }
    }

    private async Task LoadCharacterAsync(string id)
    {
        try
        {
            _selected = await _api.GetCharacterAsync(id); _avatarMediaId = null; _clearAvatar = false;
            _name.Text = _selected.Name; _cardName.Text = _selected.CardName; _userName.Text = _selected.UserName;
            _identity.Text = _selected.Identity; _personality.Text = _selected.Personality; _speech.Text = _selected.SpeechStyle;
            _greeting.Text = _selected.Greeting; _rules.Text = string.Join(Environment.NewLine, _selected.BehavioralRules); _facts.Text = string.Join(Environment.NewLine, _selected.WorldFacts);
            _legacyPrompt.Text = _selected.LegacySystemPrompt; SetPromptMode(_selected.PromptMode);
            _avatarPreview.Source = null; _avatarPreview.IsVisible = false; if (!string.IsNullOrWhiteSpace(_selected.AvatarUrl)) _ = LoadAvatarAsync(_selected.AvatarUrl);
            _status.Text = _selected.PromptMode == CharacterPromptMode.Legacy ? "Legacy 模式：完整提示词会原样保存" : "结构化模式：各字段由后端组合为角色提示词";
        }
        catch (Exception ex) { _status.Text = $"角色读取失败：{ex.Message}"; }
    }

    private void NewCharacter()
    {
        _selected = null; _avatarMediaId = null; _clearAvatar = false; _list.SelectedItem = null;
        foreach (var box in new[] { _name, _cardName, _identity, _personality, _speech, _greeting, _rules, _facts, _legacyPrompt }) box.Text = "";
        _avatarPreview.Source = null; _avatarPreview.IsVisible = false; SetPromptMode(CharacterPromptMode.Structured);
        _userName.Text = "你"; _status.Text = "正在创建新的角色卡"; _name.Focus();
    }

    private async Task ChooseAvatarAsync()
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider; if (storage is null) return;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选择角色头像", AllowMultiple = false, FileTypeFilter = new[] { FilePickerFileTypes.ImageAll } });
        var file = files.FirstOrDefault(); var path = file?.TryGetLocalPath(); if (file is null || path is null) return;
        try { await using var stream = File.OpenRead(path); _avatarMediaId = (await _api.UploadAsync(stream, file.Name, Mime(path))).Id; _clearAvatar = false; _avatarPreview.Source = new Avalonia.Media.Imaging.Bitmap(path); _avatarPreview.IsVisible = true; _status.Text = $"已选择头像：{file.Name}"; }
        catch (Exception ex) { _status.Text = $"头像上传失败：{ex.Message}"; }
    }

    private void ClearAvatar() { _avatarMediaId = null; _clearAvatar = true; _avatarPreview.Source = null; _avatarPreview.IsVisible = false; _status.Text = "保存后移除头像"; }

    private void SetPromptMode(CharacterPromptMode mode)
    {
        _promptMode = mode; _structuredPanel.IsVisible = mode == CharacterPromptMode.Structured; _legacyPanel.IsVisible = mode == CharacterPromptMode.Legacy;
    }

    private async Task SaveAsync(bool activate)
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_cardName.Text)) { _status.Text = "角色名称和卡片名称不能为空"; return; }
        if (_promptMode == CharacterPromptMode.Structured && string.IsNullOrWhiteSpace(_identity.Text)) { _status.Text = "结构化模式下身份设定不能为空"; return; }
        if (_promptMode == CharacterPromptMode.Legacy && string.IsNullOrWhiteSpace(_legacyPrompt.Text)) { _status.Text = "Legacy 模式下完整 System Prompt 不能为空"; return; }
        var request = new SaveCharacterRequest(_name.Text.Trim(), _cardName.Text?.Trim() ?? _name.Text.Trim(), _userName.Text?.Trim() ?? "你",
            _identity.Text?.Trim() ?? "", _personality.Text?.Trim() ?? "", _speech.Text?.Trim() ?? "", Lines(_rules.Text), Lines(_facts.Text), _selected?.InitialState ?? new RuntimeState(),
            _greeting.Text?.Trim() ?? "你好。", _promptMode, _legacyPrompt.Text?.Trim() ?? "", _avatarMediaId, _clearAvatar, _selected?.AvatarDisplay ?? new ImageDisplayPreferences());
        try
        {
            var saved = _selected is null ? await _api.CreateCharacterAsync(request) : await _api.UpdateCharacterAsync(_selected.Id, request);
            Changed = true; await ReloadAsync(saved.Id); _status.Text = activate ? "角色卡已保存并切换" : "角色卡已保存";
            if (activate && (await _api.GetCharactersAsync()).FirstOrDefault(item => item.Id == saved.Id) is { } summary) CharacterActivated?.Invoke(summary);
        }
        catch (Exception ex) { _status.Text = $"保存失败：{ex.Message}"; }
    }

    private async Task DeleteAsync()
    {
        if (_selected is null) { _status.Text = "请先选择一个角色"; return; }
        try { await _api.DeleteCharacterAsync(_selected.Id); Changed = true; _selected = null; await ReloadAsync(); _status.Text = "角色已删除"; }
        catch (Exception ex) { _status.Text = $"无法删除：{ex.Message}"; }
    }

    private static IReadOnlyList<string> Lines(string? value) => (value ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string Mime(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };

    private ListBoxItem CreateCharacterItem(CharacterSummaryResponse item)
    {
        var row = new ListBoxItem { Tag = item, Padding = new Thickness(11), Margin = new Thickness(0,0,0,4), Content = new StackPanel { Spacing = 3, Children = { new TextBlock { Text = item.Name, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = item.CardName, FontSize = 10, Foreground = Brush.Parse("#718198") } } } };
        var edit = new MenuItem { Header = "编辑角色" }; edit.Click += async (_, _) => { _list.SelectedItem = row; await LoadCharacterAsync(item.Id); };
        var remove = new MenuItem { Header = "删除角色" }; remove.Click += async (_, _) => { await LoadCharacterAsync(item.Id); await DeleteAsync(); };
        row.ContextMenu = new ContextMenu { ItemsSource = new[] { edit, remove } };
        row.AddHandler(PointerPressedEvent, (_, args) => { if (args.GetCurrentPoint(row).Properties.IsRightButtonPressed) { args.Handled = true; row.ContextMenu.Open(row); } }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
        return row;
    }

    private async Task LoadAvatarAsync(string url)
    {
        try { var content = await _api.DownloadAsync(url); _avatarPreview.Source = new Avalonia.Media.Imaging.Bitmap(new MemoryStream(content.Bytes)); _avatarPreview.IsVisible = true; } catch { }
    }
}
