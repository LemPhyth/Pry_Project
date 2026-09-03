using Pry.Core.Expression;
using Pry.Core.Models;

namespace Pry.Core.TurnTaking;

public sealed class InterruptClassifier(StickerCatalog stickerCatalog)
{
    private static readonly string[] HardSignals =
        ["等等", "等下", "不是", "不对", "先别", "停一下", "别说了", "你误会了", "我不是这个意思", "换个话题", "先回答"];
    private static readonly HashSet<string> Backchannels = new(StringComparer.OrdinalIgnoreCase)
        { "嗯", "嗯嗯", "哦", "哦哦", "对", "确实", "哈哈", "哈哈哈", "草", "笑死", "然后呢", "继续", "我在听", "好", "好的" };

    public InterruptKind Classify(UserInputPart input)
    {
        var text = input.Text.Trim().TrimEnd('。', '！', '!', '～', '~');
        if (HardSignals.Any(text.Contains)) return InterruptKind.HardInterrupt;
        var sticker = stickerCatalog.Find(input.StickerId);
        if (text.Length == 0 && sticker?.InteractionRole == "interrupt") return InterruptKind.HardInterrupt;
        if (text.Length == 0 && sticker?.LikelyBackchannel == true) return InterruptKind.Backchannel;
        if (Backchannels.Contains(text) && !text.Contains('?') && !text.Contains('？')) return InterruptKind.Backchannel;
        return InterruptKind.SoftInterrupt;
    }
}
