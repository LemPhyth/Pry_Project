using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class ThemeImageResourcesTests
{
    [Fact]
    public void Replacing_and_disposing_detach_and_release_images_once()
    {
        var loaded = new List<TestImage>();
        var resources = new ThemeImageResources(_ => { var image = new TestImage(); loaded.Add(image); return image; });
        var preview = new Image();
        Assert.True(resources.Load(preview, "first"));
        Assert.True(resources.Load(preview, "second"));
        Assert.Equal(1, loaded[0].DisposeCount);
        Assert.Same(loaded[1], preview.Source);
        resources.Dispose();
        resources.Dispose();
        Assert.Null(preview.Source);
        Assert.All(loaded, image => Assert.Equal(1, image.DisposeCount));
    }

    [Fact]
    public void Failed_load_clears_previous_preview()
    {
        var image = new TestImage();
        using var resources = new ThemeImageResources(path => path == "bad" ? throw new InvalidDataException() : image);
        var preview = new Image();
        resources.Load(preview, "good");
        Assert.False(resources.Load(preview, "bad"));
        Assert.Null(preview.Source);
        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public void Refresh_releases_old_gallery_and_distinguishes_empty_from_failed()
    {
        var image = new TestImage();
        using var resources = new ThemeImageResources(path => path == "bad" ? throw new InvalidDataException() : image);
        var gallery = new WrapPanel();
        ThemeImageGallery.Refresh(gallery, ["good"], "good", _ => { }, false, Brushes.Red, resources);
        Assert.IsType<Button>(Assert.Single(gallery.Children));
        ThemeImageGallery.Refresh(gallery, ["bad"], null, _ => { }, false, Brushes.Red, resources);
        Assert.Equal(1, image.DisposeCount);
        Assert.Equal("图片无法加载，请重新导入。", Assert.IsType<TextBlock>(Assert.Single(gallery.Children)).Text);
        ThemeImageGallery.Refresh(gallery, [], null, _ => { }, false, Brushes.Red, resources);
        Assert.Equal("尚未导入图片", Assert.IsType<TextBlock>(Assert.Single(gallery.Children)).Text);
    }

    private sealed class TestImage : IImage, IDisposable
    {
        public Size Size => new(10, 10);
        public int DisposeCount { get; private set; }
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) { }
        public void Dispose() => DisposeCount++;
    }
}
