using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class BackgroundImageCacheTests
{
    [Fact]
    public void Cache_key_tracks_path_timestamp_and_blur_radius()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-background-{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllBytes(path, [137, 80, 78, 71]);

            using var cache = new BackgroundImageCache(2);
            var first = cache.GetKey(path, 3);
            var same = cache.GetKey(path, 3.04);
            var differentRadius = cache.GetKey(path, 0);
            File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(2));
            var differentVersion = cache.GetKey(path, 3);

            Assert.Equal(first, same);
            Assert.NotEqual(first, differentRadius);
            Assert.NotEqual(first, differentVersion);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
