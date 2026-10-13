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
}
