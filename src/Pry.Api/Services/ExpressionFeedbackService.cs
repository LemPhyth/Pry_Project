using Pry.Api.Runtime;
using Pry.Contracts;

namespace Pry.Api.Services;

/// <summary>
/// 显式表达反应的接收入口：把界面上那一下"就是这样／别这样说"变成她学习后台队列里的一笔证据。
///
/// 两处顺序是有意的。一，开关检查排在最前——"这个入口今天不存在"不该先去翻投递账本，
/// 关掉的状态下连"这条消息归属哪一轮"都不必问。二，这里只往队列里放，绝不在这条请求上排空：
/// 排空归 <see cref="ExpressionDrainService"/>，一次点击把慢仓储搬到自己的请求上，
/// 就等于让表态这条后台通道重新变成会拖慢界面的那条。
/// </summary>
public sealed class ExpressionFeedbackService(CharacterIntelligenceGateway gateway, BackendRuntime runtime)
{
    public async Task<ExpressionFeedbackResponse> ReactAsync(string conversationId, long messageId,
        ExpressionFeedbackRequest request, CancellationToken token)
    {
        if (messageId <= 0) throw new ApiValidationException("messageId", "必须是一条已投递消息的编号");
        if (!runtime.Preferences.ExplicitExpressionFeedback)
            throw new ExpressionFeedbackRejectedException(ExpressionFeedbackErrorCodes.Disabled,
                "高级开关没有打开，这次点击不会被当作一次表态。");

        var outcome = await gateway.ReportExplicitFeedbackAsync(conversationId, messageId, request.Approved, token);
        return outcome switch
        {
            ExpressionFeedbackOutcome.Recorded =>
                new ExpressionFeedbackResponse(messageId, ExpressionFeedbackOutcomes.Recorded),
            // 那一轮她没有真的说出可学的说法（占位符那一类）。这不是失败：点击是真的，只是没有内容可学。
            ExpressionFeedbackOutcome.NothingToLearn =>
                new ExpressionFeedbackResponse(messageId, ExpressionFeedbackOutcomes.NothingToLearn),
            ExpressionFeedbackOutcome.MessageMissing =>
                throw new ExpressionFeedbackRejectedException(ExpressionFeedbackErrorCodes.MessageMissing,
                    "这个会话里没有这条消息。"),
            ExpressionFeedbackOutcome.WrongConversation =>
                throw new ExpressionFeedbackRejectedException(ExpressionFeedbackErrorCodes.WrongConversation,
                    "这条回复不属于当前会话，不能记到这里的某一轮上。"),
            ExpressionFeedbackOutcome.NotAttributable =>
                // 明确拒，而不是猜：退化成"按最近一轮归属"正是 DEF-P3SEAM-02 花一整刀才拆掉的写法。
                throw new ExpressionFeedbackRejectedException(ExpressionFeedbackErrorCodes.NotAttributable,
                    "这一条不是本次运行投递的，或者她的说法账本已经记不清它出自哪一轮。只在这一句还留在界面上时表这个态。"),
            _ => throw new ExpressionFeedbackRejectedException(ExpressionFeedbackErrorCodes.QueueFull,
                "后台学习队列此刻是满的，这次点击没有被记下。稍后可以再点一次。")
        };
    }
}
