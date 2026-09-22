namespace Pry.Contracts;

/// <summary>
/// 线协议版本常量的唯一来源。Client、Api 与诊断界面都必须读取这里，不得各自复制字面量。
/// 兼容规则：major 必须相等，客户端 minor 不得高于服务端；字段与事件类型只增不改不删，
/// 因此客户端必须忽略未知字段与未知事件类型。语义变更必须换 major。
/// </summary>
public static class PryProtocol
{
    public const string Version = "1.0";

    /// <summary>服务端仍愿意服务的最低客户端版本。</summary>
    public const string MinSupportedVersion = "1.0";

    public const string ServerHeader = "Pry-Protocol-Version";
    public const string ClientHeader = "Pry-Client-Protocol";
    public const string MismatchErrorCode = "protocol_mismatch";

    /// <summary>
    /// 本机会话令牌请求头。回环地址不区分用户：同一台机器上任何同用户进程都能连上监听端口，
    /// 所以 1.0 用这个头把"能读到本地数据库"的权限收给持有令牌的调用方。
    /// </summary>
    public const string SessionTokenHeader = "X-Pry-Token";
    public const string UnauthorizedErrorCode = "unauthorized";

    /// <summary>
    /// 进程发现文件的名字，写在 DataDirectory 下。它是给"另起一个进程连本机后端"的调用方看的；
    /// 同进程内嵌时必须直接读同一个服务对象，不要去读自己刚写的文件。
    /// </summary>
    public const string RuntimeDescriptorFileName = "runtime.json";

    /// <summary>
    /// 未配置 <c>Pry:DataDirectory</c> 时的本机数据目录，即发现文件的默认所在。
    /// 写侧（后端）与读侧（独立进程客户端）必须取同一个来源：两边各写一份默认值时，
    /// 一旦某一边的命名变了，发现就会以"文件不存在"静默失败。
    /// </summary>
    public static string DefaultDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion");

    /// <summary>事件流空闲时下发 SSE 注释心跳的周期。</summary>
    public const int HeartbeatIntervalMs = 15_000;

    /// <summary>
    /// 客户端连续多少毫秒读不到任何字节就判定连接半开。必须覆盖至少两个完整心跳，
    /// 否则服务端一次刷写延迟就会被误判成断连；余量留给调度与网络抖动。
    /// </summary>
    public const int LivenessTimeoutMs = HeartbeatIntervalMs * 2 + 2_000;

    /// <summary>SSE 注释心跳帧，客户端只把它当作字节级存活信号，不得当成事件解析。</summary>
    public const string HeartbeatFrame = ":ping\n";

    /// <summary>
    /// 连接本机后端的普通请求预算（PLAT-001：任何出站请求都不得无界等待）。
    /// 这里放 int 而不是各文件各写一份，因为它的下限由上面两个线协议常量决定，不能独立调整：
    /// Kestrel 要等第一帧刷出才发响应头，所以空闲会话的订阅在 <see cref="HeartbeatIntervalMs"/>
    /// 之内根本还没有响应头可读；取一个 <see cref="LivenessTimeoutMs"/> 让"订阅起头"与"流中静默"同尺度。
    /// <para>
    /// 生效方式与 <see cref="RequestBudgetCeilingMs"/> 配套，别只看常量名：这一档由客户端**逐请求**声明
    /// （<c>PryBackendClient</c> 里的请求级 cancellation），不是 <c>HttpClient.Timeout</c>。整请求必须更久的
    /// 调用自己声明更宽的一档，见 <see cref="TranscriptionRequestTimeoutMs"/>。
    /// </para>
    /// </summary>
    public const int BackendRequestTimeoutMs = LivenessTimeoutMs;

    /// <summary>
    /// 语音识别的整请求预算：后端要等整个识别过程结束才回包，识别本身在远端 Provider 有 3 分钟上限，
    /// 且同一时刻只跑一个（排队会把等待叠加）。4 分钟 = 那一档上限加排队余量，仍然是有界的。
    /// </summary>
    public const int TranscriptionRequestTimeoutMs = 4 * 60_000;

