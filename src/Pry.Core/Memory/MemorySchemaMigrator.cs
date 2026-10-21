using Microsoft.Data.Sqlite;

namespace Pry.Core.Memory;

/// <summary>迁移步骤的执行档位；档位决定"跑不跑"与"事务由谁持有"。</summary>
public enum SchemaMigrationMode
{
    /// <summary>结构性自愈（历史机制：<c>CREATE … IF NOT EXISTS</c>）。每次初始化都跑，必须幂等，
    /// 在迁移器的事务边界内执行。</summary>
    SelfHealingBaseline,

    /// <summary>按版本闸门的一次性升级：仅当登记版本仍低于 <see cref="SchemaMigrationStep.TargetVersion"/> 时执行。
    /// 与语句同批的版本推进写在本步骤事务里，失败即整体回滚（补上 PLAT-003 P2 指出的"两句串跑、无事务"形状）。</summary>
    VersionGatedUpgrade,

    /// <summary>领域表的幂等修复通道（主动策略、读水位、Runtime、协议事件、学习状态）。每次初始化都跑，
    /// 必须幂等，在迁移器的事务边界内执行。</summary>
    IdempotentRepair,

    /// <summary>同上的修复通道，但实现自带连接与事务（长期记忆仓储）。迁移器不再包外层事务——
    /// 外层持写锁时内层第二条连接会自锁；原子性由该步骤自己的实现负责。</summary>
    IdempotentRepairOwnsConnection,

    /// <summary>收尾的版本对齐：把登记版本推进到当前二进制的 <see cref="MemoryDatabase.SchemaVersion"/>，
    /// 只在同一事务里跟随成功步骤生效。必须是清单的最后一步。</summary>
    VersionAlign,
}

/// <summary>
/// 迁移登记清单里的一条（HDS-001 F 条）。登记要求：
/// <list type="number">
///   <item><description>唯一的 <see cref="Id"/> 与人话 <see cref="Name"/>；</description></item>
///   <item><description><see cref="TargetVersion"/> 不得超过当前二进制的 <see cref="MemoryDatabase.SchemaVersion"/>——
///   本机制不冒领新版本号（v4 §9.1 明令），预占版本属于冒领；</description></item>
///   <item><description>幂等：同一数据库上重跑不改变已达成状态；</description></item>
///   <item><description>事务边界：要么整步生效、要么整步消失，失败不留半套结构。</description></item>
/// </list>
/// </summary>
/// <param name="TargetVersion">该步骤产出的结构版本；闸门档位与不变量核对都读这个数。</param>
/// <param name="Id">登记号，稳定、唯一、点分命名。</param>
/// <param name="Name">失败信息里呈现给用户的步骤名。</param>
/// <param name="Mode">执行档位。</param>
/// <param name="Apply">步骤本体。上下文给出共享连接与外层事务（如适用），实现必须把命令挂到
/// <see cref="SchemaMigrationContext.Transaction"/> 上，否则自己的语句不在回滚范围内。</param>
public sealed record SchemaMigrationStep(
    int TargetVersion,
    string Id,
    string Name,
    SchemaMigrationMode Mode,
    Func<SchemaMigrationContext, CancellationToken, Task> Apply)
{
    /// <summary>该步骤是否需要迁移器提供外层事务边界。</summary>
    public bool UsesAmbientTransaction => Mode != SchemaMigrationMode.IdempotentRepairOwnsConnection;
}

/// <summary>步骤执行上下文：迁移器持有的连接，以及（如适用）正在执行该步骤的事务。</summary>
public sealed record SchemaMigrationContext(SqliteConnection Connection, SqliteTransaction? Transaction);

/// <summary>
/// 迁移执行器：按登记清单依次执行，每步一个事务边界；任何一步失败——回滚该步、
/// 版本不推进、以 <see cref="MemoryMigrationException"/> 停下，绝不允许"跳过坏步骤继续往后写"。
/// 因为全部步骤要求幂等，修好成因后重跑初始化就是完整的恢复路径（NR-23）。
/// </summary>
public static class MemorySchemaMigrator
{
    public static async Task RunAsync(SqliteConnection connection, int versionBefore,
        IReadOnlyList<SchemaMigrationStep> steps, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(steps);
        ValidateRegistry(steps);
        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (step.Mode == SchemaMigrationMode.VersionGatedUpgrade && versionBefore >= step.TargetVersion)
                continue; // 已按旧路径吃进过这一步；跳过是闸门档的语义，不是漏跑。

            SqliteTransaction? transaction = null;
            try
            {
                if (step.UsesAmbientTransaction)
                    transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
                await step.Apply(new SchemaMigrationContext(connection, transaction), cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                if (transaction is not null)
                {
                    try { await transaction.RollbackAsync(CancellationToken.None); }
                    catch (SqliteException) { /* 连接已断时回滚是空操作；事务句柄释放即可。 */ }
                    await transaction.DisposeAsync();
                }
                if (error is MemoryMigrationException or SchemaVersionTooNewException) throw;
                throw new MemoryMigrationException(step.Id, step.Name, error);
            }
            finally
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }
        }
    }

    /// <summary>登记清单不变量：任何时候读到不合规清单都是代码缺陷，必须在执行前炸给开发者。</summary>
    public static void ValidateRegistry(IReadOnlyList<SchemaMigrationStep> steps)
    {
        if (steps.Count == 0)
            throw new InvalidOperationException("迁移登记清单为空：初始化没有可执行的结构步骤。");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (!ids.Add(step.Id))
                throw new InvalidOperationException($"迁移登记号重复：{step.Id}。");
            if (step.TargetVersion < 1)
                throw new InvalidOperationException($"迁移步骤 {step.Id} 的目标版本 {step.TargetVersion} 非法（必须 ≥ 1）。");
            if (step.TargetVersion > MemoryDatabase.SchemaVersion)
                throw new InvalidOperationException(
                    $"迁移步骤 {step.Id} 预占了未来版本 {step.TargetVersion}（当前二进制登记 {MemoryDatabase.SchemaVersion}）；" +
                    "版本分配权在持久层任务，不得冒领（v4 §9.1）。");
        }
        if (steps[^1].Mode != SchemaMigrationMode.VersionAlign)
            throw new InvalidOperationException("迁移清单必须以版本对齐步骤收尾，否则升级路径不会推进登记版本。");
        if (steps.Count(s => s.Mode == SchemaMigrationMode.VersionAlign) > 1)
            throw new InvalidOperationException("迁移清单只允许一个版本对齐步骤。");
    }
}
