using Pry.Core.Models;

namespace Pry.Core.Inference;

/// <summary>
/// 出站模型请求的有界超时（PLAT-001）。此前所有相关 <see cref="HttpClient"/> 都是
/// <c>Timeout.InfiniteTimeSpan</c>：Provider 或本地服务挂起时，前台请求被无限占住，
/// 界面既不等不到回复也等不到错误。
///
/// 两个必须知道的事实决定了这里的形状：
/// 1. <c>HttpClient.Timeout</c> 在使用 <c>ResponseHeadersRead</c> 时**只约束到响应头到达**，
///    不约束随后的正文读取（本机实测：3 秒超时的客户端持续读了 10 秒流未被打断）。
///    所以它是"起头超时"，不能替代读取超时，也正因为如此它才敢设得有界而不打断长回复。
/// 2. 生成类请求的排队等待是合法行为：llama-server 单槽时会把后到的请求排在正在生成的那一轮之后，
///    本地这一档因此必须远大于一次普通请求的响应时间，否则会把"排队"变成"超时"。
/// </summary>
public static class ProviderTimeouts
{
    /// <summary>远端 OpenAI-compatible 服务从收到请求到给出响应头的上限。</summary>
    public static readonly TimeSpan RemoteResponseHeaders = TimeSpan.FromSeconds(120);

    /// <summary>本地 llama-server 的同一段上限，留出单槽排队的余量。</summary>
    public static readonly TimeSpan LocalResponseHeaders = TimeSpan.FromSeconds(300);

    /// <summary>响应头之后，流式回复允许的最长空档。半开连接与"服务端静默挂住"都从这里出。</summary>
    public static readonly TimeSpan ResponseStreamIdle = TimeSpan.FromSeconds(120);

    /// <summary>创建模型客户端。超时在构造时定死：<c>Timeout</c> 一旦发出请求就不可改。</summary>
    public static HttpClient CreateModelClient(bool localLoopback) => new()
    {
        Timeout = localLoopback ? LocalResponseHeaders : RemoteResponseHeaders
    };

    /// <summary>
    /// 本地档位判据：进程自己拉起的 <c>local-llama</c>，或显式配在回环上的自建服务。两者都会出现
    /// "请求在单槽队列里等上一轮生成完"的合法排队，因此共用较宽的起头超时。判据用
    /// <see cref="Uri.IsLoopback"/> 而不是字符串比较：<c>localhost</c>、<c>127.0.0.1</c>、<c>::1</c> 是同一件事。
    /// </summary>
    public static bool IsLocal(ModelProfile profile) =>
        profile.Provider == "local-llama" ||
        Uri.TryCreate(profile.BaseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
