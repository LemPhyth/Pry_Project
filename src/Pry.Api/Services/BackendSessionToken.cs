using System.Security.Cryptography;

namespace Pry.Api.Services;

/// <summary>
/// 本机会话令牌：一个进程生命周期内随机的 32 字节秘密，只存在于内存与 <c>runtime.json</c> 里。
/// 1.0 只绑回环，而回环不区分用户——同机任意同用户进程都能连上监听端口，
/// 所以"谁能读本地数据库"必须由这个令牌决定，而不是由"谁能访问 127.0.0.1"决定。
/// </summary>
public sealed class BackendSessionToken
{
    public const int SecretLength = 32;

    private readonly byte[] _secret;

    public BackendSessionToken() : this(RandomNumberGenerator.GetBytes(SecretLength))
    {
    }

    private BackendSessionToken(byte[] secret)
    {
        _secret = secret;
        Value = Convert.ToBase64String(secret);
    }

    /// <summary>放进 <c>X-Pry-Token</c> 请求头的形式。</summary>
    public string Value { get; }

    /// <summary>
    /// 判定调用方出示的令牌是否有效。必须逐字节等时比较：短路比较的耗时差是本地进程
    /// 唯一可用的旁路信号，而它足以把一个 32 字节秘密拆成 32 次可测量的猜测。
    /// 长度不等的输入由 <see cref="CryptographicOperations.FixedTimeEquals"/> 直接判否。
    /// </summary>
    public bool Matches(string? candidate)
    {
        if (candidate is null) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(candidate), _secret);
        }
        catch (FormatException)
        {
            // 非法 Base64 就是无效令牌，不是服务器错误：绝不把输入内容带进异常或日志。
            return false;
        }
    }

    /// <summary>防止任何日志、异常或 JSON 序列化路径意外把秘密写出去。</summary>
    public override string ToString() => nameof(BackendSessionToken) + "[REDACTED]";
}
