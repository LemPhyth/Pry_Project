// HDS-009「状态与显式学习迁移」第一片·隔离原型判据（工单 HDS-WAVE33-1028 §二）。
//
// 逐条对上工单 §一 的五条，每条至少一枚独立可判的格（下面的"枚"＝展开后的节点数；
// 注释里刻意不写方括号属性名，以免顶账时 grep 把注释行算成格）：
//   §一.1 Canon/Overlay 分层  ＝ 5 枚 Fact（叙事不得改写 Canon、作者侧带来源可改、作者侧缺来源被拒、
//                                     Overlay 缺来源与版本被拒、Overlay 带来源与版本入册且不碰 Canon）
//   §一.2 叙事侧三道授权闸    ＝ 4 枚 Fact（亲密闸／作者边界闸／用户情绪闸各一枚独立命名拒因＝区分性探针，
//                                     三面全给时被第一道拦下）
//   §一.3 投递后与点击归因    ＝ 4 枚 Fact ＋ 1 枚 Theory 展开 3 条（未投递不得归因、投递但被抑制/去重/拒绝
//                                     不得归因（三行）、投递+点击才入册、投递但缺点击不入册、
//                                     "我明天告诉你"只提出承诺不证明同意也不证明完成）
//   §一.4 经验只作软材料      ＝ 4 枚 Fact（缺有效期入参即明确失败、窗口内作可选材料、超期不作材料、
//                                     经验从不带说话裁决权）＋ 2 枚源码扫描格（旧链类型名、无源码侧数字默认值，各带阳性探针）
//   §一.5 单向隔离＋最小来源  ＝ 4 枚 Fact（继承缺来源即失败、每条输入都留命名读数不静默丢弃、
//                                     成功继承带来源与版本、没有写回旧链的第二条路径）
//
// 断言只钉绝对字面量或"当场算出再与字面量比"的形状；没有"两条路径同构造所以恒等"那种覆盖证据。
// 本文件不改任何既有判据、不改上游门、不用 Assert.Skip、不注释既有断言、不往任何既有测试文件加行。
using Pry.Core.Narrative.StateAndLearning;
using Xunit;

namespace Pry.Core.Tests;

public sealed class StateAndLearningPrototypeTests
{
    private static readonly DateTime Base = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static readonly string[] LegacyChainNames =
    [
        "BehaviorPlanner", "ReplyGenerationRenderer", "CharacterStateService", "RelationshipState",
        "MoodState", "ExpressionLearner", "LearningOutcome",
    ];

    private static readonly string[] PrototypeFiles =
    [
        "StateLayering.cs", "NarrativeAuthority.cs", "DeliveryAttribution.cs",
        "ExperienceMaterial.cs", "LegacyInheritance.cs",
    ];

    // ──────────────────────── §一.1 Canon 与 Overlay 分层 ────────────────────────

    [Fact]
    public void A_narrative_output_may_not_rewrite_the_stable_canon_layer()
    {
        var layers = new StateLayering();

        var readout = layers.ReviseCanon(new CanonRevisionRequest(
            "canon-1", "rewritten by narrator", true, new LayerSourceReference("src-1", 3)));

        Assert.Equal("Rejected", readout.Outcome);
        Assert.Equal("canon_immune_to_narrative_output", readout.Reason);
        Assert.False(layers.HasCanon("canon-1"));
    }

    [Fact]
    public void An_author_side_canon_revision_with_a_pointable_source_is_applied()
    {
        var layers = new StateLayering();

        var readout = layers.ReviseCanon(new CanonRevisionRequest(
            "canon-1", "author settled", false, new LayerSourceReference("src-author", 9)));

        Assert.Equal("Applied", readout.Outcome);
        Assert.Equal("canon_revised_by_authority", readout.Reason);
        Assert.Equal("author settled", layers.CanonContent("canon-1"));
    }

    [Fact]
    public void An_author_side_canon_revision_without_a_source_is_refused_with_its_own_reason()
    {
        var layers = new StateLayering();

        var nullSource = layers.ReviseCanon(new CanonRevisionRequest(
            "canon-2", "no source", false, null));
        var blankSource = layers.ReviseCanon(new CanonRevisionRequest(
            "canon-2", "blank source", false, new LayerSourceReference("   ", 1)));

        Assert.Equal("Rejected", nullSource.Outcome);
        Assert.Equal("canon_source_unattributable", nullSource.Reason);
        Assert.Equal("Rejected", blankSource.Outcome);
        Assert.Equal("canon_source_unattributable", blankSource.Reason);
        Assert.False(layers.HasCanon("canon-2"));
    }

