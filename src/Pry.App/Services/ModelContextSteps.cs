namespace Pry.App.Services;

public static class ModelContextSteps
{
    public static readonly IReadOnlyList<int> Values = [4096, 8192, 32768, 131072, 262144];

    public static int Move(int current, bool increase) => increase
        ? Values.FirstOrDefault(value => value > current, Values[^1])
        : Values.LastOrDefault(value => value < current, Values[0]);
}
