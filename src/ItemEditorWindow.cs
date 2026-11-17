namespace Cthangover.Tools.ItemsEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using Cthangover.Core.Factories.Impls;
    using Cthangover.Core.Items;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Core.Scenes;
    using Cthangover.Core.UI.Tool;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Interface.Theming;
    using Cthangover.Mods.Interface.Tool;
    using Cthangover.Tools.Common;
    using Cthangover.Tools.Contracts;
    using Cthangover.Tools.EntityEditor;
    using Cthangover.Tools.Services;
    using Godot;

    /// <summary>
    /// Editor of items (<c>"Kind": "item"</c>, <see cref = "ItemInfo"/>): name and
    /// description in every language of the mod, price, picture in the
    /// <c>items</c> group (the inventory icon), item types and the use action
    /// offered from the actions the loaded mods register. Fields of other mods'
    /// item C# (<c>heal</c>, <c>verb</c>…) are "other fields" of the base window.
    /// New items go to <c>items/&lt;mod&gt;.json</c>.
    /// <para>
    /// "Place on map" (plan 12.5): opens the RPG map editor on the first map of the item's mod with a pickup
    /// object ready to place (<see cref = "IMapPickupEditor"/>). The map editor writes no <c>Interact</c>, so the
    /// placement gives the item only once its author adds <c>action item.add id=&lt;item&gt; count=1</c>; the status
    /// line says so.
    /// </para>
    /// </summary>
    public partial class ItemEditorWindow : EntityEditorWindow
    {
        /// <summary>Builds the window and opens the first mod that has items.</summary>
        public ItemEditorWindow() : base("tools/items_editor/title", "item", "items", Fields())
        {
        }

        private static List<EntityField> Fields()
        {
            return new()
            {
                new EntityField
                {
                    Key = "Name",
                    LabelKey = "tools/items_editor/name",
                    Kind = EntityFieldKind.LocaleText,
                    KeyPattern = "items/{0}/name"
                },
                new EntityField
                {
                    Key = "Description",
                    LabelKey = "tools/items_editor/description",
                    Kind = EntityFieldKind.LocaleText,
                    KeyPattern = "items/{0}/desc"
                },
                new EntityField
                {
                    Key = "Cost",
                    LabelKey = "tools/items_editor/cost",
                    TooltipKey = "tools/items_editor/cost_tip",
                    Kind = EntityFieldKind.Integer
                },
                new EntityField
                {
                    Key = "Sprite",
                    LabelKey = "tools/items_editor/sprite",
                    TooltipKey = "tools/items_editor/sprite_tip",
                    Kind = EntityFieldKind.Picture,
                    Group = "items"
                },
                new EntityField
                {
                    Key = "ItemType",
                    LabelKey = "tools/items_editor/type",
                    TooltipKey = "tools/items_editor/type_tip",
                    Kind = EntityFieldKind.Flags,
                    Options = ItemTypes
                },
                new EntityField
                {
                    Key = "ItemAction",
                    LabelKey = "tools/items_editor/action",
                    TooltipKey = "tools/items_editor/action_tip",
                    Kind = EntityFieldKind.Choice,
                    Options = Actions
                }
            };
        }

        private static IEnumerable<string> ItemTypes()
        {
            return Enum.GetNames(typeof(ItemType)).Where(IsSelectableItemType);
        }

        private static bool IsSelectableItemType(string name)
        {
            return name != nameof(ItemType.None);
        }

        private static string ActionId(string id)
        {
            return id;
        }

        private static IEnumerable<string> Actions()
        {
            return new[]
            {
                "null"
            }.Concat(ItemActionFactory.Instance.Ids.OrderBy(ActionId, StringComparer.Ordinal));
        }

        /// <summary>A resource worth one coin with no use action; the picture is the author's to pick.</summary>
        protected override JsonObject CreateEntity(string id)
        {
            return new()
            {
                ["Id"] = id,
                ["Cost"] = 1,
                ["ItemType"] = nameof(ItemType.Resource),
                ["ItemAction"] = "null"
            };
        }

        /// <summary><c>&lt;mod&gt;/new_item</c>, the folder-style ids items use.</summary>
        protected override string NewIdBase(string mod)
        {
            return mod + "/new_item";
        }

        // Created by CreatePreviewBlock while the base window builds (before this constructor body): no initialisers.
        private Button placeButton;
        /// <summary>The "Place on map" button, below the fields.</summary>
        protected override Control CreatePreviewBlock()
        {
            var block = ViewRoot.Create<VBoxContainer>();
            block.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            block.AddChild(ToolUi.Section("tools/items_editor/section_map"));
            placeButton = ToolUi.Button("tools/items_editor/place_on_map", PlaceOnMap, ButtonKind.Secondary,
                 "tools/items_editor/place_on_map_tip");
            block.AddChild(placeButton);
            return block;
        }

        /// <summary>Opens the RPG map editor with the item's pickup ready to place on the item's mod's first map.</summary>
        private void PlaceOnMap()
        {
            var mod = ModId;
            if (mod == null || SelectedId == null)
            {
                return;
            }

            var map = ModFileService.ListFiles(mod, "maps", ".json").FirstOrDefault();
            if (map == null)
            {
                SetStatus(ToolUi.Tr("tools/items_editor/place_no_map"), ToolStatusKind.Error);
                return;
            }

            var provider = ToolFactory.Instance.Get("rpg_map_editor");
            if (provider == null)
            {
                SetStatus(ToolUi.Tr("tools/items_editor/place_missing"), ToolStatusKind.Error);
                return;
            }

            var window = provider.CreateWindow();
            ToolWindow.ShowWindow(window);
            var pickupEditor = window as IMapPickupEditor;
            var pickup = pickupEditor?.PreparePickup(mod, map, SelectedId);
            var created = false;
            if (pickup == null)
            {
                // Plan 14.G4: the item has no pickup object yet, so one is written from its picture and placed at once.
                var id = CreatePickup(mod);
                if (id == null)
                {
                    return;
                }

                created = true;
                pickup = pickupEditor?.PreparePickup(mod, map, SelectedId);
            }

            if (pickup == null)
            {
                SetStatus(ToolUi.Tr("tools/items_editor/place_no_pickup"), ToolStatusKind.Error);
                return;
            }

            SetStatus(string.Format(ToolUi.Tr(created ? "tools/items_editor/place_created" : "tools/items_editor/place_done"), pickup,
                 SelectedId), ToolStatusKind.Info);
        }

        /// <summary>
        /// Writes the pickup object of the item into the item's mod (<c>objects/pickup_&lt;name&gt;.json</c>, id
        /// <c>&lt;mod&gt;/pickup/&lt;name&gt;</c>, the item's picture as its part) and refreshes the caches and the running game.
        /// Returns the object's id, or null with the status line set (no picture, a name that is not lowercase, a file in the way).
        /// </summary>
        private string CreatePickup(string mod)
        {
            var name = SelectedId?.Split('/').Last() ?? string.Empty;
            if (!Regex.IsMatch(name, "^[a-z0-9_]+$"))
            {
                SetStatus(ToolUi.Tr("tools/items_editor/place_no_name"), ToolStatusKind.Error);
                return null;
            }

            var sprite = Document?.Find(SelectedId)?.Data?["Sprite"] is JsonValue value && value.TryGetValue(out string picture) 
                ? picture : null;
            if (string.IsNullOrEmpty(sprite))
            {
                SetStatus(ToolUi.Tr("tools/items_editor/place_no_art"), ToolStatusKind.Error);
                return null;
            }

            var path = "objects/pickup_" + name + ".json";
            if (ModFileService.ReadText(mod, path) != null)
            {
                SetStatus(ToolUi.Tr("tools/items_editor/place_no_pickup"), ToolStatusKind.Error);
                return null;
            }

            var id = mod + "/pickup/" + name;
            using var pickupPicture = ItemPickupPicture.Prepare(mod, sprite, id, out var pictureProblem);
            if (pickupPicture == null)
            {
                SetStatus(ToolUi.Tr(pictureProblem), ToolStatusKind.Error);
                return null;
            }

            var part = new JsonObject
            {
                ["Image"] = pickupPicture.Id,
                ["Geometry"] = "upright",
                ["At"] = new JsonArray
                {
                    0.125,
                    0.125
                },
                ["Size"] = new JsonArray
                {
                    0.75,
                    0.75
                },
            };
            var entity = new JsonObject
            {
                ["Id"] = id,
                ["Form"] = "sprite",
                ["Category"] = "pickup",
                ["Solid"] = false,
                ["Parts"] = new JsonArray(part),
            };
            // The shared writer cannot write the decimal anchor (0.125); indented JSON with a type resolver and LF lines is the same file.
            var text = new JsonObject
            {
                ["Kind"] = "rpg_object",
                ["Items"] = new JsonArray(entity)
            }.ToJsonString(new System.Text.Json.JsonSerializerOptions { 
                WriteIndented = true, 
                TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(), 
            }) + "\n";
            if (!ModFileService.WriteText(mod, path, text))
            {
                SetStatus(ToolUi.Tr("tools/common/readonly"), ToolStatusKind.Error);
                return null;
            }

            pickupPicture.Commit();
            ModManager.Instance.GetResolver<PatchResolver>()?.ClearCache();
            ModManager.Instance.GetResolver<EntityResolver>()?.ClearCache();
            ModManager.Instance.GetResolver<FileListResolver>()?.ClearCache();
            SceneManager.ReloadContent();
            GameLogger.Log("ITEMS_EDITOR", $"pickup '{id}' written to '{mod}/{path}'");
            return id;
        }
    }
}
