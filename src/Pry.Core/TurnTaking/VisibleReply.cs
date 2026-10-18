namespace Pry.Core.TurnTaking;

/// <summary>
/// "这一轮她到底说没说上话"只在这里定义一次（判据：<c>DEF-P3SEAM-04</c> 完成标准 ③④）。
/// <c>ModelReplyShape.Empty</c> 与 <c>ModelReplyShape.OnlyInternalDraft</c> 这两档兑现到界面上的就是
/// <see cref="Unanswered"/> 那一条占位，同一个出口、同一档判定，不在 Core 之外写出第二套"什么算空"：
/// 可见侧早就承认它等于"没说上话"，学习侧此前只比长度，于是两个点的轮次被记成一次成功的互动。
/// 宿主只用这一个判定口——Core 之外不出现占位文本的字面量。
/// </summary>
public static class VisibleReply
{
    /// <summary>本轮没有可落库的可见答复时投出去的那一条，与"模型一个字没输出"同档，不复用原始输出。</summary>
    internal const string Unanswered = "……";

    /// <summary>
    /// 若干条已投递气泡里是否存在她真的说出口的可见内容：空白与那条占位都不算。
    /// 一条都不算 ⇒ 这一轮在账本上按"她没说话"处理，一个字都不许教。
    /// </summary>
    public static bool HasVisibleContent(IEnumerable<string> deliveredTexts)
    {
        foreach (var text in deliveredTexts)
        {
            var trimmed = text.Trim();
            if (trimmed.Length > 0 && trimmed != Unanswered) return true;
        }

        return false;
    }
}
