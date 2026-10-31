using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cthangover.Core.Bootstrap;
using Cthangover.Core.Items;
using Cthangover.Core.Localization;
using Cthangover.Core.Mods;
using Cthangover.Core.Mods.Resolvers;
using Cthangover.Core.Scenes;
using Cthangover.Core.Utils;
using Cthangover.Mods.Interface.Tool;
using Cthangover.Tools.EntityEditor;
using Cthangover.Tools.RpgEditor;
using Cthangover.Tools.Services;
using Godot;

namespace Cthangover.Tools.ItemsEditor.Tests
{
    /// <summary>
    /// Drives the item editor like an author: the toolbar buttons, the form's
    /// fields (their change signals), Ctrl+Z into the window, Save. Then the game
    /// itself is asked: the entity cache must hold the new item with the typed
    /// price and type, and the translation server its typed name. The new items
    /// file is deleted and the mod's locale files are restored afterwards, so
    /// rpg_plus_game stays as it was.
    /// Result line: <c>[TEST_RESULT] name=items_editor_edit passed=N failed=M</c>.
    /// </summary>
    public partial class ItemEditorTestDriver : Node
    {
        private const float TickSeconds = 0.15f;
        private const float TimeoutSeconds = 60f;
        private const string Mod = "rpg_plus_game";
        private const string NewFile = "items/rpg_plus_game.json";
        private const string NewId = "rpg_plus_game/new_item";
        private const string ResinItem = "rpg_plus/resin";
        private const string ResinPickup = "rpg_plus_game/pickup/resin";
        private const string PickupFile = "objects/pickup_resin.json";
        private const string TypedName = "Тестовый амулет";
        private const string RenamedResin = "rpg_plus/resin_renamed";
        private const string HerbalistScenario = "scenarios/rpg_plus/woodland/herbalist.scenario";

        private readonly List<string> failures = new();
        private readonly Dictionary<string, string> backups = new();
        // Every data and scenario file of the mod as it was: the rename's undo must leave each one byte for byte.
        private readonly Dictionary<string, string> allFiles = new();
        private readonly Dictionary<string, string> itemFiles = new();
        private int passed;
        private int step;
        private double elapsed;
        private ItemEditorWindow window;
        private RpgEditorWindow rpgWindow;

        /// <summary>Creates the driver; <paramref name="shotsDir"/> is unused (no screenshots).</summary>
        public ItemEditorTestDriver(string shotsDir)
        {
            Name = "ItemEditorTestDriver";
            var timer = new Timer { WaitTime = TickSeconds, Autostart = true };
            AddChild(timer);
            timer.Timeout += Tick;
        }

        private void Tick()
        {
            if (step < 0)
                return;
            elapsed += TickSeconds;
            if (elapsed > TimeoutSeconds)
            {
                Fail($"timeout at step {step}");
                Finish();
                return;
            }
            try
            {
                RunStep();
            }
            catch (System.Exception ex)
            {
                Fail($"step {step}: {ex.GetType().Name}: {ex.Message}");
                Finish();
            }
        }