    /// <summary>
    /// <c>HttpClient.Timeout</c> 只做**兜底**：逐请求预算才是做主的那一档，所以它必须比最宽的一档还宽，
    /// 否则它会先跳、把声明写小的那档顶掉（4 分钟语音预算曾被 32 秒顶掉就是这么来的）。
    /// <c>HttpClient.Timeout</c> 是客户端级而非请求级设置，因此每个真会连上后端的装配点都要用它取同一个上界：
    /// <c>EmbeddedBackend</c> 与 <c>RuntimeDiscovery.TryConnectAsync</c> 两处，各有一条断言钉住
    /// （<c>SessionTokenTests</c>、<c>RuntimeDiscoveryProcessTests</c>）。
    /// <c>PryBackendClient.Unattached</c> 例外：它的基址是端口 0，请求在连上之前就失败，预算无从生效。
    /// 余量留给调度：兜底与预算同时到点时报成"取消"而不是"超时"，界面会读成用户自己关了请求。
    /// </summary>
    public const int RequestBudgetCeilingMs = TranscriptionRequestTimeoutMs + 15_000;

    /// <summary>
    /// 当前构建真实具备的能力开关。客户端据此降级，而不是从版本号推测。
    /// 尚不存在的能力（如 websocket-events）不得出现在此列表。
    /// </summary>
    public static IReadOnlyList<string> Features { get; } =
        ["protocol-version-negotiation", "http-command-plane", "sse-event-plane", "durable-event-cursor",
            "session-token", "runtime-discovery"];

    private static (int Major, int Minor) Parsed { get; } = Parse(Version);

    public static bool IsSupported(string? clientVersion)
    {
        // 不带请求头的是版本化上线前的已发布客户端，必须继续可用，不能因为升级而断连。
        if (string.IsNullOrWhiteSpace(clientVersion)) return true;
        if (!TryParse(clientVersion, out var major, out var minor)) return false;
        return major == Parsed.Major && minor <= Parsed.Minor;
    }

    public static bool TryParse(string value, out int major, out int minor)
    {
        (major, minor) = (0, 0);
        var parts = value.Trim().Split('.');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor)) return false;
        return major > 0 && minor >= 0;
    }

    private static (int Major, int Minor) Parse(string value)
    {
        if (!TryParse(value, out var major, out var minor))
            throw new InvalidOperationException($"PryProtocol.Version 必须是 major.minor 形式，当前为 {value}。");
        return (major, minor);
    }
}

/// <summary>握手信息。路径不带版本前缀语义，客户端在协商失败时也必须能读到它。</summary>
public sealed record ProtocolInfoResponse(
    string ProtocolVersion,
    string MinSupportedClientVersion,
    string CoreVersion,
    int SchemaVersion,
    IReadOnlyList<string> Features);

/// <summary>
/// <c>runtime.json</c> 的文件格式：本机构建的后端在启动后写、优雅退出时删，供另一个进程发现端口与令牌。
/// 它是跨进程契约而不是 HTTP DTO，所以字段只增不改不删，读取方必须忽略未知字段。
/// <see cref="Token"/> 是机密：不得写入日志、错误响应或遥测。
/// </summary>
public sealed record RuntimeDescriptor(
    int Pid,
    int Port,
    string Token,
    string ProtocolVersion,
    int SchemaVersion,
    DateTimeOffset StartedAt)
{
    /// <summary>记录类型默认会把每个字段打进 ToString，而 Token 是机密，所以这里必须改写掉。</summary>
    public override string ToString() =>
        $"RuntimeDescriptor {{ Pid={Pid}, Port={Port}, Token=REDACTED, ProtocolVersion={ProtocolVersion}," +
        $" SchemaVersion={SchemaVersion}, StartedAt={StartedAt:O} }}";
}