    [Fact]
    public void An_overlay_without_a_pointable_source_and_version_is_refused()
    {
        var layers = new StateLayering();

        var nullSource = layers.WriteOverlay(new OverlayDraft("mood-1", "shifted", "mood", null));
        var nullVersion = layers.WriteOverlay(new OverlayDraft(
            "mood-1", "shifted", "mood", new LayerSourceReference("src-1", null)));

        Assert.Equal("Rejected", nullSource.Outcome);
        Assert.Equal("overlay_requires_pointable_source_and_version", nullSource.Reason);
        Assert.Equal("Rejected", nullVersion.Outcome);
        Assert.Equal("overlay_requires_pointable_source_and_version", nullVersion.Reason);
        Assert.Empty(layers.Overlays);
    }

    [Fact]
    public void An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched()
    {
        var layers = new StateLayering();
        layers.ReviseCanon(new CanonRevisionRequest("canon-1", "settled", false,
            new LayerSourceReference("src-author", 1)));

        var readout = layers.WriteOverlay(new OverlayDraft(
            "rel-1", "closer today", "relationship", new LayerSourceReference("src-1", 4)));

        Assert.Equal("Applied", readout.Outcome);
        Assert.Equal("overlay_recorded", readout.Reason);
        Assert.Equal(1L, readout.OverlaySequence!.Value);
        var overlay = Assert.Single(layers.Overlays);
        Assert.Equal("src-1", overlay.SourceId);
        Assert.Equal(4, overlay.SourceVersion);
        Assert.Equal("relationship", overlay.ChangeDomain);
        // 落 Overlay 不改稳定层：Canon 仍是作者那一条。
        Assert.Equal("settled", layers.CanonContent("canon-1"));
    }

    // ─────────────────────── §一.2 叙事侧不能直接写三类授权面 ───────────────────────

    [Fact]
    public void A_narrative_candidate_may_not_write_intimacy_state()
    {
        var authority = new NarrativeAuthority();

        var readout = authority.Assess(new NarrativeWriteAttempt("c-1", true, false, false));

        Assert.False(readout.Allowed);
        Assert.Equal("intimacy_state_is_not_narrator_writable", readout.Reason);
    }

    [Fact]
    public void A_narrative_candidate_may_not_modify_the_author_boundary()
    {
        var authority = new NarrativeAuthority();

        var readout = authority.Assess(new NarrativeWriteAttempt("c-2", false, true, false));

        Assert.False(readout.Allowed);
        Assert.Equal("author_boundary_is_not_narrator_mutable", readout.Reason);
    }

    [Fact]
    public void A_narrative_candidate_may_not_declare_the_users_emotion()
    {
        var authority = new NarrativeAuthority();

        var readout = authority.Assess(new NarrativeWriteAttempt("c-3", false, false, true));

        Assert.False(readout.Allowed);
        Assert.Equal("user_emotion_is_not_narrator_assertable", readout.Reason);
    }

    // 区分性探针的另一半：三面全给时被第一道拦下，证明这是三道独立闸（各自的单面格给出三个不同拒因），
    // 不是同一句 if 的三种措辞。
    [Fact]
    public void A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate()
    {
        var authority = new NarrativeAuthority();

        var readout = authority.Assess(new NarrativeWriteAttempt("c-4", true, true, true));

        Assert.False(readout.Allowed);
        Assert.Equal("intimacy_state_is_not_narrator_writable", readout.Reason);
    }

    // ─────────────────────── §一.3 投递后与点击证据归因 ───────────────────────

