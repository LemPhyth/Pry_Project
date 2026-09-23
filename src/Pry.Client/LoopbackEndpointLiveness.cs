using System.Net;
using System.Net.NetworkInformation;

namespace Pry.Client;

/// <summary>
/// 重连前的廉价存活探测：目标端口在本机监听表里有没有监听者。
/// 用在死端口上反复重连的那条路径——被拒的握手在 Windows 上不是瞬时的，RST 已经回来、
/// connect 完成却被压在 TCP 层，实测每次约 2.05 秒；八轮重连就是十几秒纯等待，
/// 而"有没有监听者"这件事本机的监听表 0 毫秒就能回答。
/// 这里只用来**省掉一次明知不会成功的握手**：失败计数、退避阶梯、终止文案一概不变，
/// 监听者一回来（含后端重启在同一端口）下一次尝试照旧走真连接。
/// 只认字面量回环地址：主机名要先解析、非回环可能是远端，两者都不能拿本机监听表回答。
/// </summary>
public static class LoopbackEndpointLiveness
{
    /// <summary>该端口是否仍有监听者。凡是不能肯定的（非字面回环地址、地址解析不了、查询本身失败）一律答"有"，让真连接去判。</summary>
    public static bool IsListening(string host, int port)
    {
        if (!IPAddress.TryParse(host, out var target) || !IPAddress.IsLoopback(target)) return true;
        if (port is < 1 or > 65535) return true;
        try
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return listeners.Any(listener => Accepts(listener, target, port));
        }
        catch
        {
            // 监听表读不到（权限、API 不可用）时不能把"查不到"读成"没人监听"，否则探测会变成快速失败。
            return true;
        }
    }

    private static bool Accepts(IPEndPoint listener, IPAddress target, int port)
    {
        if (listener.Port != port) return false;
        var address = listener.Address.IsIPv4MappedToIPv6 ? listener.Address.MapToIPv4() : listener.Address;
        if (address.AddressFamily != target.AddressFamily) return false;
        // 通配地址（0.0.0.0 / ::）也吃回环连接；判"没人听"只在精确地址或通配都不命中时才成立。
        return address.Equals(target) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any);
    }
}
