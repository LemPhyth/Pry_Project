namespace Pry.Core.Backup;

/// <summary>恢复核对清单里的一格：备份时记的期望值、隔离目录里实测的值、是否一致。</summary>
public sealed record RestoreCheckItem(string Name, string Expected, string Actual, bool Passed);

/// <summary>
/// 恢复核对报告（v4 §9.3 "核对行数、稳定 ID、引用、索引、外键、墓碑、孤立态及偏好"的执行记录）。
/// 每一格独立成项——汇总一个"通过"没有审计价值，差异必须能指到具体那一格。
/// </summary>
public sealed record RestoreVerificationReport(IReadOnlyList<RestoreCheckItem> Items)
{
    public bool AllPassed => Items.All(item => item.Passed);

    public IReadOnlyList<RestoreCheckItem> Failures => Items.Where(item => !item.Passed).ToList();

    public RestoreCheckItem Item(string name) => Items.FirstOrDefault(item => item.Name == name)
        ?? throw new ArgumentException($"核对清单里没有这一格：{name}", nameof(name));
}

/// <summary>一次恢复的产出：落点目录与核对报告。</summary>
public sealed record RestoreResult(string TargetDataDirectory, RestoreVerificationReport Verification);
