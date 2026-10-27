// HDS-007 第一片·隔离原型：到期意图登记处与长离线恢复（工单 §一.4 的补投面）。
//
// 时钟由调用方给：长离线恢复就是"拿一个前跳后的时刻来收一次到期意图"。补投按登记顺序
// （RegisteredSequence 小→大）逐条点名，一条已补投的意图不会被第二次补投，
// 也一条不会因为没到点就从清单上消失——没到点的仍留在 StillPending 里可观察。
//
// 全内存、参数全走入参、零生产调用方；登记序号由调用方给，本原型不自行发号。
namespace Pry.Core.Narrative.Advancement;

/// <summary>一条到期意图的登记材料：什么时候到期、按什么顺序登记的。</summary>
public sealed record DueIntentRegistration(string IntentKey, DateTime DueUtc, long RegisteredSequence);

/// <summary>一次补投里被点名的那条意图：它什么时候到期、什么时候被这次恢复带走。</summary>
public sealed record RecoveredDueIntent(
    string IntentKey,
    DateTime DueUtc,
    long RegisteredSequence,
    DateTime RecoveredAtUtc);

/// <summary>
/// 一次时钟前跳之后的恢复读数：补投了哪几条（按登记顺序）、还剩哪几条没到点。
/// 两个清单合起来覆盖全部已登记意图，不存在"少了哪条"的沉默出口。
/// </summary>
public sealed record DueIntentRecoveryPlan(
    DateTime JumpedToUtc,
    IReadOnlyList<RecoveredDueIntent> Replayed,
    IReadOnlyList<DueIntentRegistration> StillPending);

/// <summary>
/// 到期意图登记处。它只回答两件事：这个时刻之前有哪些意图到期、它们按什么顺序补投过。
/// 补投是"投递一次"而不是"删除"——已补投的意图仍在登记处可数。
/// </summary>
public sealed class DueIntentRegistry
{
    private sealed class Entry(DueIntentRegistration registration)
    {
        public DueIntentRegistration Registration { get; } = registration;
        public bool Replayed { get; set; }
    }

    private readonly List<Entry> _entries = [];

    /// <summary>全部已登记意图（含已补投的），按登记顺序。</summary>
    public IReadOnlyList<DueIntentRegistration> Registrations =>
        _entries.OrderBy(x => x.Registration.RegisteredSequence).Select(x => x.Registration).ToList();

    /// <summary>登记一条到期意图。同一个意图键不得登记两次。</summary>
    public DueIntentRegistry Register(DueIntentRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        if (string.IsNullOrWhiteSpace(registration.IntentKey))
            throw new ArgumentException("A due intent must carry a non-empty intent key.", nameof(registration));
        if (_entries.Any(x => x.Registration.IntentKey == registration.IntentKey))
            throw new ArgumentException(
                $"意图 {registration.IntentKey} 已经登记过，重复登记会被拒而不是静默合并。",
                nameof(registration));
        _entries.Add(new Entry(registration));
        return this;
    }

    /// <summary>
    /// 按前跳后的时钟收一次到期意图：<see cref="DueIntentRegistration.DueUtc"/> 不晚于该时刻的、
    /// 且此前没被补投过的，按登记顺序逐条带走；同一次调用里既补投也标已补投。
    /// </summary>
    public DueIntentRecoveryPlan RecoverAfterClockJump(DateTime jumpedToUtc)
    {
        var due = _entries
            .Where(x => x.Replayed is false && x.Registration.DueUtc <= jumpedToUtc)
            .OrderBy(x => x.Registration.RegisteredSequence)
            .ToList();
        foreach (var entry in due)
        {
            entry.Replayed = true;
        }

        var replayed = due
            .Select(x => new RecoveredDueIntent(x.Registration.IntentKey, x.Registration.DueUtc,
                x.Registration.RegisteredSequence, jumpedToUtc))
            .ToList();
        var stillPending = _entries
            .Where(x => x.Replayed is false)
            .OrderBy(x => x.Registration.RegisteredSequence)
            .Select(x => x.Registration)
            .ToList();
        return new DueIntentRecoveryPlan(jumpedToUtc, replayed, stillPending);
    }
}
