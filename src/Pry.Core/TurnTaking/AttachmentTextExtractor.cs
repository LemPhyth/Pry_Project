using System.IO.Compression;
using System.Text;
using System.Xml;
using Pry.Core.Models;

namespace Pry.Core.TurnTaking;

public static class AttachmentTextExtractor
{
    private const int MaxCharacters = 2_200;
    private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    // AUD-04：限额先于分配。这些是文档级上限，超限即拒绝，绝不返回半截解压内容。
    private const long MaxDocxDecompressedBytes = 8L * 1024 * 1024; // 解压字节上限（8 MiB）
    private const long MaxDocxCompressionRatio = 100;               // 压缩比上限（解压:压缩）
    private const int MaxDocxNodes = 200_000;                       // XML 元素节点上限
    private const int MaxDocxDepth = 200;                           // XML 嵌套深度上限
    private const int MaxDocxNameLength = 256;                      // 元素/属性名长度上限
    private const int MaxDocxTextCharacters = 500_000;              // 累计正文字符上限（时间/内存放大兜底）
    private const int DocxChunkSize = 8192;
    private const int DocxProcessingTimeoutMs = 15_000;             // 处理时间上限

    public static async Task<string> ExtractForPromptAsync(ChatAttachment attachment, CancellationToken cancellationToken = default)
    {
        string text;
        try
        {
            text = attachment.Kind == ChatAttachmentKind.Docx
                ? await ReadDocxAsync(attachment.Path, cancellationToken)
                : await File.ReadAllTextAsync(attachment.Path, cancellationToken);
        }
        catch (AttachmentLimitExceededException ex)
        {
            // 明确的服务端错误语义：不返回任何解压出来的正文内容，只说明因超限被拒绝。
            return $"[附件：{attachment.Name}，超出安全解压限额，已拒绝处理（{ex.Reason}）]";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or XmlException)
        {
            return $"[附件：{attachment.Name}，读取失败]";
        }

        text = text.Replace("\0", "").Trim();
        if (text.Length > MaxCharacters)
        {
            const int tailCharacters = 400;
            text = text[..(MaxCharacters - tailCharacters)]
                   + $"\n[中间内容因上下文长度限制已省略；原文共 {text.Length} 个字符]\n"
                   + text[^tailCharacters..];
        }
        return $"[附件：{attachment.Name}]\n{text}";
    }

    /// <summary>
    /// 读取 DOCX 正文。所有上限都在分配之前生效：先用压缩条目头拒绝超大/高压缩比，
    /// 再带上限地流式解压（边读边复核实际字节、取消与超时），最后用 XmlReader 流式解析
    /// （节点数、深度、名字长度、正文字符逐步封顶），不再 <c>XDocument.Load</c> 全量建 DOM 后才截短。
    /// </summary>
    private static async Task<string> ReadDocxAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("DOCX 缺少正文。");

        var compressed = entry.CompressedLength;
        var declared = entry.Length;
        if (declared > MaxDocxDecompressedBytes) throw new AttachmentLimitExceededException("解压字节上限");
        if (compressed > 0 && declared / compressed > MaxDocxCompressionRatio) throw new AttachmentLimitExceededException("压缩比上限");

        // 处理时间上限：与调用方 token 关联，超时能真正中断正在进行的解压/解析。
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(DocxProcessingTimeoutMs);
        var limitToken = timeout.Token;

        using var raw = entry.Open();
        using var buffer = new MemoryStream();
        var chunk = new byte[DocxChunkSize];
        long totalBytes = 0;
        try
        {
            while (true)
            {
                var read = await raw.ReadAsync(chunk, limitToken);
                if (read <= 0) break;
                totalBytes += read;
                // 压缩条目头可被伪造：用实际解压字节复核，超限立刻停止、不再继续分配。
                if (totalBytes > MaxDocxDecompressedBytes) throw new AttachmentLimitExceededException("解压字节上限");
                buffer.Write(chunk, 0, read);
            }
            if (compressed > 0 && totalBytes / compressed > MaxDocxCompressionRatio)
                throw new AttachmentLimitExceededException("压缩比上限");

            return ParseWordBody(buffer, limitToken);
        }
        catch (OperationCanceledException)
        {
            // 用户取消如实向上传播；仅因超时被取消才转成明确的限额错误。
            if (cancellationToken.IsCancellationRequested) throw;
            throw new AttachmentLimitExceededException("处理超时");
        }
    }

    private static string ParseWordBody(MemoryStream xml, CancellationToken token)
    {
        xml.Position = 0;
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        var paragraphs = new List<string>();
        var paragraph = new StringBuilder();
        var inTextElement = false;
        var nodes = 0;
        long textChars = 0;

        using var reader = XmlReader.Create(xml, settings);
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            var isWord = reader.NamespaceURI == WordNamespace;
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (++nodes > MaxDocxNodes) throw new AttachmentLimitExceededException("XML 节点上限");
                    if (reader.Depth > MaxDocxDepth) throw new AttachmentLimitExceededException("XML 深度上限");
                    if (reader.Name.Length > MaxDocxNameLength) throw new AttachmentLimitExceededException("XML 名称长度上限");
                    if (reader.MoveToFirstAttribute())
                    {
                        do
                        {
                            if (reader.Name.Length > MaxDocxNameLength)
                                throw new AttachmentLimitExceededException("XML 属性名长度上限");
                        }
                        while (reader.MoveToNextAttribute());
                        reader.MoveToElement();
                    }
                    if (isWord && reader.LocalName == "t" && !reader.IsEmptyElement) inTextElement = true;
                    break;

                case XmlNodeType.Text or XmlNodeType.CDATA:
                    if (inTextElement)
                    {
                        textChars += reader.Value.Length;
                        if (textChars > MaxDocxTextCharacters) throw new AttachmentLimitExceededException("文本字符上限");
                        paragraph.Append(reader.Value);
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (!isWord) break;
                    if (reader.LocalName == "t") inTextElement = false;
                    else if (reader.LocalName == "p")
                    {
                        var line = paragraph.ToString();
                        if (!string.IsNullOrWhiteSpace(line)) paragraphs.Add(line);
                        paragraph.Clear();
                    }
                    break;
            }
        }

        var tail = paragraph.ToString();
        if (!string.IsNullOrWhiteSpace(tail)) paragraphs.Add(tail);
        return string.Join(Environment.NewLine, paragraphs);
    }
}

/// <summary>附件解压超出安全限额（压缩比 / 解压字节 / XML 节点·深度·名字长度 / 文本量 / 处理时间）。
/// 与 IOException 族分开，好让调用方把它当"内容过大已拒绝"而不是"读取失败"。</summary>
public sealed class AttachmentLimitExceededException(string reason) : Exception($"附件超出安全解压限额：{reason}")
{
    public string Reason { get; } = reason;
}
