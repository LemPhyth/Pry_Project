using Avalonia.Controls;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class CharacterCardListController
{
    private readonly ListBox _list;
    private IReadOnlyList<CharacterDefinition> _characters = [];
    private bool _refreshing;

    public CharacterCardListController(ListBox list)
    {
        _list = list;
        _list.SelectionChanged += (_, _) =>
        {
            if (_refreshing || _list.SelectedItem is not CharacterCardListEntry entry) return;
            var character = _characters.FirstOrDefault(value => value.Id == entry.Id);
            if (character is not null) Selected?.Invoke(character);
        };
    }

    public event Action<CharacterDefinition>? Selected;

    public void Refresh(IReadOnlyList<CharacterDefinition> characters, string? currentId, string? selectedId)
    {
        _characters = characters;
        _refreshing = true;
        var items = characters.Select(character => CharacterCardListEntry.Create(character, currentId)).ToArray();
        _list.ItemsSource = items;
        _list.SelectedItem = items.FirstOrDefault(item => item.Id == selectedId);
        _refreshing = false;
    }

    public void ClearSelection()
    {
        _refreshing = true;
        _list.SelectedItem = null;
        _refreshing = false;
    }
}

public sealed record CharacterCardListEntry(string Id, string Label)
{
    public static CharacterCardListEntry Create(CharacterDefinition character, string? currentId) => new(
        character.Id,
        CharacterCardDraftService.Label(character) + (character.Id == currentId ? "  · 当前" : ""));

    public override string ToString() => Label;
}
