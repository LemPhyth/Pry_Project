using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class CharacterAvatarEditorTests
{
    [Fact]
    public void Load_preserves_path_and_crop_without_requiring_a_real_image()
    {
        var display = new ImageDisplayPreferences { FocusX = .2, FocusY = .8, Zoom = 1.7 };
        var editor = new CharacterAvatarEditor((Image _, ImageDisplayPreferences _) => { });

        editor.Load("missing.png", display);

        Assert.Equal("missing.png", editor.AvatarPath);
        Assert.Equal(display.FocusX, editor.Display.FocusX);
        Assert.Equal(display.FocusY, editor.Display.FocusY);
        Assert.Equal(display.Zoom, editor.Display.Zoom);
    }
}
