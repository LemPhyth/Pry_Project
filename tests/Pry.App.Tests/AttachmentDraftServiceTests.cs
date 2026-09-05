using Pry.App.Services;
using Pry.Contracts;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class AttachmentDraftServiceTests
{
    [Fact]
    public void Add_paths_classifies_supported_files_and_reports_rejections_and_warnings()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-attachments-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var image = Write(directory, "image.PNG", [1]);
            var document = Write(directory, "notes.txt", [1, 2, 3]);
            var unsupported = Write(directory, "script.exe", [1]);
            var service = new AttachmentDraftService(maximumCount: 3, warningBytes: 2);

            var result = service.AddPaths([image, document, unsupported]);

            Assert.Collection(service.Items,
                item => Assert.Equal(ChatAttachmentKind.Image, item.Kind),
                item => Assert.Equal(ChatAttachmentKind.Text, item.Kind));
            Assert.Single(result.Rejected);
            Assert.Contains("格式不支持", result.Rejected[0]);
            Assert.Single(result.Warnings);
            Assert.Contains("notes.txt", result.Warnings[0]);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Draft_enforces_capacity_and_can_remove_sent_items()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-attachments-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var first = Write(directory, "first.txt", [1]);
            var second = Write(directory, "second.csv", [2]);
            var service = new AttachmentDraftService(maximumCount: 1);

            var result = service.AddPaths([first, second]);
            var sent = Assert.Single(service.Items);
            Assert.Contains("最多同时添加 1 个附件", Assert.Single(result.Rejected));

            service.RemoveRange([sent]);
            Assert.Empty(service.Items);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Backend_policy_replaces_client_fallback_limits()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-attachments-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var first = Write(directory, "first.txt", [1, 2]);
            var second = Write(directory, "second.txt", [3, 4]);
            var service = new AttachmentDraftService(maximumCount: 6, warningBytes: 100);
            service.ApplyPolicy(new MediaUploadPolicyResponse(1, null, 1));

            var result = service.AddPaths([first, second]);

            Assert.Single(service.Items);
            Assert.Contains("最多同时添加 1 个附件", Assert.Single(result.Rejected));
            Assert.Contains("较大", Assert.Single(result.Warnings));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string Write(string directory, string name, byte[] content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, content);
        return path;
    }
}
