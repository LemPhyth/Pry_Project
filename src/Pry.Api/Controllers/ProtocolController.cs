using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Core.Memory;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ProtocolController(MemoryDatabase database) : ControllerBase
{
    private static string BuildVersion { get; } =
        typeof(ProtocolController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    /// <summary>握手端点：客户端与诊断界面在此读取协议版本、数据库结构版本与能力开关。</summary>
    [HttpGet("info")]
    public async Task<ProtocolInfoResponse> Info(CancellationToken token) => new(
        PryProtocol.Version, PryProtocol.MinSupportedVersion, BuildVersion,
        await database.GetSchemaVersionAsync(token), PryProtocol.Features);
}
