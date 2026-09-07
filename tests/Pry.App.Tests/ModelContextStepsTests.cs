using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class ModelContextStepsTests
{
    [Theory]
    [InlineData(4096, true, 8192)]
    [InlineData(8192, true, 32768)]
    [InlineData(32768, true, 131072)]
    [InlineData(131072, true, 262144)]
    [InlineData(262144, true, 262144)]
    [InlineData(262144, false, 131072)]
    [InlineData(131072, false, 32768)]
    [InlineData(32768, false, 8192)]
    [InlineData(8192, false, 4096)]
    [InlineData(4096, false, 4096)]
    [InlineData(10000, true, 32768)]
    [InlineData(10000, false, 8192)]
    public void Moves_between_catalog_tiers(int current, bool increase, int expected) =>
        Assert.Equal(expected, ModelContextSteps.Move(current, increase));
}
