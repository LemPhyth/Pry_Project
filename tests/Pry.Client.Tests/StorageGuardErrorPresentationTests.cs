using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// HDS-001 E 条的界面出口：数据保护守卫（如"未知高版本结构拒写"）的拒绝必须原样到达用户，
/// 不许掉进泛化兜底——兜底文案"可以先重试"配到拒写守卫上就是撒谎（重试永远不会改变结果）。
/// 测试经 Contracts 契约走假守卫，与 Core 的具体异常类型解耦：Core 侧真实类型的呈现
/// 由 Pry.Core.Tests 的拒写用例保证，这里保证的是"凡是按契约报出的守卫，界面一律照契约说"。
/// </summary>
public sealed class StorageGuardErrorPresentationTests
{
    [Fact]
    public void A_guarded_refusal_is_presented_verbatim_as_storage_error()
    {
        var presented = PryErrorPresenter.Describe(new FakeGuardError());

        Assert.Equal(PryErrorKind.StorageGuarded, presented.Kind);
        Assert.Equal("本机数据库来自更新版本的 Pry", presented.Headline);
        Assert.Equal("版本 99 比本程序认识的 10 新，已拒绝写入，一份数据都没有被改动。", presented.Body);
        Assert.False(presented.CanRetry);
    }

    [Fact]
    public void Guard_technical_detail_keeps_the_exception_type_without_leaking_a_stack_frame()
    {
        var presented = PryErrorPresenter.Describe(new FakeGuardError());

        Assert.Contains("FakeGuardError", presented.TechnicalDetail, StringComparison.Ordinal);
        Assert.Contains("数据库结构版本 99", presented.TechnicalDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("at Pry.", presented.TechnicalDetail, StringComparison.Ordinal);
    }

    [Fact]
    public void The_guard_arm_precedes_every_generic_bucket()
    {
        // 假守卫刻意继承 TimeoutException（本会命中超时档、且默认给"可以再试"）：
        // 呈现结果必须是 StorageGuarded 且无重试口风，证明契约档排在分类表最前。
        var presented = PryErrorPresenter.Describe(new TimeoutShapedGuard());

        Assert.Equal(PryErrorKind.StorageGuarded, presented.Kind);
        Assert.False(presented.CanRetry);
        Assert.DoesNotContain("再试", presented.Headline + presented.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void An_allowed_retry_guard_keeps_its_own_headline_and_retry_flag()
    {
        // 迁移失败守卫（RetryAllowed=true，对应 Core 的 MemoryMigrationException 语义）：
        // 同一档呈现，但重试口径由守卫自己声明，界面不越俎代庖。
        var presented = PryErrorPresenter.Describe(new FakeRepairGuardError());

        Assert.Equal(PryErrorKind.StorageGuarded, presented.Kind);
        Assert.True(presented.CanRetry);
        Assert.Contains("回滚", presented.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_exceptions_still_fall_back_after_the_guard_arm_was_added()
    {
        // 回归护栏：新增守卫档不得改变不认识异常的既有出口。
        var presented = PryErrorPresenter.Describe(new InvalidOperationException("ordinary failure"));

        Assert.Equal(PryErrorKind.Unknown, presented.Kind);
        Assert.True(presented.CanRetry);
        Assert.Contains("ordinary failure", presented.TechnicalDetail, StringComparison.Ordinal);
    }

    private sealed class FakeGuardError : Exception, IUserPresentableError
    {
        public FakeGuardError() : base("数据库结构版本 99 高于当前程序可写入的版本 10；已拒绝写入任何数据。") { }

        public string Headline => "本机数据库来自更新版本的 Pry";
        public string Body => "版本 99 比本程序认识的 10 新，已拒绝写入，一份数据都没有被改动。";
        public bool RetryAllowed => false;
    }

    private sealed class TimeoutShapedGuard : TimeoutException, IUserPresentableError
    {
        public string Headline => "守卫抢先，超时档不许接手";
        public string Body => "这是拒写，不是慢。";
        public bool RetryAllowed => false;
    }

    private sealed class FakeRepairGuardError : Exception, IUserPresentableError
    {
        public FakeRepairGuardError() : base("持久层迁移步骤失败，已整体回滚。") { }

        public string Headline => "Pry 没有完成数据库的启动整备";
        public string Body => "这一步已整体回滚，没有留下半套结构；修好成因后重启即可从完好状态续跑。";
        public bool RetryAllowed => true;
    }
}
