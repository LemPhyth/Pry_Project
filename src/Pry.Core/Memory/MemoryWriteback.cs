using System.Text.RegularExpressions;
using Pry.Core.Models;

namespace Pry.Core.Memory;

/// <summary>
/// Deliberately small, deterministic admission grammar. The entire message must be one explicit
/// first-person assertion. Quotes, attachments, hypotheticals and third-party attributions fail closed.
/// </summary>
public sealed class ExplicitFactExtractor : IMemoryCandidateExtractor
{
    private static readonly Regex Assertion = new(
        @"^(?<correction>更正[，,:：]\s*)?我(?<now>现在)?(?<verb>不喜欢|喜欢|住在|叫)(?<value>[\p{L}0-9 ·-]{1,40}?)(?<end>了)?[。.!！]?$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
    private static readonly string[] Excluded = ["说", "假如", "如果", "可能", "例如", "比如", "忽略", "指令", "系统", "提示词", "假装", "扮演", "应该", "必须", "不要", "朋友", "同事", "小说", "故事", "测试", "但是", "因为", "不过", "他说", "她说"];

    public static IReadOnlyList<MemoryCandidate> Extract(ChatMessage message)
    {
        if (message.Role != ChatRole.User || message.ImagePath is not null || message.Content.Length > 100 ||
            Excluded.Any(x => message.Content.Contains(x, StringComparison.Ordinal))) return [];
        var match = Assertion.Match(message.Content.Trim());
        if (!match.Success) return [];
        var verb = match.Groups["verb"].Value; var value = match.Groups["value"].Value.Trim();
        if (value.Length == 0 || value.IndexOfAny(['我', '你', '他', '她', '它', '但', '没', '不']) >= 0) return [];
        var kind = verb is "喜欢" or "不喜欢" ? "preference" : "personal_context";
        var topic = verb is "喜欢" or "不喜欢" ? "preference:" + SqliteMemoryRepository.Fingerprint("topic", value)
            : verb == "住在" ? "personal:residence" : "personal:name";
        return [new MemoryCandidate(kind, "用户" + verb + value, topic, message.Id, message.Content.Trim(), .6,
            match.Groups["correction"].Success || match.Groups["now"].Success || verb == "不喜欢")];
    }

    public static bool IsHighSignal(ChatMessage message) => Extract(message).Count != 0;
    public Task<IReadOnlyList<MemoryCandidate>> ExtractAsync(MemoryJob job, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<MemoryCandidate>>(job.Inputs.SelectMany(Extract).ToArray());
    }
}

public sealed class MemoryWriteback(IMemoryRepository repository, IMemoryCandidateExtractor extractor,
    ConsolidationPolicy? policy = null, TimeProvider? timeProvider = null) : IMemoryWriteback
{
    private readonly ConsolidationPolicy _policy = policy ?? new();
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    // Durable enqueue only. No extraction or model work happens on the reply path.
    public Task<WritebackReceipt> SubmitAsync(CompletedMemoryTurn turn, CancellationToken token = default) =>
        repository.EnqueueAsync(turn, _policy, _time.GetUtcNow(), token);

    public async Task<bool> ProcessOneAsync(CancellationToken token = default)
    {
        var job = await repository.ClaimAsync(_policy, _time.GetUtcNow(), token);
        if (job is null) return false;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _policy.LeaseSeconds / 2)));
        try
        {
            var candidates = await extractor.ExtractAsync(job, timeout.Token).WaitAsync(timeout.Token);
            await repository.CompleteAsync(job, candidates, _time.GetUtcNow(), token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; } // Lease recovery on restart.
        catch (Exception)
        {
            await repository.FailAsync(job, _policy, _time.GetUtcNow(), token);
        }
        return true;
    }
}
