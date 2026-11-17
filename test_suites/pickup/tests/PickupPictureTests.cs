namespace Cthangover.Tools.ItemsEditor.Tests.Pickup
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Core.Mods.Testing;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Interface.Tool;
    using Cthangover.Mods.Rpg;
    using Cthangover.Mods.Rpg.Model;
    using Cthangover.Mods.Rpg.Render;
    using Cthangover.Tools.Common;
    using Cthangover.Tools.RpgEditor;
    using Cthangover.Tools.Services;
    using Godot;

    /// <summary>
    /// Exercises the real Place on map action and verifies its independently resolvable copied picture and complete JSON.
    /// </summary>
    public sealed class PickupPictureTests
    {
        [ModTest("pickup/copy_inventory_picture_into_map_group", TimeoutSeconds = 120)]
        static async Task Picture(ModTestContext ctx)
        {
            ModAssert.True(GameEnvironment.IsRuntime, "pickup authoring requires native tool windows and image groups");
            using var fixture = new PickupPictureFiles();
            var source = ModFileService.GetWritablePath("rpg_plus_game", "items/rpg_plus/resin.png");
            var target = ModFileService.GetWritablePath("rpg_plus_game", "rpg_images/rpg_plus_game/pickup/resin.png");
            ModAssert.False(File.Exists(target), "the fixture starts without a map-picture copy");
            var original = File.ReadAllBytes(source);
            var window = ToolWindow.Open<ItemEditorWindow>();
            RpgEditorWindow map = null;
            try
            {
                window.OpenMod("rpg_plus_game");
                window.Select(window.Document.Find("rpg_plus/resin"));
                var button = FindButton(window, ToolUi.Tr("tools/items_editor/place_on_map"));
                ModAssert.NotNull(button, "the author's ordinary Place on map control is present");
                button.EmitSignal(Button.SignalName.Pressed);
                await Frames();
                map = FindMap(((SceneTree)Godot.Engine.GetMainLoop()).Root);
                ModAssert.NotNull(map, "the action opens the real map editor");
                ModAssert.Equal("rpg_plus_game/pickup/resin", map.PlaceObject, "the new pickup is ready for placing");
                ModAssert.True(File.Exists(target), "the action creates an independent map-picture resource");
                ModAssert.True(File.ReadAllBytes(target).AsSpan().SequenceEqual(original),
                    "the new map picture preserves every source byte without rescaling");
                ModAssert.True(File.ReadAllBytes(source).AsSpan().SequenceEqual(original), "the inventory picture is untouched");
                var json = ModFileService.ReadText("rpg_plus_game", "objects/pickup_resin.json");
                var root = JsonNode.Parse(json);
                var value = root["Items"][0];
                var picture = value["Parts"][0]["Image"].GetValue<string>();
                ModAssert.Equal("rpg_plus_game/pickup/resin", picture, "Parts.Image names the resource in the map group");
                var restored = JsonSerializer.Deserialize<RpgObject>(value.ToJsonString(), JsonOptionsProvider.ReadOptions);
                var roundTrip = JsonSerializer.Deserialize<RpgObject>(JsonSerializer.Serialize(restored, JsonOptionsProvider.ReadOptions),
                    JsonOptionsProvider.ReadOptions);
                ModAssert.Equal(restored.ID, roundTrip.ID, "pickup identity survives complete JSON serialization");
                ModAssert.True(roundTrip.Form == RpgObjectForm.Sprite, "the saved object remains a sprite");
                ModAssert.Equal("pickup", roundTrip.Category, "the saved category remains available to the map picker");
                ModAssert.True(roundTrip.Parts[0].Geometry == RpgPartGeometry.Upright, "the saved part retains its geometry");
                ModAssert.Equal(picture, roundTrip.Parts[0].Image, "the copied resource id survives the round trip");
                ModAssert.True(roundTrip.Parts[0].At[0] == 0.125f && roundTrip.Parts[0].At[1] == 0.125f,
                    "both authored offsets survive the round trip");
                ModAssert.True(roundTrip.Parts[0].Size[0] == 0.75f && roundTrip.Parts[0].Size[1] == 0.75f,
                    "both authored sizes survive the round trip");
                ModAssert.True(roundTrip.Solid == false, "the copied picture explicitly remains non-solid");
                using var textures = new RpgTextures();
                using var builder = new RpgRenderSceneBuilder(textures.Image, textures.Charset);
                var looks = builder.PartLooks(roundTrip, 32);
                ModAssert.Equal(1, looks.Count, "the actual renderer builds one valid pickup part");
                ModAssert.NotNull(looks[0].Texture, "the actual map texture loader resolves the copied resource");
                var projections = new[]
                {
                    "topdown",
                    "isometric",
                };
                foreach (var projection in projections)
                {
                    map.SetProjection(projection);
                    var at = map.Canvas.CanvasPointOf(new Vector2I(3, 3));
                    using var press = new InputEventMouseButton
                    {
                        ButtonIndex = MouseButton.Left,
                        Pressed = true,
                        Position = at,
                    };
                    using var release = new InputEventMouseButton
                    {
                        ButtonIndex = MouseButton.Left,
                        Position = at,
                    };
                    map.Canvas._GuiInput(press);
                    map.Canvas._GuiInput(release);
                    await Frames();
                    var report = RpgMapDataAudit.Check(map.Document, projection, ObjectOf);
                    ModAssert.True(report.Complete, "the authored map remains fully checked in " + projection);
                    ModAssert.Equal(0, report.Issues.Count, "the newly placed pickup has no missing picture or geometry errors");
                    ModAssert.True(map.Undo(), "one click remains one undo operation");
                }
            }
            finally
            {
                map?.QueueFree();
                window.QueueFree();
                await Frames();
            }
        }

        private static RpgObject ObjectOf(string id)
        {
            return Entities.Collect<RpgObject>().TryGetValue(id, out var value) ? value : null;
        }

        private static Button FindButton(Node parent, string text)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is Button button && button.Text == text)
                {
                    return button;
                }

                var found = FindButton(child, text);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static RpgEditorWindow FindMap(Node parent)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is RpgEditorWindow map)
                {
                    return map;
                }

                var found = FindMap(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static async Task Frames()
        {
            var tree = (SceneTree)Godot.Engine.GetMainLoop();
            for (var i = 0; i < 4; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
    }
}
