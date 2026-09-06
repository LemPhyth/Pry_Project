using Avalonia;
using Avalonia.Controls;
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
    private readonly TextBlock _status = new() { Foreground = Brush.Parse("#8492A8"), TextWrapping = TextWrapping.Wrap };
    private CharacterResponse? _selected;
    private string? _avatarMediaId;

    public PryMessengerCharacterWindow(PryBackendClient api)
    {
        _api = api; Background = Brush.Parse("#101827");
        var add = MakeButton("新建角色", NewCharacter);
        var chooseAvatar = MakeButton("选择头像", ChooseAvatarAsync);
        var save = MakeButton("保存角色", SaveAsync, true);
        var remove = MakeButton("删除角色", DeleteAsync);
        var close = MakeButton("完成", () => CloseRequested?.Invoke());
        _list.SelectionChanged += async (_, _) =>
        {
            if ((_list.SelectedItem as ListBoxItem)?.Tag is CharacterSummaryResponse item) await LoadCharacterAsync(item.Id);
        };
        _list.DoubleTapped += (_, _) => { if ((_list.SelectedItem as ListBoxItem)?.Tag is CharacterSummaryResponse item) CharacterActivated?.Invoke(item); };
        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 12, Margin = new Thickness(18), Children = { add, _list } };
        Grid.SetRow(_list, 1);
        var form = new StackPanel { Spacing = 9, Margin = new Thickness(20), Children =
        {
            new TextBlock { Text = "角色卡", FontSize = 22, FontWeight = FontWeight.SemiBold }, _name, _cardName, _userName,
            _identity, _personality, _speech, _greeting, _rules, _facts, _status,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { remove, chooseAvatar, save, close } }
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
    private static TextBox Area(string watermark) => new() { Watermark = watermark, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 62, MaxLength = 4000 };
    private static Button MakeButton(string text, Action action) { var button = new Button { Content = text }; button.Click += (_, _) => action(); return button; }
    private static Button MakeButton(string text, Func<Task> action, bool primary = false) { var button = new Button { Content = text, Background = primary ? Brush.Parse("#6C63FF") : null }; button.Click += async (_, _) => await action(); return button; }

    private async Task ReloadAsync(string? selectId = null)
    {
        try
        {
            var items = await _api.GetCharactersAsync();
            _list.ItemsSource = items.Select(item => new ListBoxItem { Tag = item, Padding = new Thickness(11), Margin = new Thickness(0,0,0,4), Content = new StackPanel { Spacing = 3, Children = { new TextBlock { Text = item.Name, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = item.CardName, FontSize = 10, Foreground = Brush.Parse("#718198") } } } }).ToArray();
            _list.SelectedItem = _list.Items.Cast<ListBoxItem>().FirstOrDefault(item => (item.Tag as CharacterSummaryResponse)?.Id == selectId) ?? _list.Items.Cast<ListBoxItem>().FirstOrDefault();
        }
        catch (Exception ex) { _status.Text = $"角色读取失败：{ex.Message}"; }
    }

    private async Task LoadCharacterAsync(string id)
    {
        try
        {
            _selected = await _api.GetCharacterAsync(id); _avatarMediaId = null;
            _name.Text = _selected.Name; _cardName.Text = _selected.CardName; _userName.Text = _selected.UserName;
            _identity.Text = _selected.Identity; _personality.Text = _selected.Personality; _speech.Text = _selected.SpeechStyle;
            _greeting.Text = _selected.Greeting; _rules.Text = string.Join(Environment.NewLine, _selected.BehavioralRules); _facts.Text = string.Join(Environment.NewLine, _selected.WorldFacts);
            _status.Text = _selected.PromptMode == CharacterPromptMode.Legacy ? "旧式角色卡：保存后转为结构化编辑" : "";
        }
        catch (Exception ex) { _status.Text = $"角色读取失败：{ex.Message}"; }
    }

    private void NewCharacter()
    {
        _selected = null; _avatarMediaId = null; _list.SelectedItem = null;
        foreach (var box in new[] { _name, _cardName, _identity, _personality, _speech, _greeting, _rules, _facts }) box.Text = "";
        _userName.Text = "你"; _status.Text = "正在创建新的角色卡"; _name.Focus();
    }

    private async Task ChooseAvatarAsync()
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider; if (storage is null) return;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选择角色头像", AllowMultiple = false, FileTypeFilter = new[] { FilePickerFileTypes.ImageAll } });
        var file = files.FirstOrDefault(); var path = file?.TryGetLocalPath(); if (file is null || path is null) return;
        try { await using var stream = File.OpenRead(path); _avatarMediaId = (await _api.UploadAsync(stream, file.Name, Mime(path))).Id; _status.Text = $"已选择头像：{file.Name}"; }
        catch (Exception ex) { _status.Text = $"头像上传失败：{ex.Message}"; }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_identity.Text) || string.IsNullOrWhiteSpace(_personality.Text) || string.IsNullOrWhiteSpace(_speech.Text)) { _status.Text = "名称、身份、性格和说话风格不能为空"; return; }
        var request = new SaveCharacterRequest(_name.Text.Trim(), _cardName.Text?.Trim() ?? _name.Text.Trim(), _userName.Text?.Trim() ?? "你",
            _identity.Text.Trim(), _personality.Text.Trim(), _speech.Text.Trim(), Lines(_rules.Text), Lines(_facts.Text), _selected?.InitialState ?? new RuntimeState(),
            _greeting.Text?.Trim() ?? "你好。", CharacterPromptMode.Structured, "", _avatarMediaId, false, _selected?.AvatarDisplay ?? new ImageDisplayPreferences());
        try
        {
            var saved = _selected is null ? await _api.CreateCharacterAsync(request) : await _api.UpdateCharacterAsync(_selected.Id, request);
            Changed = true; await ReloadAsync(saved.Id); _status.Text = "角色卡已保存";
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
}
