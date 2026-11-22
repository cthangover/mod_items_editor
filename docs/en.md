# Items Editor — editing a mod's items in a form

The Items Editor edits the items of a mod while the game runs: names and descriptions in every language of the mod, price, picture, item types and the action used from the bag. **Save** writes the mod's files and the game shows the change at once — no restart, no reload.

It opens from the tool box (the hammer on the toolbar) as **Items**.

## An edit and its result in the game

In RPG Plus the bag holds the village shop's elixir:

![The inventory with the Elixir selected: Targeted, price 10.](assets/bag-before.jpg)

In the editor: choose the mod `rpg_plus_game` at the top, pick **Elixir** in the list, type a new English name and press **Save** (Ctrl+S):

![The Items window on rpg_plus_game: the list Brushwood, Stones, Forest mushroom and Dawn elixir; the form of rpg_demo/elixir with the name key rpg_demo/item/elixir/name, text (en) Dawn elixir and the other six languages unchanged, the description in seven languages, Price 10, Icon rpg_demo/healing_elixir with its picture, Types TargetUsed, Action action/heal. The status line: "Saved: 0 file(s), 1 text(s) in mod rpg_plus_game; the game re-read its data".](assets/editor-saved.jpg)

The editor illustration is from an earlier capture. Legacy `rpg_demo/` keys or an `rpg_tileset` palette visible in it are historical; use the current `rpg_plus/` keys and `rpg_object` model described in the text.

Only the English line of the mod's `locale/en.properties` changed; the item file was not touched. The same elixir in the bag right after:

![The inventory now shows the item as Dawn elixir, still Targeted, price 10.](assets/bag-after.jpg)

## The form

- **Id** — the item's id; scenarios, shops, maps and other mods find it by this id.
- **Name** and **Description** — the translation key and a line per language of the mod. A new item gets keys made from its id; the texts go into the mod's locale files.
- **Price** — in coins (shops can set their own prices).
- **Icon** — a picture of the items group; **…** opens a grid to pick one.
- **Types** — Quest, Used, TargetUsed, Food, Resource, Recipe, CantDrop; several can be on.
- **Action** — what using the item does: `null` or one of the item actions the loaded mods register (`action/heal`, `action/equip`…).
- **Fields of other mods** — fields that other mods describe in their schemas for items (for example Equipment's equip type, traits and allowed classes) appear in their own section with the same kind of editors. The **equip type** is a picker: it offers the types the equipment slots accept, so nothing is typed by hand.
- **Id** of a saved item is read-only here; **Rename…** under it changes the id (see below).
- **Other fields** — everything else in the item's JSON, shown and edited as JSON; it is kept as it is.

The toolbar: **New** (Ctrl+N) adds `<mod>/new_item` — a resource worth one coin with no action; **Copy** (Ctrl+D), **Delete**, **Undo**/**Redo** (Ctrl+Z / Ctrl+Y). Save writes only the files whose text changed; a new item goes to `items/<mod>.json`.

## Items of other mods

The list shows the items of the chosen mod. Tick **Entries of other mods** to add the items of its dependencies; they are marked `↗ owner`. Editing such an item does not touch the owner's file: the change is saved as a patch in your mod (`patches/item/<your mod>.json`) that holds only the values you changed. **Reset to inherited** removes that patch, **Delete** writes a `$delete` entry, and **Copy** makes an own item under a new id. Only mods installed as folders can be written to.

Each field shows where its value comes from. A badge tells you when the value is your own patch, another mod's patch, or inherited through `Extends`; the **↺** button removes your own value of the field, and the inherited one comes back.

## Renaming an id

**Rename…** appears under the Id of a saved item. It asks for the new id, and **Show changes** lists the files that will change before anything is written:

- the item's own file and every file of the mod that names the item (recipes, shops, placements, scenarios, character loot) are rewritten with the new id;
- the files of other mods that name the item are only listed: they are not changed, and after the rename those mods no longer find it.

**Rename** saves your pending edits first, then writes the listed files; the game knows the new id at once. **Undo rename** in the toolbar puts every changed file back exactly as it was. An id has letters, digits, `_`, `-` and `/` between its parts, and must be free in every mod. A reference the schemas do not describe (a value a script builds at run time, or a text in code) is not found: check such places by hand.

## Place on map

**Place on map** opens the RPG Map Editor on the first map file of the item's mod, with a pickup object already chosen for placing. The pickup is the object of category `pickup` whose id ends with the item's name: for the item `rpg_plus/amber` it is `rpg_plus/pickup/amber`. If the item has no pickup object yet, the editor writes one into the item's mod (`objects/pickup_<name>.json`, id `<mod>/pickup/<name>`) with the item's picture, and chooses it at once. The item needs a picture and a name made of lowercase letters, digits and underscores; the status line says so otherwise. The map is fitted to the window, so the whole map is in view. Click a cell to place the pickup, then set its Interact in the map editor to give the item.


A new pickup copies its item picture into `rpg_images/<mod>/pickup/<name>.<extension>` of the same mod. The copy preserves the original bytes; the inventory picture stays unchanged. The object references this separate map picture, so it renders and validates in both projections. An existing different file is not overwritten: the editor reports a conflict. If writing the object does not complete, the newly created copy is removed.

![The RPG Map Editor opened by Place on map on rpg_plus_game's first map, rpg_desert: the Sprites tab has the amber pickup chosen and the Place tool is active.](assets/place-on-map.jpg)

The map editor does not automatically give the pickup an interaction. Select the placement and choose **Interaction → Does → Gives an item**, then choose the item and count and save. Inline event commands can also run `action item.add id=<item> count=1`. For a one-time pickup, add a saved flag and a state or event condition; the Give item picker alone repeats on every use.

## If something goes wrong

1. **The mod list is empty.** No mod can be written to; packed mods cannot be edited.
2. **The bag still shows the old name.** The item was saved in another language than the game's; check the line of the game's language.
3. **The action list lacks an action.** The mod that registers it is not loaded in this game.

## Storage dimensions

The item form includes cell width/height, group width/height, group limit and rotation (0 or 1). Zero group dimensions inherit the item dimensions. Limit 1 prevents grouping. These fields write the Inventory extension and retain unrelated item data. Saved groups keep their saved footprint.
