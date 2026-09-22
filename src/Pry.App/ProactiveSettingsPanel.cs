using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.App.Services;

namespace Pry.App;

internal sealed class ProactiveSettingsPanel : UserControl
{
    private readonly PryBackendClient _api;
    private readonly string _character, _room;
    private ProactivePolicyResponse? _policy;
    private readonly CheckBox _enabled = new() { Content = "允许角色在空闲时主动找我聊天" };
    private readonly CheckBox _quiet = new() { Content = "启用安静时段" };
    private readonly NumericUpDown _idle = new(), _interval = new(), _limit = new();
    private readonly TextBox _zone = new(), _start = new(), _end = new();
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    public ProactiveSettingsPanel(PryBackendClient api, string character, string room)
    {
        _api = api; _character = character; _room = room;
        var form = new StackPanel { Spacing = 24, Margin = new Thickness(24), MaxWidth = 800 };
        var contact = new StackPanel { Spacing = 16, Children = {
            PryUi.Heading("联系节奏"), PryUi.Hint("设置应用于当前角色及会话。角色会在你空闲时，使用已加载的聊天模型发起话题。"),
            _enabled, PryUi.Field("空闲多久后可以联系（秒）", _idle),
            PryUi.Field("两次尝试的最短间隔（秒）", _interval), PryUi.Field("每日最多消息数", _limit) } };
        var quiet = new StackPanel { Spacing = 16, Children = { PryUi.Heading("安静时段"), _quiet, PryUi.Field("时区", _zone) } };
        var times = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 16 };
        times.Children.Add(PryUi.Field("开始（HH:mm）", _start));
        var end = PryUi.Field("结束（HH:mm）", _end); Grid.SetColumn(end, 1); times.Children.Add(end);
        quiet.Children.Add(times);
        form.Children.Add(PryUi.Section(contact)); form.Children.Add(PryUi.Section(quiet));
        var save = new Button { Content = "保存主动话题设置", Classes = { "primary" } };
        var reload = new Button { Content = "重新读取策略与状态" };
        reload.Click += async (_, _) => await LoadAsync();
        save.Click += async (_, _) =>
        {
            if (_policy is null) return;
            if (!TimeOnly.TryParseExact(_start.Text, "HH:mm", out var start) || !TimeOnly.TryParseExact(_end.Text, "HH:mm", out var end))
            { _status.Text = "请使用 HH:mm 输入安静时段"; return; }
            save.IsEnabled = false;
            try
            {
                _policy = await _api.SaveProactivePolicyAsync(_character, new SaveProactivePolicyRequest(_policy.Revision, _room,
                    _enabled.IsChecked == true, (int)(_idle.Value ?? 0), (int)(_interval.Value ?? 0), (int)(_limit.Value ?? 0),
                    _zone.Text ?? "", start.Hour * 60 + start.Minute, end.Hour * 60 + end.Minute, _quiet.IsChecked == true));
                _status.Text = "主动话题设置已保存";
            }
            catch (Exception ex) { _status.Text = $"保存失败：{ex.Message}。若配置版本冲突，请重新读取后确认修改。"; }
            finally { save.IsEnabled = true; }
        };
        _status.Foreground = PryVisualTheme.Get("Muted"); _status.FontSize = 13;
        var actions = PryUi.Actions(reload, save); actions.HorizontalAlignment = HorizontalAlignment.Right;
        var footer = PryUi.Footer(new StackPanel { Spacing = 8, Children = { _status, actions } });
        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { new ScrollViewer { Content = form }, footer } };
        Grid.SetRow(footer, 1); Content = root;
        AttachedToVisualTree += async (_, _) => await LoadAsync();
    }
    private async Task LoadAsync()
    {
        try
        {
            _policy = await _api.GetProactivePolicyAsync(_character);
            _enabled.IsChecked = _policy.Enabled; _enabled.IsEnabled = _policy.DeliveryAvailable;
            _idle.Value = _policy.IdleSeconds; _interval.Value = _policy.MinimumIntervalSeconds; _limit.Value = _policy.DailyLimit;
            _quiet.IsChecked = _policy.QuietHoursEnabled; _zone.Text = _policy.TimeZoneId;
            _start.Text = $"{_policy.QuietStartMinute / 60:00}:{_policy.QuietStartMinute % 60:00}";
            _end.Text = $"{_policy.QuietEndMinute / 60:00}:{_policy.QuietEndMinute % 60:00}";
            var state = await _api.GetProactiveStatusAsync(_character);
            _status.Text = $"今日已发送 {state.SentToday} 条。" + (state.State switch
            {
                "disabled" => "主动话题未开启", "model_not_loaded" => "请先正常聊天以加载模型", "generating" => "角色正在准备话题",
                "awaiting_user_reply" => "等待你回复后再联系", "quiet_hours" => "现在处于安静时段",
                "activity_stale" => "等待应用同步活动状态", "user_busy" => "正在等待你空闲", "daily_limit" => "今日已达到上限",
                "target_unavailable" => "目标会话不可用", "minimum_interval" => "等待联系间隔", "idle_threshold" => "等待空闲时长",
                "eligible" => "条件满足，等待调度", _ => "等待状态更新"
            });
        }
        catch (Exception ex) { _status.Text = $"读取失败：{ex.Message}"; }
    }
}
