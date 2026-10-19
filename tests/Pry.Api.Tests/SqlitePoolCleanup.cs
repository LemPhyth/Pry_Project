using Microsoft.Data.Sqlite;

namespace Pry.Api.Tests;

/// <summary>
/// fixture 收尾只准清"自己这一条库"产生的连接池。
/// <see cref="SqliteConnection.ClearAllPools"/> 是进程级操作：它会 dispose 掉此刻别的测试类正在使用的
/// <c>sqlite3</c> 句柄，而 <c>Pry.Api.Tests</c> 没有关 collection 并行，所以症状出现在**别人**的用例里
/// （已释放句柄上的 prepare 抛 <see cref="ObjectDisposedException"/>，或临时目录删不掉抛 <see cref="IOException"/>），
/// 异常里一行都不指向真正的肇事者（DEF-P1WALK-04）。
/// </summary>
internal static class SqliteTestPools
{
    /// <param name="databasePath">本 fixture 独占的库文件，即数据目录里的那份 <c>memory.db</c>。</param>
    public static void Clear(string databasePath)
    {
        // 串的形状必须与 MemoryDatabase 内部那条逐字对齐，否则清的是另一个根本不存在的池。
        SqliteConnection.ClearPool(new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared
        }.ToString()));
        // 宿主里的长期记忆仓储（SqliteMemoryRepository）用的是第三条串，漏掉它 memory.db 会带着池里的
        // 句柄一起留在原地，删临时目录时抛 IOException——症状出现在别人的用例里（DEF-P1WALK-04 同族）。
        SqliteConnection.ClearPool(new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 5
        }.ToString()));
    }

    /// <summary>
    /// fixture 收尾：清完自己这条库的池再删临时目录，删除带一个有界等窗口。
    /// <para>
    /// 为什么需要窗口：池里"正在归还"的那句连接不在 <see cref="SqliteConnection.ClearPool"/> 的等待范围内——
    /// 它此刻仍握着 <c>memory.db</c> 的 OS 句柄，返回之后才被销毁。整棵树并发跑（或这台机器可用内存被别的
    /// 进程压住）时，紧随其后的 <c>Directory.Delete</c> 就会拿到 <see cref="IOException"/>。合流树门禁的 run1
    /// 就是这样一条红：<c>ExpressionDrainChannelTests.The_channel_keeps_up_so_no_delivered_turn_is_dropped</c>，
    /// 异常点在 <c>DisposeAsync()</c> 的删除那一行，单跑 10/10 绿、整程序集重采 240/0 绿。
    /// </para>
    /// <para>
    /// 窗口用尽仍然删不掉**照原样抛**：这里等的是"句柄归还"这件与断言无关的事，不是把删除失败变成隐形——
    /// 真有一个 never-closed 的连接时，门禁必须继续响。非空转由
    /// <c>TestFixtureTeardownLockTests</c> 两个方向分别钉住。
    /// </para>
    /// </summary>
    /// <param name="databasePath">本 fixture 独占的库文件。</param>
    /// <param name="root">要整棵删掉的临时目录。</param>
    /// <param name="attempts">删除尝试次数上限；每次之间重清一次池并等一小段。</param>
    public static void ClearAndDeleteRoot(string databasePath, string root, int attempts = 40)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);
        for (var attempt = 1; ; attempt++)
        {
            Clear(databasePath);
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
                return;
            }
            catch (Exception error) when (attempt < attempts &&
                                          (error is IOException or UnauthorizedAccessException))
            {
                // 只等句柄归还这一件事：下一轮先重清池，因为上一次 ClearPool 时它还在途中。
                Thread.Sleep(50);
            }
        }
    }
}