        private void RunStep()
        {
            switch (step)
            {
                case 0:
                    if (!Boot.IsReady)
                        return;
                    foreach (var file in ModFileService.ListFiles(Mod, "locale", ".properties"))
                        backups[file] = ModFileService.ReadText(Mod, file);
                    foreach (var file in ModFileService.ListFiles(Mod, "items", ".json"))
                        itemFiles[file] = ModFileService.ReadText(Mod, file);
                    foreach (var file in ModFileService.ListFiles(Mod, string.Empty, ".json").Concat(ModFileService.ListFiles(Mod, string.Empty, ".scenario")))
                        allFiles[file] = ModFileService.ReadText(Mod, file);
                    Check(allFiles.ContainsKey(HerbalistScenario), "the mod's data and scenario files are listed");
                    Check(!itemFiles.ContainsKey(NewFile), "the mod has no editor items file yet");
                    window = ToolWindow.Open<ItemEditorWindow>();
                    step++;
                    return;
                case 1:
                    if (window.Document == null)
                        return;
                    window.OpenMod(Mod);
                    var language = Language();
                    var branches = window.Document.Find("rpg_plus/branches");
                    Check(branches != null, "the demo's items are listed");
                    window.Select(branches);
                    Check(window.Document.GetText(language, "rpg_plus/item/branches/name") == TranslationServer.Translate("rpg_plus/item/branches/name").ToString(),
                        $"the form shows the item's name in '{language}'");
                    PlaceOnMap();

                    Press("tools/entity_editor/new");
                    Check(window.SelectedId == NewId, $"New adds and selects {NewId} (got {window.SelectedId})");
                    Type(NameEdit(), TypedName);
                    var cost = window.FindChildren("*", "", true, false).OfType<SpinBox>().First();
                    cost.Value = 7;
                    var food = window.FindChildren("*", "", true, false).OfType<CheckBox>().First(c => c.Text == nameof(ItemType.Food));
                    food.ButtonPressed = true;
                    var record = window.Document.Find(NewId);
                    Check(record.Data["Cost"]?.ToString() == "7" && record.Data["ItemType"]?.ToString().Split(',').OrderBy(t => t).SequenceEqual(new[] { "Food", "Resource" }) == true, "the form writes price and types");
                    // Plan 14.G5: Equip (the equipment mod's field) is a picker of the equip types the slots accept.
                    var equip = window.FindChild("Picker_Equip", true, false) as OptionButton;
                    Check(equip != null, "Equip is a picker in the item form");
                    var sword = equip == null ? -1 : Enumerable.Range(0, equip.ItemCount).FirstOrDefault(i => equip.GetItemText(i) == "sword", -1);
                    Check(sword >= 0, "the picker offers the sword of the equipment slots");
                    if (sword >= 0)
                    {
                        equip.Select(sword);
                        equip.EmitSignal(OptionButton.SignalName.ItemSelected, sword);
                    }
                    Check(window.Document.Find(NewId)?.Data["Equip"]?.ToString() == "sword", "the picked equip type is written as Equip");

                    Press("tools/entity_editor/duplicate");
                    Check(window.Document.Find(NewId + "_2") != null, "Copy adds a copy under a free id");
                    window.PushInput(new InputEventKey { Keycode = Key.Z, PhysicalKeycode = Key.Z, CtrlPressed = true, Pressed = true });
                    Check(window.Document.Find(NewId + "_2") == null && window.Document.Find(NewId) != null, "Ctrl+Z takes the copy back");
                    step++;
                    return;
                case 2:
                    try
                    {
                        PlaceOnMapWithoutPickup();
                        Press("tools/common/save");
                        var items = Entities.Collect<ItemInfo>();
                        Check(items.TryGetValue(NewId, out var saved), "the game knows the new item right after saving");
                        Check(saved?.Cost == 7 && saved.ItemType.HasFlag(ItemType.Food) && saved.ItemType.HasFlag(ItemType.Resource), "with the typed price and types");
                        Check(TranslationServer.Translate(saved?.Name ?? "-").ToString() == TypedName, $"and the typed name ({saved?.Name})");
                        var written = ModFileService.ReadText(Mod, NewFile) ?? string.Empty;
                        Check(written.Contains("\"Kind\": \"item\"") && !written.Contains("\r") && written.Contains(TypedName) == false, "the new file declares its kind, has LF lines and no texts");
                        Check(itemFiles.All(f => ModFileService.ReadText(Mod, f.Key) == f.Value), "the demo's item files are untouched");
                        RenameResin();
                    }
                    finally
                    {
                        Cleanup();
                    }
                    Finish();
                    return;
            }
        }

