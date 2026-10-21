using System.Net;
using System.Net.Sockets;
using System.Text;
using Pry.Contracts;

namespace Pry.Client;

public enum PryErrorKind
{
    LocalCoreUnavailable,
    ModelServiceUnavailable,
    ModelCapacity,
    RequestRejected,
    ResourceMissing,
    ResourceConflict,
    TurnBusy,
    RateLimited,
    BackendFault,
    Timeout,
    ProtocolMismatch,
    SessionRejected,
    ProviderRejected,
    /// <summary>数据保护守卫拦下的动作（如未知高版本结构拒写，HDS-001）：错误自带用户文案，不走泛化兜底。</summary>
    StorageGuarded,
    Unknown
}

/// <summary>
/// Headline 与 Body 面向用户，TechnicalDetail 只用于"查看技术详情"展开区。
/// </summary>
public sealed record PryUserError(
    PryErrorKind Kind,
    string Headline,
    string Body,
    string TechnicalDetail,
    bool CanRetry);

/// <summary>
/// 错误呈现的唯一出口：界面不再自己决定"该说什么、还能不能再试"。
/// 类表必须与线契约同步扩张——服务端能写出的每一个 <c>code</c> 都要有对应一档，
/// 漏掉的那一档会掉进泛化兜底，把"改密钥"说成"稍后重试"（QA-P2-04①）。
/// </summary>
public static class PryErrorPresenter
{
    private static readonly (PryErrorKind Kind, string Headline, string Body, bool CanRetry) UnknownCase =
        (PryErrorKind.Unknown, "Pry 遇到了未预期的问题", "这次操作没有完成，可以先重试；反复出现请反馈技术详情。", true);

    public static PryUserError Describe(Exception exception) => exception switch
    {
        // 守卫类拒绝先于一切分类：它们发生在 HTTP 通道之前（启动、恢复核对），
        // 文案由持有者自己按契约给出——泛化兜底会把"版本不认识的库"说成"稍后重试"，那是撒谎。
        IUserPresentableError guarded => new PryUserError(PryErrorKind.StorageGuarded, guarded.Headline, guarded.Body,
            $"异常类型：{exception.GetType().Name} · detail={exception.Message}", guarded.RetryAllowed),
        PryBackendException backend => DescribeBackend(backend),
        HttpRequestException transport => DescribeTransport(transport),
        TimeoutException or TaskCanceledException => new PryUserError(PryErrorKind.Timeout,
            "这一步等得太久", "Pry 没有在规定时间内返回结果，可以再试一次。", "异常类型：超时", true),
        _ => Fallback(exception),
    };

    private static PryUserError DescribeBackend(PryBackendException error)
    {
        var (kind, headline, body, canRetry) = Classify(error);
        return new PryUserError(kind, headline, body,
            Technical(error.Code, error.StatusCode, error.Message, error.TraceId), canRetry);
    }

    /// <summary>
    /// 禁令优先于分类：<see cref="PryBackendException.RetryForbidden"/> 为真时，任何一档都不得留下
    /// "可以再试一次"的口风——包括那些还没登记进下面分类表的码，所以兜底也走禁行那一支。
    /// </summary>
    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Classify(PryBackendException error) =>
        error.RetryForbidden ? Banned(error) : (error.Code, error.StatusCode) switch
        {
            ("validation_error", _) => Rejected("这里还差一点信息", "Pry 没有接受这次填写，请检查后重试。"),
            ("invalid_request", _) => Rejected("请求没有发出", "填写内容不符合要求，请检查后重试。"),
            ("payload_too_large", _) => Rejected("这个文件太大了", "换一个更小的图片或附件再试。"),
            ("resource_not_found", _) => Gone("找不到对应的内容", "它可能已经被删除，或者属于另一个会话。"),
            ("resource_conflict", _) => Conflict("这个内容还在被使用", "先处理引用它的会话、角色或记忆，再重试。"),
            ("idempotency_conflict", _) => Conflict("这次操作和刚才重复了", "Pry 已经处理过一次，无需再试。"),
            ("turn_busy", _) => Busy("普莱正在回复上一条", "发新消息会打断它；也可以等这条说完。"),
            ("interaction_not_available", _) => Rejected("这个角色现在不做这个动作", "换一种互动，或在角色设置里启用它。"),
            ("interaction_cooldown", _) => Limited("这一下太快了", "等几秒再试，普莱会回应你。"),
            ("proactive_not_ready", _) => Rejected("主动话题还没准备好", "先正常聊一句加载模型，或保持这个开关关闭。"),
            (ProviderErrorCodes.Auth, _) => (PryErrorKind.ProviderRejected, "模型服务不认这个密钥",
                "在设置里检查该 Provider 的 API Key；改完保存后重试。", false),
            (ProviderErrorCodes.RateLimited, _) => Limited("模型服务让我们慢一点",
                "这个 Provider 暂时限流，等一会儿再发，或在设置里换一个 Provider。"),
            (ProviderErrorCodes.Timeout, _) => Fault("模型服务没有按时回答",
                "这次请求超时了，可以再试一次；反复出现时检查一下网络或换一个 Provider。"),
            (ProviderErrorCodes.InvalidResponse, _) => (PryErrorKind.ProviderRejected, "模型返回的内容读不懂",
                "这一帧回复格式不合法，Pry 没有把它当成正常结束。请重试；持续出现时换一个 Provider。", false),
            (ProviderErrorCodes.ModelUnavailable, _) => Model("模型服务暂时没有响应",
                "本地模型可能正在排队或已经退出，稍后重试；反复失败请重新加载模型。", true),
            (ProviderErrorCodes.ContextOverflow, _) => (PryErrorKind.ModelCapacity, "这次要读的内容太长",
                "换更长的上下文窗口、缩短输入，或允许 Pry 裁剪历史后再试。", false),
            (ProviderErrorCodes.Cancelled, _) => (PryErrorKind.RequestRejected, "这次回复已经停止",
                "是你或新消息打断的，不算失败。", false),
            ("model_missing", _) or ("invalid_model", _) => Model("本地模型不可用",
                "在设置里换一个模型，或把模型文件放回模型目录。", false),
            ("runtime_missing", _) => Model("本地推理组件没有准备好", "程序文件可能不完整，重新安装后再试。", false),
            ("cuda_out_of_memory", _) or ("memory_allocation_failed", _) =>
                (PryErrorKind.ModelCapacity, "显存不够了",
                    "关闭其他占显存的程序，或在设置里降低 GPU 层数和上下文长度。", true),
            ("context_performance_low", _) => (PryErrorKind.ModelCapacity, "当前设置在这台机器上太慢",
                "换更小的模型，或允许 Pry 缩短上下文。", false),
            ("startup_timeout", _) or ("process_exited", _) or ("process_start_failed", _) =>
                Model("模型服务暂时没有响应",
                    "可以再试一次；反复失败时展开技术详情，把编号反馈给开发者。", true),
            ("internal_error", _) => Fault("Pry 暂时不可用",
                "稍后重试；如果一直失败，请把技术详情里的编号反馈出来。"),
            (_, >= HttpStatusCode.InternalServerError) => Fault("本地服务没有完成这次请求",
                "稍后重试；持续失败时展开技术详情查看编号。"),
            (_, >= HttpStatusCode.BadRequest) => Rejected("这次操作没有成功", "请检查填写内容后重试。"),
            _ => UnknownCase,
        };

