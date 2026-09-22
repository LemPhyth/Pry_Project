namespace Pry.Api.Services;

/// <summary>
/// 上传体积上限（PLAT-006）。之前两处 <c>long.MaxValue</c> 让"本机后端"成为一条无界的磁盘写入
/// 通道：任何一个能连上回环端口的进程都能把用户目录写满，而 10 MiB 只是一条提示不是边界。
/// 这里把边界分成两层：<see cref="CeilingUploadBytes"/> 是编译期天花板，同时出现在端点特性与
/// <c>FormOptions</c> 里，配置无法把它抬高；<see cref="ResolveMaximumUploadBytes"/> 是可在天花板以下
/// 收紧的运行期上限（只走环境变量/测试注入，不进 appsettings.json）。
/// </summary>
public static class MediaUploadLimits
{
    /// <summary>单文件字节数天花板。属性特性只能引用常量，所以这一条必须是 <c>const</c>。</summary>
    public const long CeilingUploadBytes = 50L * 1024 * 1024;

    /// <summary>multipart 分隔符、部件头与字段名的余量：上限按"文件本体"算，请求体要多留这一点。</summary>
    public const long MultipartOverheadBytes = 1L * 1024 * 1024;

    /// <summary>请求体天花板，等于文件天花板加 multipart 余量。</summary>
    public const long CeilingRequestBodyBytes = CeilingUploadBytes + MultipartOverheadBytes;

    /// <summary>运行期可配置的下界：再小就无法承载一个正常截图，测试用小值时也不应把服务配成不可用。</summary>
    public const long MinimumMaximumUploadBytes = 64L * 1024;

    /// <summary>未配置时的运行期上限直接取天花板。</summary>
    public const long DefaultMaximumUploadBytes = CeilingUploadBytes;

    public static long ResolveMaximumUploadBytes(IConfiguration configuration)
    {
        var configured = configuration.GetValue("Pry:MaximumUploadBytes", DefaultMaximumUploadBytes);
        // 越界一律夹取而不是报错：这是一个本地单机应用，配置写错不该让后端起不来，也不该让上限被配没。
        return Math.Clamp(configured, MinimumMaximumUploadBytes, CeilingUploadBytes);
    }
}

/// <summary>请求体超过运行期上限。由中间件映射为 <c>413 payload_too_large</c>。</summary>
public sealed class PayloadTooLargeException(string resource, long maximumBytes)
    : Exception($"{resource} 超过允许大小 {maximumBytes} 字节")
{
    public string Resource { get; } = resource;
    public long MaximumBytes { get; } = maximumBytes;
}