        /// <summary>"Place on map" (plan 12.5): the RPG map editor opens on a map of the mod with the item's pickup chosen.</summary>
        private void PlaceOnMap()
        {
            Press("tools/items_editor/place_on_map");
            rpgWindow = GetTree().Root.FindChildren("*", "", true, false).OfType<RpgEditorWindow>().FirstOrDefault();
            Check(rpgWindow != null, "Place on map opens the RPG map editor");
            Check(rpgWindow?.Document?.MapId != null, "the RPG map editor opens a map of the mod");
            Check(rpgWindow?.Tool == RpgEditorTool.Place && rpgWindow.PlaceObject == "rpg_plus/pickup/branches",
                $"the item's pickup is ready to place (got {rpgWindow?.PlaceObject})");
        }

        /// <summary>
        /// Plan 14.G6: a saved item with references is renamed through the Rename button. The dialog lists the files of the
        /// mod that name it (the item, a recipe, a scenario); Apply rewrites them; the game knows the new id only; the undo
        /// button puts every file of the mod back as it was.
        /// </summary>
        private void RenameResin()
        {
            window.Select(window.Document.Find(ResinItem));
            window.Rename();
            var dialog = GetTree().Root.FindChildren("*", "", true, false).OfType<EntityRenameDialog>().FirstOrDefault();
            Check(dialog != null, "Rename opens the rename dialog");
            if (dialog == null)
                return;
            Type(dialog.FindChild("NewId", true, false) as LineEdit, RenamedResin);
            dialog.ShowPreview();
            var labels = (dialog.FindChild("Changes", true, false) as VBoxContainer)?.GetChildren().OfType<Label>().Select(l => l.Text).ToList() ?? new List<string>();
            Check(labels.Any(l => l.Contains("items/woodland.json")) && labels.Any(l => l.Contains("recipes/woodland.json")),
                "the preview lists the item's own files that name it");
            Check(labels.Any(l => l.Contains("herbalist.scenario")), "and the scenario that gives it");
            Check(dialog.GetOkButton().Disabled == false, "Apply is enabled after the preview");
            dialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
            var items = Entities.Collect<ItemInfo>();
            Check(items.ContainsKey(RenamedResin) && !items.ContainsKey(ResinItem), "the game knows the renamed item and not the old id");
            var recipes = ModFileService.ReadText(Mod, "recipes/woodland.json") ?? string.Empty;
            Check(recipes.Contains("\"" + RenamedResin + "\"") && !recipes.Contains("\"" + ResinItem + "\""), "the recipe rows name the new id");
            var scenario = ModFileService.ReadText(Mod, HerbalistScenario) ?? string.Empty;
            Check(scenario.Contains("id=" + RenamedResin + " ") && !scenario.Contains("id=" + ResinItem + " "), "the scenario's action names the new id");
            Press("tools/rename/undo");
            var changed = allFiles.Where(f => f.Value != null && ModFileService.ReadText(Mod, f.Key) != f.Value).Select(f => f.Key).ToList();
            Check(changed.Count == 0, $"undo writes every file back as it was ({string.Join(", ", changed)})");
            Check(Entities.Collect<ItemInfo>().ContainsKey(ResinItem), "the game knows the old id again");
        }

        private static string Language() => LanguageCode.Normalize(TranslationServer.GetLocale()) ?? "ru";

        /// <summary>The first text row of the current language: the name (the first locale field).</summary>
        private LineEdit NameEdit()
        {
            var label = string.Format(TranslationServer.Translate("tools/entity_editor/text_in"), Language());
            return window.FindChildren("*", "", true, false).OfType<Label>().First(l => l.Text == label && l.IsVisibleInTree())
                .GetParent().GetChildren().OfType<LineEdit>().First();
        }

        private static void Type(LineEdit edit, string text)
        {
            edit.Text = text;
            edit.EmitSignal(LineEdit.SignalName.TextChanged, text);
        }