    /// <summary>每一档都要给出用户能自己做的那件事；未登记的禁行码把服务端原码带出来，而不是退回"正在重连"。</summary>
    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Banned(PryBackendException error) =>
        error.Code switch
        {
            PryProtocol.MismatchErrorCode => (PryErrorKind.ProtocolMismatch, "协议版本不一致",
                "界面与本地核心的协议版本不一致，已停止重连。请用同一个发行版启动 Pry（新界面连旧核心、或反过来都会走到这里）。", false),
            PryProtocol.UnauthorizedErrorCode => (PryErrorKind.SessionRejected, "本机会话已失效",
                "本地核心拒绝了本机的会话令牌，已停止重连。请完全退出 Pry 再启动，让界面与核心重新握手。", false),
            _ => (PryErrorKind.RequestRejected, "重试不会改变结果",
                $"后端明确表示重试不会成功（{error.Code ?? $"HTTP {(int)error.StatusCode}"}），已停止重连。请按技术详情里的编号检查后重新启动 Pry。", false),
        };

    /// <summary>内嵌后端与界面同进程，端口未监听或后端提前退出都表现为回环连接被拒。</summary>
    private static PryUserError DescribeTransport(HttpRequestException error)
    {
        var technical = Technical(null, 0, error.Message, null);
        return HasRefusal(error)
            ? new PryUserError(PryErrorKind.LocalCoreUnavailable, "本地核心没有启动成功",
                "Pry 的本地服务没有在本机监听。重启 Pry 通常可以恢复；反复失败请展开技术详情查看编号。", technical, true)
            : new PryUserError(PryErrorKind.ModelServiceUnavailable, "模型服务暂时没有响应",
                "本机连接被中断，稍后重试通常就会恢复。", technical, true);
    }

    private static PryUserError Fallback(Exception exception)
    {
        var (kind, headline, body, canRetry) = UnknownCase;
        return new PryUserError(kind, headline, body, $"{exception.GetType().Name}: {exception.Message}", canRetry);
    }

    private static bool HasRefusal(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException socket
                && socket.SocketErrorCode is SocketError.ConnectionRefused
                    or SocketError.HostUnreachable or SocketError.NetworkUnreachable)
                return true;
        }
        return false;
    }

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Rejected(string headline, string body) =>
        (PryErrorKind.RequestRejected, headline, body, false);

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Gone(string headline, string body) =>
        (PryErrorKind.ResourceMissing, headline, body, false);

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Conflict(string headline, string body) =>
        (PryErrorKind.ResourceConflict, headline, body, false);

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Busy(string headline, string body) =>
        (PryErrorKind.TurnBusy, headline, body, true);

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Limited(string headline, string body) =>
        (PryErrorKind.RateLimited, headline, body, true);

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Model(string headline, string body,
        bool canRetry) => (PryErrorKind.ModelServiceUnavailable, headline, body, canRetry);

    private static (PryErrorKind Kind, string Headline, string Body, bool CanRetry) Fault(string headline, string body) =>
        (PryErrorKind.BackendFault, headline, body, true);

    private static string Technical(string? code, HttpStatusCode status, string? detail, string? traceId)
    {
        var parts = new StringBuilder();
        if (status != 0) parts.Append($"HTTP {(int)status}");
        if (!string.IsNullOrWhiteSpace(code)) parts.Append(parts.Length == 0 ? $"code={code}" : $" · code={code}");
        if (!string.IsNullOrWhiteSpace(detail)) parts.Append(parts.Length == 0 ? $"detail={detail}" : $" · detail={detail}");
        if (!string.IsNullOrWhiteSpace(traceId)) parts.Append($" · traceId={traceId}");
        return parts.Length == 0 ? "没有更多技术信息" : parts.ToString();
    }
}
