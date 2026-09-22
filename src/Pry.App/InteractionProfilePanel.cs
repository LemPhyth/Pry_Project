using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.App.Services;

namespace Pry.App;

internal sealed class InteractionProfilePanel : UserControl
{
    private readonly PryBackendClient _api;
    private readonly string _characterId;
    private readonly StackPanel _actions = new() { Spacing = 24 }, _poke = new() { Spacing = 24 };
    private readonly NumericUpDown _cooldown = new() { Minimum = 0 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private int _schemaVersion = 1;
    private bool _loaded;
    public InteractionProfilePanel(PryBackendClient api, string characterId)
    {
        _api = api; _characterId = characterId;
        var form = new StackPanel { Spacing = 16 };
        form.Children.Add(PryUi.Heading("互动配置"));
        form.Children.Add(PryUi.Hint("为当前角色设置动作与回应。此处独立保存，内置角色的修改作为用户覆盖保留。"));
        form.Children.Add(PryUi.Field("互动冷却（秒）", _cooldown));
        form.Children.Add(PryUi.Heading("戳一戳回复")); form.Children.Add(_poke);
        form.Children.Add(Button("添加戳一戳回复", () => AddReply(_poke, null)));
        form.Children.Add(PryUi.Heading("其他动作"));
        form.Children.Add(_actions); form.Children.Add(Button("添加动作", () => AddAction(null)));
        var save = new Button { Content = "保存互动配置", HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += async (_, _) =>
        {
            if (!_loaded) return;
            form.IsEnabled = false;
            try
            {
                var request = new SaveInteractionProfileRequest(_schemaVersion, (int)(_cooldown.Value ?? 0), Replies(_poke),
                    _actions.Children.Cast<Border>().Select(x => ((ActionDraft)x.Tag!).Value()).ToArray());
                var response = await _api.SaveInteractionProfileAsync(_characterId, request);
                Display(response); _status.Text = "互动配置已保存";
            }
            catch (Exception ex) { _status.Text = $"保存失败，编辑内容已保留：{ex.Message}"; }
            finally { form.IsEnabled = true; }
        };
        var retry = new Button { Content = "重试读取", IsVisible = false };
        _status.Foreground = PryVisualTheme.Get("Muted"); _status.FontSize = 13;
        form.Children.Add(save); form.Children.Add(_status); form.Children.Add(retry); Content = PryUi.Section(form);
        async Task LoadAsync()
        {
            if (_loaded) return;
            form.IsEnabled = false;
            try { Display(await _api.GetInteractionProfileAsync(_characterId)); _loaded = true; retry.IsVisible = false; _status.Text = ""; }
            catch (Exception ex) { _status.Text = $"互动配置读取失败：{ex.Message}"; retry.IsVisible = true; }
            finally { form.IsEnabled = true; save.IsEnabled = _loaded; }
        }
        AttachedToVisualTree += async (_, _) => await LoadAsync();
        retry.Click += async (_, _) => await LoadAsync();
    }
    private static Button Button(string text, Action action)
    { var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Left }; if (text.StartsWith("移除")) button.Classes.Add("danger"); button.Click += (_, _) => action(); return button; }
    private void Display(InteractionProfileResponse profile)
    {
        _schemaVersion = profile.SchemaVersion; _cooldown.Value = profile.CooldownSeconds;
        _poke.Children.Clear(); _actions.Children.Clear();
        foreach (var reply in profile.PokeReplies) AddReply(_poke, reply);
        foreach (var action in profile.Actions) AddAction(action);
    }
    private static void AddReply(StackPanel list, InteractionReplyPresetDto? reply)
    {
        var draft = new ReplyDraft(reply?.Id ?? Guid.NewGuid().ToString("N"),
            new TextBox { Text = reply?.Text, Watermark = "回复正文", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 60 },
            new CheckBox { Content = "启用", IsChecked = reply?.Enabled ?? true }, new NumericUpDown { Value = (decimal)(reply?.Weight ?? 1) });
        var row = new StackPanel { Tag = draft, Spacing = 8 };
        row.Children.Add(PryUi.Field("回复正文", draft.Text));
        row.Children.Add(PryUi.Field("随机权重", draft.Weight));
        row.Children.Add(PryUi.Actions(draft.Enabled, Button("上移", () => Move(list, row, -1)), Button("下移", () => Move(list, row, 1)), Button("移除预设", () => list.Children.Remove(row))));
        list.Children.Add(row);
    }
    private static void Move(StackPanel parent, Control item, int delta)
    {
        var at = parent.Children.IndexOf(item); var next = at + delta;
        if (next < 0 || next >= parent.Children.Count) return;
        parent.Children.RemoveAt(at); parent.Children.Insert(next, item);
    }
    private static InteractionReplyPresetDto[] Replies(StackPanel list) => list.Children.Cast<StackPanel>().Select(x =>
    { var draft = (ReplyDraft)x.Tag!; return new InteractionReplyPresetDto(draft.Id, draft.Text.Text ?? "", draft.Enabled.IsChecked == true, (double)(draft.Weight.Value ?? 1)); }).ToArray();
    private void AddAction(InteractionActionDto? action)
    {
        var replies = new StackPanel { Spacing = 8 };
        foreach (var reply in action?.Replies ?? []) AddReply(replies, reply);
        var draft = new ActionDraft(action?.Id ?? Guid.NewGuid().ToString("N"), new TextBox { Text = action?.DisplayName },
            new CheckBox { Content = "启用动作", IsChecked = action?.Enabled ?? true },
            new ComboBox { ItemsSource = new[] { "预设回复", "仅动作，无文字回复" }, SelectedIndex = action?.ResponseMode == "silent" ? 1 : 0 },
            action?.ExpressionIntent, replies);
        var form = new StackPanel { Spacing = 16 }; var row = new Border { Tag = draft, Padding = new Thickness(0, 16, 0, 0),
            BorderBrush = PryVisualTheme.Get("Border"), BorderThickness = new Thickness(0, 1, 0, 0), Child = form };
        form.Children.Add(PryUi.Field("动作名称", draft.Name)); form.Children.Add(draft.Enabled); form.Children.Add(PryUi.Field("回应方式", draft.Mode));
        form.Children.Add(replies); form.Children.Add(Button("添加回复", () => AddReply(replies, null)));
        form.Children.Add(PryUi.Actions(Button("上移", () => Move(_actions, row, -1)), Button("下移", () => Move(_actions, row, 1)), Button("移除动作", () => _actions.Children.Remove(row))));
        _actions.Children.Add(row);
    }
    private sealed record ReplyDraft(string Id, TextBox Text, CheckBox Enabled, NumericUpDown Weight);
    private sealed record ActionDraft(string Id, TextBox Name, CheckBox Enabled, ComboBox Mode, string? Expression, StackPanel RepliesPanel)
    {
        public InteractionActionDto Value() => new(Id, Name.Text ?? "", Enabled.IsChecked == true, Mode.SelectedIndex == 1 ? "silent" : "preset", Expression, Replies(RepliesPanel));
    }
}