        /// <summary>
        /// Plan 14.G4: an item with no pickup object gets one from its picture (written into the mod), the RPG map editor
        /// fits the map to its canvas and chooses the pickup, and a click on the map places it.
        /// </summary>
        private void PlaceOnMapWithoutPickup()
        {
            if (rpgWindow != null)
            {
                rpgWindow.GetParent()?.RemoveChild(rpgWindow);
                rpgWindow.QueueFree();
                rpgWindow = null;
            }
            window.Select(window.Document.Find(ResinItem));
            Press("tools/items_editor/place_on_map");
            rpgWindow = GetTree().Root.FindChildren("*", "", true, false).OfType<RpgEditorWindow>().FirstOrDefault();
            Check(rpgWindow != null && rpgWindow.PlaceObject == ResinPickup && rpgWindow.Tool == RpgEditorTool.Place,
                $"Place on map creates the missing pickup and chooses it (got {rpgWindow?.PlaceObject})");
            var written = ModFileService.GetWritablePath(Mod, PickupFile);
            Check(written != null && File.Exists(written), "the pickup object is written into the item's mod");
            Check(rpgWindow != null && Mathf.Abs(rpgWindow.Canvas.MapZoom - rpgWindow.Canvas.FitZoom()) < 0.001f,
                "the map is fitted to the canvas (the zoom is the fit zoom)");
            if (rpgWindow == null)
                return;
            var before = rpgWindow.Document.Placements.Count;
            var at = rpgWindow.Canvas.CanvasPointOf(new Vector2I(3, 3));
            rpgWindow.Canvas._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at });
            rpgWindow.Canvas._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at });
            var placed = rpgWindow.SelectedId;
            Check(rpgWindow.Document.Placements.Count == before + 1 && placed != null
                && rpgWindow.Document.Placements.Any(r => r.Id == placed && r.Data["Object"].GetValue<string>() == ResinPickup),
                "a click places the new pickup on the map");
        }

        private void Cleanup()
        {
            window?.QueueFree();
            if (rpgWindow != null)
            {
                rpgWindow.GetParent()?.RemoveChild(rpgWindow);
                rpgWindow.QueueFree();
            }
            var path = ModFileService.GetWritablePath(Mod, NewFile);
            if (path != null && File.Exists(path))
                File.Delete(path);
            var pickup = ModFileService.GetWritablePath(Mod, PickupFile);
            if (pickup != null && File.Exists(pickup))
                File.Delete(pickup);
            ModManager.Instance.GetResolver<PatchResolver>()?.ClearCache();
            ModManager.Instance.GetResolver<FileListResolver>()?.ClearCache();
            SceneManager.ReloadContent();
            foreach (var backup in backups)
                ModFileService.WriteText(Mod, backup.Key, backup.Value);
            foreach (var file in allFiles.Where(f => f.Value != null && ModFileService.ReadText(Mod, f.Key) != f.Value))
                ModFileService.WriteText(Mod, file.Key, file.Value);
            ModManager.Instance.GetResolver<EntityResolver>()?.ClearCache();
            LocaleFileService.ReloadGame();
        }

        private void Press(string key)
        {
            var text = TranslationServer.Translate(key);
            var button = window.FindChildren("*", "", true, false).OfType<Button>().FirstOrDefault(b => b.Text == text);
            if (button == null)
            {
                Fail($"button '{text}' not found");
                return;
            }
            button.EmitSignal(BaseButton.SignalName.Pressed);
        }

        private void Check(bool condition, string what)
        {
            if (condition)
            {
                passed++;
                GameLogger.Log("TEST", $"[PASS] {what}");
            }
            else
            {
                Fail(what);
            }
        }

        private void Fail(string what)
        {
            failures.Add(what);
            GameLogger.Log("TEST", $"[FAIL] {what}", LogLevel.Error);
        }

        private void Finish()
        {
            step = -1;
            GameLogger.Log("TEST", $"[TEST_RESULT] name=items_editor_edit passed={passed} failed={failures.Count}");
            Cthangover.Core.Bootstrap.ApplicationExit.Quit(GetTree(), failures.Count == 0 ? 0 : 1);
        }
    }
}
