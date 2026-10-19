using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// 工单 <c>RT-005-MANAGE</c> 的界面面。这里不判"点下去学到什么"（那是 <c>Pry.Api.Tests</c> / <c>Pry.Core.Tests</c>
/// 的行为判据），只判形状：孤儿记忆必须在管理面单独摆出来、永久删除必须先问一句、"确认保留"是状态迁移而不是
/// 第二条删除通道、界面一个内部刻度都不许露。与 <see cref="ExpressionFeedbackSurfaceTests"/> 同一口径——
/// 扫的是源码文本，注释里出现同样字样也算。
/// </summary>
public sealed class MemoryManagementSurfaceTests
{
    private const string WindowSource = "PryMessengerMemoryWindow.cs";
    private const string InUse = "她正在用的";
    private const string Orphaned = "依据已被你删掉的";

    [Fact]
    public void Orphaned_memories_are_surfaced_through_the_scoped_details_read_path()
    {
        var source = Read("src", "Pry.App", WindowSource);
        // 唯一的读列表来源必须换到作用域感知的 details；只留 legacy 列表就等于孤儿永远看不见。
        Assert.Equal(1, Occurrences(source, "GetMemoryDetailsAsync("));
        Assert.Equal(1, Occurrences(source, "GetMemoriesAsync("));
        Assert.Contains("Status == \"orphaned\"", source, StringComparison.Ordinal);
        // 两组都得有独立标题，用户才看得见"分开"。
        Assert.Contains(InUse, source, StringComparison.Ordinal);
        Assert.Contains(Orphaned, source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_delete_button_confirms_before_the_irreversible_call()
    {
        var source = Read("src", "Pry.App", WindowSource);
        // 只有一个删除调用点，而且它排在确认之后、排在"没点确定就 return"之后。
        Assert.Equal(1, Occurrences(source, "await _api.DeleteMemoryAsync("));
        var confirmAt = source.IndexOf("PryMessengerConfirmDialog.ShowAsync(", StringComparison.Ordinal);
        var guardAt = source.IndexOf("if (result != ConfirmDeleteId) return;", StringComparison.Ordinal);
        var deleteAt = source.IndexOf("await _api.DeleteMemoryAsync(", StringComparison.Ordinal);
        Assert.True(confirmAt >= 0 && guardAt > confirmAt && deleteAt > guardAt,
            "删除没有先经确认门，或者确认判定排在了删除之后。");
        // 文案必须说清"此后不会再重新学到"，而不是"删除记录"这种可逆暗示。
        Assert.Contains("不会再重新学到这句话", source, StringComparison.Ordinal);
        // 破坏性动作要用危险样式，并且按 id 判定，而不是靠"点了任何按钮都算"。
        Assert.Contains("Destructive: true", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_keep_is_a_state_transition_and_not_a_second_delete_channel()
    {
        var source = Read("src", "Pry.App", WindowSource);
        Assert.Equal(1, Occurrences(source, "await _api.ConfirmKeepMemoryAsync("));
        // 确认保留走的是被删依据那一条的 id，且与删除调用是两条独立路径（不同端点、不同动词）。
        Assert.Contains("ConfirmKeepMemoryAsync(orphan.Id", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfirmKeepMemoryAsync(_selected", source, StringComparison.Ordinal);
    }

    [Fact]
    public void No_internal_scale_leaks_into_the_memory_page()
    {
        var source = Read("src", "Pry.App", WindowSource);
        // confidence / evidence 是记忆自己的数值，只作筛选依据，不作展示卖点（工单 §3.8）。
        foreach (var forbidden in new[] { "Confidence", "Evidence" })
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_scanners_see_the_shapes_they_claim_to_see()
    {
        // 每个"要守/要禁"的形状先喂一段含它的样本文本，确认计数与包含判定真的认得它。
        Assert.Equal(1, Occurrences("try { await api.DeleteMemoryAsync(id, c); }", "api.DeleteMemoryAsync("));
        Assert.Equal(2, Occurrences("x.Confidence\ny.Evidence", "Confidence") + Occurrences("x.Confidence\ny.Evidence", "Evidence"));
        Assert.Equal(1, Occurrences("if (result != ConfirmDeleteId) return;\n", "if (result != ConfirmDeleteId) return;"));
        Assert.Equal(2, Occurrences($"// {InUse}\nvar a = \"{InUse}\";", InUse));
        Assert.Contains("Destructive: true", "Button b = new(Destructive: true);", StringComparison.Ordinal);
    }

    private static int Occurrences(string text, string token)
    {
        var count = 0;
        for (var index = text.IndexOf(token, StringComparison.Ordinal); index >= 0;
             index = text.IndexOf(token, index + 1, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([RepositoryRoot(), .. parts]));

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
