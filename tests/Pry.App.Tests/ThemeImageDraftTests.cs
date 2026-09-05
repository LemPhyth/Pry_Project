using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ThemeImageDraftTests
{
    [Fact]
    public void History_filters_missing_files_and_deduplicates_paths()
    {
        var draft = new ThemeImageDraft("a.png", ["A.png", "missing.png"],
            new Dictionary<string, ImageDisplayPreferences>(), path => path != "missing.png");
        Assert.Single(draft.History);
        draft.Select("A.png", new());
        Assert.Single(draft.History);
    }

    [Fact]
    public void Switching_images_saves_previous_crop_before_selection_changes()
    {
        var draft = new ThemeImageDraft("first.png", [],
            new Dictionary<string, ImageDisplayPreferences>(), _ => true);
        var crop = new ImageDisplayPreferences { FocusX = .2, Zoom = 2 };
        draft.Select("second.png", crop);
        Assert.Equal(crop, draft.Displays["first.png"]);
        Assert.Equal("second.png", draft.SelectedPath);
        Assert.Equal(2, draft.History.Count);
    }

    [Fact]
    public void Clearing_selection_retains_history_but_removing_drops_crop()
    {
        var source = new Dictionary<string, ImageDisplayPreferences> { ["a.png"] = new() };
        var draft = new ThemeImageDraft("a.png", [], source, _ => true);
        draft.ClearSelection(new() { Zoom = 2 });
        Assert.Null(draft.SelectedPath);
        Assert.Single(draft.History);
        draft.Select("a.png", new());
        draft.RemoveSelected();
        Assert.Empty(draft.History);
        Assert.Empty(draft.Displays);
        Assert.Single(source);
        Assert.Equal(1, source["a.png"].Zoom);
    }
}
