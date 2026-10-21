namespace Pry.Contracts;

/// <summary>
/// 面向用户的错误可自述呈现：持有本接口的异常在启动失败等**未经 HTTP 通道**的路径上，
/// 也能让界面直接拿到「哪件事被拦下、用户能做什么、重试是否有意义」三件事实，
/// 而不是掉进 <c>PryErrorPresenter</c> 的泛化兜底把"版本不认识的库"说成"稍后重试"。
/// <para>
/// 约束：Headline 与 Body 必须是用户语言（中文产品文案），不得只回显异常原文；
/// 技术细节留在异常 <c>Message</c> 里由呈现层的"技术详情"展开区带走。
/// Core 与 Client 之间没有直接依赖，这个契约是它们唯一的汇合点（HDS-001 · v4 §9.3）。
/// </para>
/// </summary>
public interface IUserPresentableError
{
    /// <summary>一句话标题：说清"哪件事没有做成"。</summary>
    string Headline { get; }

    /// <summary>用户可见正文：必须包含用户下一步能执行的动作。</summary>
    string Body { get; }

    /// <summary>重试是否会改变结果。守卫类拒绝必须为 <c>false</c>，否则界面会诱导用户反复撞门。</summary>
    bool RetryAllowed { get; }
}
