using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class ChatUnderlayMaskStateTests
{
    [Fact]
    public void Calculates_normalized_mask_and_suppresses_unchanged_layout()
    {
        var state = new ChatUnderlayMaskState();

        Assert.True(state.TryCalculate(600, 60, 90, out var mask));
        Assert.Equal(.1, mask.TopTransparentEnd, 6);
        Assert.Equal(.85, mask.BottomTransparentStart, 6);
        Assert.True(mask.TopFeatherEnd > mask.TopTransparentEnd);
        Assert.True(mask.BottomFeatherStart < mask.BottomTransparentStart);
        Assert.False(state.TryCalculate(600.2, 60.2, 90.2, out _));
    }

    [Fact]
    public void Invalid_height_is_ignored_and_extreme_bars_are_clamped()
    {
        var state = new ChatUnderlayMaskState();
        Assert.False(state.TryCalculate(1, 20, 20, out _));
        Assert.True(state.TryCalculate(100, 90, 90, out var mask));
        Assert.Equal(.42, mask.TopTransparentEnd, 6);
        Assert.Equal(.58, mask.BottomTransparentStart, 6);
    }
}
