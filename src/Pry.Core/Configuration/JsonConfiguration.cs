using System.Text.Json;
using Pry.Core.Models;

namespace Pry.Core.Configuration;

public static class JsonConfiguration
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException($"无法解析配置文件：{path}");
    }

    public static void Validate(CharacterDefinition character)
    {
        if (character.SchemaVersion != 1) throw new InvalidDataException($"不支持的角色定义版本：{character.SchemaVersion}");
        if (string.IsNullOrWhiteSpace(character.Id)) throw new InvalidDataException("角色 ID 不能为空。");
        if (string.IsNullOrWhiteSpace(character.Name)) throw new InvalidDataException("角色名称不能为空。");
        if (character.PromptMode == CharacterPromptMode.Structured && string.IsNullOrWhiteSpace(character.Identity)) throw new InvalidDataException("结构化角色身份不能为空。");
        if (character.PromptMode == CharacterPromptMode.Legacy && string.IsNullOrWhiteSpace(character.LegacySystemPrompt)) throw new InvalidDataException("Legacy System Prompt 不能为空。");
    }

    public static async Task SaveAsync<T>(string path, T value, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken);
        File.Move(temporary, path, true);
    }
}