    [Fact]
    public void An_undelivered_body_is_not_attribution_evidence()
    {
        var attribution = new DeliveryAttribution();

        var readout = attribution.Admit(new ExperienceCandidateEvidence(
            "body-1", BodyDelivered: false, BodySuppressed: false, BodyDeduplicated: false,
            BodyRejected: false, HasExplicitClickOrFeedback: true));

        Assert.False(readout.Admitted);
        Assert.Equal("body_never_delivered", readout.Reason);
        Assert.Empty(attribution.Registry);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(
        bool suppressed, bool deduplicated, bool rejected)
    {
        var attribution = new DeliveryAttribution();

        var readout = attribution.Admit(new ExperienceCandidateEvidence(
            "body-2", BodyDelivered: true, BodySuppressed: suppressed, BodyDeduplicated: deduplicated,
            BodyRejected: rejected, HasExplicitClickOrFeedback: true));

        Assert.False(readout.Admitted);
        Assert.Equal("body_excluded_from_attribution", readout.Reason);
        Assert.Empty(attribution.Registry);
    }

    [Fact]
    public void An_experience_candidate_is_recorded_only_when_delivery_and_a_click_are_both_present()
    {
        var attribution = new DeliveryAttribution();

        var readout = attribution.Admit(new ExperienceCandidateEvidence(
            "body-3", BodyDelivered: true, BodySuppressed: false, BodyDeduplicated: false,
            BodyRejected: false, HasExplicitClickOrFeedback: true));

        Assert.True(readout.Admitted);
        Assert.Equal("experience_attributed", readout.Reason);
        var row = Assert.Single(attribution.Registry);
        Assert.Equal("body-3", row.BodyId);
    }

    [Fact]
    public void A_delivered_body_without_an_explicit_click_is_not_recorded()
    {
        var attribution = new DeliveryAttribution();

        var readout = attribution.Admit(new ExperienceCandidateEvidence(
            "body-4", BodyDelivered: true, BodySuppressed: false, BodyDeduplicated: false,
            BodyRejected: false, HasExplicitClickOrFeedback: false));

        Assert.False(readout.Admitted);
        Assert.Equal("no_explicit_click_or_feedback", readout.Reason);
        Assert.Empty(attribution.Registry);
    }

    [Fact]
    public void A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion()
    {
        var attribution = new DeliveryAttribution();

        var readout = attribution.AttributePromise(new PromiseEvidence(
            "promise-1", PromiseSent: true, BodyDelivered: true, UserClicked: true));

        Assert.True(readout.CommitmentProposed);
        Assert.Equal("commitment_proposed", readout.Reason);
        // 一次发送至多提出承诺：它既不证明用户同意，也不证明任务已完成。
        Assert.False(readout.UserConsentProven);
        Assert.False(readout.TaskCompletedProven);
    }

    // ─────────────────────── §一.4 经验是软材料不是支配权 ───────────────────────

    [Fact]
    public void Reading_experience_as_optional_material_without_a_validity_window_fails_loudly()
    {
        var material = new ExperienceMaterial();
        material.Store(new StoredExperience("exp-1", "some soft material", Base));

        var readout = material.ReadOptional("exp-1", Base.AddMinutes(1), null);

        Assert.False(readout.Available);
        Assert.Equal("validity_window_required_as_input", readout.Reason);
        Assert.Null(readout.OptionalMaterial);
    }

    [Fact]
    public void Experience_within_the_input_validity_window_is_offered_as_optional_material()
    {
        var material = new ExperienceMaterial();
        material.Store(new StoredExperience("exp-2", "a warm exchange", Base));

        var readout = material.ReadOptional("exp-2", Base.AddMinutes(3), TimeSpan.FromMinutes(5));

        Assert.True(readout.Available);
        Assert.Equal("experience_offered_as_material", readout.Reason);
        Assert.Equal("a warm exchange", readout.OptionalMaterial);
        Assert.False(readout.CarriesSpeakingAuthority);
    }

    [Fact]
    public void Experience_past_the_input_validity_window_is_not_offered()
    {
        var material = new ExperienceMaterial();
        material.Store(new StoredExperience("exp-3", "aged material", Base));

        var readout = material.ReadOptional("exp-3", Base.AddMinutes(6), TimeSpan.FromMinutes(5));

        Assert.False(readout.Available);
        Assert.Equal("experience_past_validity", readout.Reason);
        Assert.Null(readout.OptionalMaterial);
    }

    [Fact]
    public void Experience_never_carries_authority_over_whether_to_speak()
    {
        var material = new ExperienceMaterial();
        material.Store(new StoredExperience("exp-4", "soft only", Base));

        var offered = material.ReadOptional("exp-4", Base, TimeSpan.FromMinutes(1));
        var notStored = material.ReadOptional("exp-40", Base, TimeSpan.FromMinutes(1));

        Assert.Equal("experience_offered_as_material", offered.Reason);
        Assert.Equal("experience_not_stored", notStored.Reason);
        Assert.False(offered.CarriesSpeakingAuthority);
        Assert.False(notStored.CarriesSpeakingAuthority);
    }

    [Fact]
    public void State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites()
    {
        foreach (var file in PrototypeFiles)
        {
            Assert.Empty(LegacyChainNamesFoundIn(PrototypeSource(file)));
        }

        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            File.WriteAllText(sample,
                "var a = new BehaviorPlanner(); var b = new ReplyGenerationRenderer();"
                + " var c = new CharacterStateService(); var d = new RelationshipState();"
                + " var e = new MoodState(); var f = new ExpressionLearner(); var g = new LearningOutcome();");
            Assert.Equal(
                "BehaviorPlanner,ReplyGenerationRenderer,CharacterStateService,RelationshipState,MoodState,"
                + "ExpressionLearner,LearningOutcome",
                string.Join(",", LegacyChainNamesFoundIn(File.ReadAllText(sample))));
        }
        finally
        {
            File.Delete(sample);
        }
    }

