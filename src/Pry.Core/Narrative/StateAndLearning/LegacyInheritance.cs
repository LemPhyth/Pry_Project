// HDS-009「状态与显式学习迁移」第一片·隔离原型：旧模式单向隔离＋数据继承的最小来源约束（工单 §一.5）。
//
// 从旧状态形状"继承"进来的输入，用本文件新建的记录类型表达，逐条带来源引用与版本；
// 来源缺失即明确失败、静默丢弃算缺陷——每一条输入要么落一行、要么留下一条命名读数，没有"当没有这行"的出口。
// 反向（新链写回旧链那一层）不允许存在：由一条"只有单向"的判定守着，且这条判定能被翻向（翻向即红）。
// 本原型不引用旧链那一层的任何类型，也不复制上一把的原型类型。
namespace Pry.Core.Narrative.StateAndLearning;

/// <summary>
/// 一条从旧状态形状继承进来的输入：用新建的记录类型表达，逐条带来源指认。
/// <see cref="LegacyShape"/> 只是随身的形状标签（调用方给什么带什么），本原型不按它设门。
/// </summary>
public sealed record InheritedStateInput(
    string StateKey,
    string Content,
    string LegacyShape,
    LayerSourceReference? Source);

/// <summary>继承读数：结局（导入／失败／拒绝）＋明确原因。</summary>
public sealed record InheritanceReadout(string StateKey, string Outcome, string Reason);

/// <summary>一条真正继承进来的行：带着来源标识与来源版本。</summary>
public sealed record InheritedRow(
    string StateKey,
    string Content,
    string LegacyShape,
    string SourceId,
    long SourceVersion);

/// <summary>
/// 单向隔离原型：正向继承要求逐条来源可指认，反向写回那一层永远不被允许。
/// </summary>
public sealed class LegacyInheritance
{
    private readonly List<InheritedRow> _imported = [];
    private readonly List<string> _legacyWrites = [];

    /// <summary>继承进来的行（正向），按导入顺序。</summary>
    public IReadOnlyList<InheritedRow> Imported => _imported;

    /// <summary>写回旧链那一层的痕迹——本原型里应当恒空；单向判定被翻向时这里才会出现内容。</summary>
    public IReadOnlyList<string> LegacyWrites => _legacyWrites;

    /// <summary>
    /// 正向继承。来源缺失（标识空 或 版本不在场）即明确失败，绝不静默丢弃：
    /// 每条输入都会留下一条命名读数。摘掉来源闸这一行 → 只有"继承缺来源即失败"那格会红。
    /// </summary>
    public InheritanceReadout Import(InheritedStateInput input)
    {
        if (input.Source is null
            || string.IsNullOrWhiteSpace(input.Source.SourceId)
            || input.Source.SourceVersion is null)
            return new InheritanceReadout(input.StateKey, "Failed", "inherited_source_missing");

        _imported.Add(new InheritedRow(
            input.StateKey, input.Content, input.LegacyShape,
            input.Source.SourceId, input.Source.SourceVersion.Value));
        return new InheritanceReadout(input.StateKey, "Imported", "inherited_with_provenance");
    }

    /// <summary>
    /// 反向写回：新链产出写回旧链那一层。单向隔离的判定行把它拒掉；把该判定翻向（false→true）
    /// 就会落到旧链那一层，"没有第二条写入路径"那格当场报警——这一格因此是可红的，不是红不了的结论。
    /// </summary>
    public InheritanceReadout AttemptWriteback(InheritedStateInput writeback)
    {
        if (IsLegacyWritebackAllowed())
        {
            _legacyWrites.Add(writeback.StateKey);
            return new InheritanceReadout(writeback.StateKey, "Written", "legacy_writeback_applied");
        }

        return new InheritanceReadout(writeback.StateKey, "Refused", "one_way_isolation_only");
    }

    // 单向隔离的判定：新链不写回旧链那一层，恒为假。翻向这一行就是"第二条写入路径出现"的时刻。
    private static bool IsLegacyWritebackAllowed() => false;
}
