using Microsoft.Data.Sqlite;

namespace Pry.Core.Tests;

/// <summary>
/// fixture 收尾只准清"自己这一条库"产生的连接池。
/// <see cref="SqliteConnection.ClearAllPools"/> 是进程级操作：它会 dispose 掉此刻别的测试类正在使用的
/// <c>sqlite3</c> 句柄，而 <c>Pry.Core.Tests</c> 没有关 collection 并行，所以受害方的表现是下一次
/// prepare 拿到已释放句柄抛 <see cref="ObjectDisposedException"/>，异常里一行都不指向真正的肇事者（DEF-P1WALK-04）。
/// </summary>
internal static class SqliteTestPools
{
    /// <param name="databasePath">本 fixture 独占的库文件；清完这两个池之后该文件（含 -wal / -shm）不再被占用。</param>
    public static void Clear(string databasePath)
    {
        // MemoryDatabase 用它自己那条串建池，形状必须逐字对齐，否则清的是另一个根本不存在的池。
        SqliteConnection.ClearPool(new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared
        }.ToString()));
        // 用例直连库做突变（造触发器冲突、改序号）时用的是只带 Data Source 的那条串，那是另一个池键。
        SqliteConnection.ClearPool(new SqliteConnection($"Data Source={databasePath}"));
        // SqliteMemoryRepository 自带第三条串（外键与默认超时是它的语义，不是Formatting）：漏掉这一条，
        // 长期记忆的池就没人清，fixture 删目录时拿到的是 memory.db 仍被占用（DEF-P1WALK-04 的同族形状）。
        SqliteConnection.ClearPool(new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 5
        }.ToString()));
    }
}
