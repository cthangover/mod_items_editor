# Items Editor — editing a mod's items in a form

The Items Editor edits the items of a mod while the game runs: names and descriptions in every language of the mod, price, picture, item types and the action used from the bag. **Save** writes the mod's files and the game shows the change at once — no restart, no reload.

It opens from the tool box (the hammer on the toolbar) as **Items**.

## An edit and its result in the game

In RPG Plus the bag holds the village shop's elixir:

![The inventory with the Elixir selected: Targeted, price 10.](assets/bag-before.jpg)

In the editor: choose the mod `rpg_demo_game` at the top, pick **Elixir** in the list, type a new English name and press **Save** (Ctrl+S):

![The Items window on rpg_demo_game: the list Brushwood, Stones, Forest mushroom and Dawn elixir; the form of rpg_demo/elixir with the name key rpg_demo/item/elixir/name, text (en) Dawn elixir and the other six languages unchanged, the description in seven languages, Price 10, Icon rpg_demo/healing_elixir with its picture, Types TargetUsed, Action action/heal. The status line: "Saved: 0 file(s), 1 text(s) in mod rpg_demo_game; the game re-read its data".](assets/editor-saved.jpg)

Only the English line of the mod's `locale/en.properties` changed; the item file was not touched. The same elixir in the bag right after:

![The inventory now shows the item as Dawn elixir, still Targeted, price 10.](assets/bag-after.jpg)

## The form

- **Id** — the item's id; scenarios, shops, maps and other mods find it by this id.
- **Name** and **Description** — the translation key and a line per language of the mod. A new item gets keys made from its id; the texts go into the mod's locale files.
- **Price** — in coins (shops can set their own prices).
- **Icon** — a picture of the items group; **…** opens a grid to pick one.
- **Types** — Quest, Used, TargetUsed, Food, Resource, Recipe, CantDrop; several can be on.
- **Action** — what using the item does: `null` or one of the item actions the loaded mods register (`action/heal`, `action/equip`…).
- **Fields of other mods** — fields that other mods describe in their schemas for items (for example Equipment's equip type, traits and allowed classes) appear in their own section with the same kind of editors.
- **Other fields** — everything else in the item's JSON, shown and edited as JSON; it is kept as it is.

The toolbar: **New** (Ctrl+N) adds `<mod>/new_item` — a resource worth one coin with no action; **Copy** (Ctrl+D), **Delete**, **Undo**/**Redo** (Ctrl+Z / Ctrl+Y). Save writes only the files whose text changed; a new item goes to `items/<mod>.json`.

## Which items can be edited

The list shows the items of one mod — those in its own files. Only mods installed as folders are in the mod list. To change another mod's item without editing that mod, ship the same id in your mod and declare a dependency on the owner; the Items Editor itself edits the files of the selected mod only.

## If something goes wrong

1. **The mod list is empty.** No mod can be written to; packed mods cannot be edited.
2. **The bag still shows the old name.** The item was saved in another language than the game's; check the line of the game's language.
3. **The action list lacks an action.** The mod that registers it is not loaded in this game.
