using System.IO.Compression;
using System.Text;
using Pry.Core.Models;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Core.Tests;

// HDS-011 slice: AUD-04 - DOCX decompression limits must be enforced BEFORE allocation,
// the read must be genuinely cancellable, and over-limit must return a clear error (never partial content).
public sealed class AttachmentLimitTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pry-docx-limit-" + Guid.NewGuid().ToString("N"));

    public AttachmentLimitTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, true); } catch (IOException) { }
    }

    private static string WordDoc(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" +
        body + "</w:body></w:document>";

    private string MakeDocx(string documentXml, CompressionLevel level, string name = "test.docx")
    {
        var path = Path.Combine(_directory, name);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("word/document.xml", level);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(documentXml);
        stream.Write(bytes, 0, bytes.Length);
        return path;
    }

    private Task<string> Extract(string path, ChatAttachmentKind kind = ChatAttachmentKind.Docx,
        CancellationToken token = default) =>
        AttachmentTextExtractor.ExtractForPromptAsync(new ChatAttachment(path, kind, Path.GetFileName(path)), token);

    [Fact]
    public async Task A_valid_docx_still_extracts_its_body_text()
    {
        // Guards against an always-rejecting limit (which would be a green gate that never runs the real path).
        var path = MakeDocx(WordDoc("<w:p><w:r><w:t>Hello附件世界</w:t></w:r></w:p>"), CompressionLevel.NoCompression);
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.StartsWith("[附件：test.docx]", result);
        Assert.Contains("Hello附件世界", result);
        Assert.DoesNotContain("超出安全解压限额", result);
    }

    [Fact]
    public async Task An_oversized_decompressed_document_is_rejected_and_leaks_no_partial_body()
    {
        // 9 MB of body is over the 8 MiB cap -> rejected on the declared length before any large allocation.
        var path = MakeDocx(WordDoc("<w:p><w:r><w:t>" + new string('甲', 3_000_000) + "</w:t></w:r></w:p>"),
            CompressionLevel.Optimal);
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.Contains("超出安全解压限额", result);
        Assert.Contains("解压字节上限", result);
        Assert.DoesNotContain(new string('甲', 50), result); // no half-decompressed content smuggled through
    }

    [Fact]
    public async Task A_high_compression_ratio_is_rejected()
    {
        // Highly repetitive body (well under the byte cap) but a thousands:1 ratio -> rejected on the ratio rule.
        var path = MakeDocx(WordDoc("<w:p><w:r><w:t>" + new string('a', 200_000) + "</w:t></w:r></w:p>"),
            CompressionLevel.Optimal);
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.Contains("超出安全解压限额", result);
        Assert.Contains("压缩比上限", result);
    }

    [Fact]
    public async Task An_over_deeply_nested_document_is_rejected_before_full_parse()
    {
        // Stored (no compression) so the ratio rule cannot mask the depth rule; nesting exceeds the depth cap.
        var body = new StringBuilder();
        for (var i = 0; i < 300; i++) body.Append("<w:p>");
        var path = MakeDocx(WordDoc(body.ToString()), CompressionLevel.NoCompression);
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.Contains("超出安全解压限额", result);
        Assert.Contains("XML 深度上限", result);
    }

    [Fact]
    public async Task An_excessive_node_count_is_rejected()
    {
        // 250k empty paragraphs exceed the node cap (bytes stay under the size cap, stored to avoid the ratio rule).
        var body = string.Concat(Enumerable.Repeat("<w:p/>", 250_001));
        var path = MakeDocx(WordDoc(body), CompressionLevel.NoCompression);
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.Contains("超出安全解压限额", result);
        Assert.Contains("XML 节点上限", result);
    }

    [Fact]
    public async Task An_overlong_attribute_name_is_rejected()
    {
        var path = MakeDocx(WordDoc("<w:p w:" + new string('a', 300) + "=\"v\"/>"), CompressionLevel.NoCompression);
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.Contains("超出安全解压限额", result);
        Assert.Contains("XML 属性名长度上限", result);
    }

    [Fact]
    public async Task The_docx_read_honours_cancellation_and_does_not_silently_swallow_it()
    {
        // Cancellation must be observable to the caller, not folded into the "read failed" string.
        var path = MakeDocx(WordDoc("<w:p><w:r><w:t>body</w:t></w:r></w:p>"), CompressionLevel.NoCompression);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Extract(path, token: cancelled.Token));
    }

    [Fact]
    public async Task A_missing_document_part_reports_a_read_failure_not_a_limit()
    {
        var path = Path.Combine(_directory, "empty.docx");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("other.txt", CompressionLevel.NoCompression);
            using var s = entry.Open();
            s.Write(Encoding.UTF8.GetBytes("no word/document.xml here"));
        }
        var result = await Extract(path, token: TestContext.Current.CancellationToken);

        Assert.Contains("读取失败", result);
        Assert.DoesNotContain("超出安全解压限额", result);
    }
}