    [Fact]
    public void State_and_learning_prototype_declares_no_source_side_numeric_default()
    {
        foreach (var file in PrototypeFiles)
        {
            Assert.Empty(SourceSideDefaultsFoundIn(PrototypeSource(file)));
        }

        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            File.WriteAllText(sample,
                "private const int FallbackBand = 4; private static readonly TimeSpan FallbackWindow = TimeSpan.Zero;");
            Assert.Equal("const,static readonly",
                string.Join(",", SourceSideDefaultsFoundIn(File.ReadAllText(sample))));
        }
        finally
        {
            File.Delete(sample);
        }
    }

    // ───────────────── §一.5 旧模式单向隔离＋继承最小来源约束 ─────────────────

    [Fact]
    public void Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped()
    {
        var inheritance = new LegacyInheritance();

        var nullSource = inheritance.Import(new InheritedStateInput("old-1", "v", "relationship", null));
        var nullVersion = inheritance.Import(new InheritedStateInput(
            "old-2", "v", "mood", new LayerSourceReference("src-1", null)));

        Assert.Equal("Failed", nullSource.Outcome);
        Assert.Equal("inherited_source_missing", nullSource.Reason);
        Assert.Equal("Failed", nullVersion.Outcome);
        Assert.Equal("inherited_source_missing", nullVersion.Reason);
        Assert.Empty(inheritance.Imported);
    }

    [Fact]
    public void Every_inherited_input_leaves_a_named_readout_rather_than_silently_vanishing()
    {
        var inheritance = new LegacyInheritance();

        var outcomes = new[]
        {
            inheritance.Import(new InheritedStateInput("a", "1", "relationship", new LayerSourceReference("src-1", 2))).Outcome,
            inheritance.Import(new InheritedStateInput("b", "2", "mood", null)).Outcome,
            inheritance.Import(new InheritedStateInput("c", "3", "scene", new LayerSourceReference("src-3", 1))).Outcome,
        };

        // 三条输入各留下一条命名读数：一条都没有静默消失（两条 Imported、一条 Failed）。
        Assert.Equal("Imported,Failed,Imported", string.Join(",", outcomes));
        Assert.Equal(2, inheritance.Imported.Count);
    }

    [Fact]
    public void A_successful_inheritance_carries_its_source_reference_and_version()
    {
        var inheritance = new LegacyInheritance();

        var readout = inheritance.Import(new InheritedStateInput(
            "old-3", "carried", "relationship", new LayerSourceReference("src-inherit", 6)));

        Assert.Equal("Imported", readout.Outcome);
        Assert.Equal("inherited_with_provenance", readout.Reason);
        var row = Assert.Single(inheritance.Imported);
        Assert.Equal("src-inherit", row.SourceId);
        Assert.Equal(6, row.SourceVersion);
    }

    [Fact]
    public void There_is_no_second_write_path_back_to_the_legacy_layer()
    {
        var inheritance = new LegacyInheritance();

        var readout = inheritance.AttemptWriteback(new InheritedStateInput(
            "old-9", "must not land", "relationship", new LayerSourceReference("src-1", 1)));

        Assert.Equal("Refused", readout.Outcome);
        Assert.Equal("one_way_isolation_only", readout.Reason);
        Assert.Empty(inheritance.LegacyWrites);
    }

    // ─────────────────────────────── 仪器与夹具 ───────────────────────────────

    private static IEnumerable<string> LegacyChainNamesFoundIn(string source) =>
        LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal));

    private static IEnumerable<string> SourceSideDefaultsFoundIn(string source)
    {
        if (source.Contains("const ", StringComparison.Ordinal)) yield return "const";
        if (source.Contains("static readonly", StringComparison.Ordinal)) yield return "static readonly";
    }

    private static string PrototypeSource(string fileName)
    {
        var path = Path.Combine(RepositoryRoot(), "src", "Pry.Core", "Narrative", "StateAndLearning", fileName);
        Assert.True(File.Exists(path), $"隔离原型源码不在盘上：{path}");
        return File.ReadAllText(path);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pry.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "自测试输出目录上溯不到 Pry.slnx，仓库根无法定位。");
        return directory!.FullName;
    }
}
