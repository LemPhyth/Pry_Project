using System.Text.RegularExpressions;

namespace Pry.Api.Logging;

/// <summary>
/// 敏感字段过滤器（PLAT-006）。日志面是不可枚举的——任何一处 <c>LogInformation(ex, ...)</c> 都可能
/// 把异常原文连同请求头一起写出去——所以遮蔽不能留在调用点各自处理（原先只有
/// <c>LlamaServerManager</c> 一处字符串替换）。这里把它收到落盘前的唯一出口上。
/// 两类规则叠加：已登记的秘密值按原文精确遮蔽，字段名形式（<c>api_key</c>、<c>Authorization</c>、
/// <c>X-Pry-Token</c>、<c>Bearer</c>、<c>sk-</c> 前缀）按模式遮蔽。
/// </summary>
public sealed partial class LogRedactor
{
    public const string Placeholder = "[redacted]";

    /// <summary>短于此长度的登记值不参与遮蔽：否则 "Info"、"true" 这类常见词会被遮成噪声。</summary>
    public const int MinimumRegisteredSecretLength = 8;

    private readonly HashSet<string> _secrets = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <summary>登记一个已知秘密（本机会话令牌、外部模型密钥）。空值与过短值忽略。</summary>
    public void RegisterSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < MinimumRegisteredSecretLength) return;
        lock (_gate) _secrets.Add(value);
    }

    public int RegisteredSecretCount
    {
        get { lock (_gate) return _secrets.Count; }
    }

    public string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        string redacted = text;
        lock (_gate)
        {
            foreach (var secret in _secrets) redacted = redacted.Replace(secret, Placeholder, StringComparison.Ordinal);
        }
        redacted = FieldPattern().Replace(redacted, "$1$2$3" + Placeholder + "$3");
        redacted = BearerPattern().Replace(redacted, "$1" + Placeholder);
        redacted = ProviderKeyPattern().Replace(redacted, Placeholder);
        return UserInfoPattern().Replace(redacted, "$1" + Placeholder + "$3");
    }

    /// <summary><c>api_key": "…" / token=… / password: …</c> 这类键值形式；保留键名、分隔符与引号，只遮值。</summary>
    [GeneratedRegex(@"(?i)\b(api[_-]?key|authorization|access[_-]?token|refresh[_-]?token|token|secret|password|passwd|cookie|x-pry-token)\b(\s*[=:]\s*)([""']?)([^\s""'&,;:}]+)\3")]
    private static partial Regex FieldPattern();

    [GeneratedRegex(@"(?i)\b(bearer\s+)[A-Za-z0-9._~+/=-]{8,}")]
    private static partial Regex BearerPattern();

    /// <summary>常见推理服务的密钥形状（<c>sk-</c>、<c>sk-</c>_<c>proj</c> 前缀），即使没带字段名也要遮掉。</summary>
    [GeneratedRegex(@"\bsk-[A-Za-z0-9_-]{12,}\b")]
    private static partial Regex ProviderKeyPattern();

    /// <summary>URL 里的 <c>scheme://user:password@host</c>：遮掉口令，保留用户名与主机。</summary>
    [GeneratedRegex(@"(?i)(://[^/:@\s]*:)([^@\s]+)(@)")]
    private static partial Regex UserInfoPattern();
}
