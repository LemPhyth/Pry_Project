using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class UserProfileDialogTests
{
    [Fact]
    public void Draft_trims_profile_and_preserves_unrelated_theme_values()
    {
        var theme = new ThemePreferences { AccentColor = "#123456", AvatarSize = 60 };

        var result = UserProfileDialog.BuildResult(new UserProfilePreferences(), theme,
            "  用户  ", "  签名  ", null, new ImageDisplayPreferences(),
            new Dictionary<string, ImageDisplayPreferences>());

        Assert.Equal("用户", result.Profile.DisplayName);
        Assert.Equal("签名", result.Profile.Signature);
        Assert.Equal("#123456", result.Theme.AccentColor);
        Assert.Equal(60, result.Theme.AvatarSize);
        Assert.Null(result.Theme.UserAvatarPath);
    }

    [Fact]
    public void Draft_stores_crop_for_selected_avatar()
    {
        var path = Path.GetTempFileName();
        try
        {
            var display = new ImageDisplayPreferences { FocusX = .2, FocusY = .7, Zoom = 1.5 };
            var result = UserProfileDialog.BuildResult(new UserProfilePreferences(), new ThemePreferences(),
                "用户", "", path, display, new Dictionary<string, ImageDisplayPreferences>());

            Assert.Equal(path, result.Theme.UserAvatarPath);
            Assert.Equal([path], result.Theme.UserAvatarHistory);
            Assert.Equal(display, result.Theme.UserAvatarDisplays[path]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
