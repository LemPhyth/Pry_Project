using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class SidebarResizeStateTests
{
    [Fact]
    public void Drag_uses_pointer_delta_and_clamps_width()
    {
        var state = new SidebarResizeState();
        state.Begin(100, 300);

        Assert.Equal(350, state.Move(150));
        Assert.Equal(420, state.Move(500));
        Assert.Equal(220, state.Move(-500));
        Assert.Equal(220, state.Complete());
        Assert.False(state.Active);
    }

    [Fact]
    public void Move_before_begin_has_no_effect()
    {
        var state = new SidebarResizeState();
        Assert.Equal(0, state.Move(300));
        Assert.False(state.Active);
    }
}
