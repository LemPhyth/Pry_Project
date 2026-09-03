using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Pry.Core.Models;

namespace Pry.Core.TurnTaking;

public static class AttachmentTextExtractor
{
    private const int MaxCharacters = 2_200;

    public static async Task<string> ExtractForPromptAsync(ChatAttachment attachment, CancellationToken cancellationToken = default)
    {
        string text;
        try
        {
            text = attachment.Kind == ChatAttachmentKind.Docx
                ? ReadDocx(attachment.Path)
                : await File.ReadAllTextAsync(attachment.Path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Xml.XmlException)
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

    private static string ReadDocx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("DOCX 缺少正文。");
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var paragraphs = document.Descendants(word + "p")
            .Select(p => string.Concat(p.Descendants(word + "t").Select(t => t.Value)))
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(Environment.NewLine, paragraphs);
    }
}
