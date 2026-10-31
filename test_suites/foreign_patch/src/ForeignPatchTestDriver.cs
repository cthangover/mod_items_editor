namespace Cthangover.Tools.ItemsEditor.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Items;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Interface.Tool;
    using Cthangover.Tools.EntityEditor;
    using Cthangover.Tools.Services;
    using Godot;

    /// <summary>
    /// Edits another mod's item the way an author does — the list, the form's price field, Delete
    /// with its confirmation, "Reset to inherited", Save — and asks the game after every save.
    /// The edited mod is rpg_plus_game, the item is classic_rpg_pack's dagger (a dependency of
    /// rpg_plus_game): the change must land in rpg_plus_game's patch file as the minimal difference,
    /// the owner's file must stay byte for byte, and the game must see the patched price, then no
    /// dagger ($delete), then the owner's price again with the patch file gone. Whatever the outcome,
    /// the patch file and rpg_plus_game's locale files are restored.
    /// Result line: <c>[TEST_RESULT] name=editor_foreign_patch passed=N failed=M</c>.
    /// </summary>
    public partial class ForeignPatchTestDriver : Node
    {
        private const float TickSeconds = 0.15f;
        private const float TimeoutSeconds = 90f;
        private const string Mod = "rpg_plus_game";
        private const string Owner = "classic_rpg_pack";
        private const string OwnerFile = "items/classic/classic_items.json";
        private const string PatchFile = "patches/item/rpg_plus_game.json";
        private const string Dagger = "classic/dagger";

        private readonly List<string> failures = new();
        private readonly Dictionary<string, string> backups = new();
        private string ownerText;
        private string patchBackup;
        private int passed;
        private int step;
        private double elapsed;
        private ItemEditorWindow window;

        /// <summary>Creates the driver ticking on a timer.</summary>
        public ForeignPatchTestDriver()
        {
            Name = "ForeignPatchTestDriver";
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
                    patchBackup = ModFileService.ReadText(Mod, PatchFile);
                    ownerText = ModFileService.ReadText(Owner, OwnerFile);
                    Check(ownerText != null && ownerText.Contains(Dagger), "the owner's file holds the dagger");
                    Check(patchBackup == null, "rpg_plus_game has no item patch yet");
                    Check(Entities.Collect<ItemInfo>().TryGetValue(Dagger, out var original) && original.Cost == 18, "the game starts with the owner's price 18");
                    window = ToolWindow.Open<ItemEditorWindow>();
                    step++;
                    return;
                case 1:
                    if (window.Document == null)
                        return;
                    window.OpenMod(Mod);
                    EditPrice();
                    step++;
                    return;
                case 2:
                    Press("tools/common/save");
                    CheckSavedPrice();
                    DeleteDagger();
                    step++;
                    return;
                case 3:
                    ResetDagger();
                    step++;
                    return;
                case 4:
                    Cleanup();
                    Finish();
                    return;
            }
        }

        private void EditPrice()
        {
            var dagger = window.Document.FindForeign(Dagger);
            Check(dagger != null && dagger.OwnerMod == Owner, $"the dagger is listed as {Owner}'s entry ({dagger?.OwnerMod})");
            Check(window.Document.Find(Dagger) == null, "it is not one of rpg_plus_game's own entries");
            window.Select(dagger);
            Check(window.SelectedId == Dagger, "the dagger is selected");
            var cost = Row("tools/items_editor/cost").OfType<SpinBox>().First();
            Check((int)cost.Value == 18, $"the form shows the owner's price ({cost.Value})");
            Check(Visible().Any(l => l.Text.Contains(Owner) && l.Text.Contains(PatchFile)), "the form says whose entry it is and where the edit goes");

            cost.Value = 25;
            var entry = window.Document.PatchOf(Dagger);
            Check(entry != null && entry.Count == 2 && entry["Cost"]?.ToString() == "25", $"the edit is a minimal patch entry ({entry?.ToJsonString()})");
            Check(window.Document.ChangedFiles().Count == 0, "no data file of the mod changes");
            Check(Badge("Cost").Text == TranslationServer.Translate("tools/entity_editor/badge_own_patch").ToString() && Badge("Cost").CanReset,
                $"the price is marked as the own patch's value ({Badge("Cost").Text})");
            Check(Badge("Sprite").Text == string.Empty, "an untouched field has no badge");
        }

        private void CheckSavedPrice()
        {
            var written = ModFileService.ReadText(Mod, PatchFile);
            var root = written == null ? null : JsonNode.Parse(written) as JsonObject;
            var entries = root?["Items"] as JsonArray;
            Check(entries?.Count == 1 && entries[0]?["Id"]?.ToString() == Dagger && entries[0]?["Cost"]?.ToString() == "25" && ((JsonObject)entries[0]).Count == 2,
                $"Save writes {PatchFile} with the minimal entry ({written?.Replace("\n", " ")})");
            Check(written != null && !written.Contains('\r') && written.EndsWith("\n"), "the patch file has LF lines");
            Check(ModFileService.ReadText(Owner, OwnerFile) == ownerText, "the owner's file is byte for byte the same");
            var items = Entities.Collect<ItemInfo>();
            Check(items.TryGetValue(Dagger, out var saved) && saved.Cost == 25, $"the game sees the patched price at once ({saved?.Cost})");
            Check(saved?.ExtraProperties != null && saved.ExtraProperties.ContainsKey("Equip") && saved.ExtraProperties.ContainsKey("Traits"), "the owner's other fields stay");
            var explained = ForeignEntities.Explain("item", Dagger);
            Check(explained != null && explained.Fields.TryGetValue("Cost", out var origin) && origin.Layer == EntityLayer.Patch && origin.ModId == Mod && origin.File == PatchFile,
                "Explain names rpg_plus_game's patch as the price's source");
            Check(explained != null && explained.Fields.TryGetValue("Sprite", out var sprite) && sprite.Layer == EntityLayer.Definition && sprite.ModId == Owner,
                "and the owner's file as the picture's");
        }

        private void DeleteDagger()
        {
            Press("tools/entity_editor/delete");
            var confirm = window.GetChildren().OfType<ConfirmationDialog>().LastOrDefault();
            Check(confirm != null, "Delete asks first");
            confirm?.EmitSignal(AcceptDialog.SignalName.Confirmed);
            Check(window.Document.IsDeleted(window.Document.FindForeign(Dagger)), "the dagger is deleted by the patch");
            Press("tools/common/save");
            var written = ModFileService.ReadText(Mod, PatchFile) ?? string.Empty;
            Check(written.Contains("\"$delete\": true") && !written.Contains("\"Cost\""), "the patch entry says $delete instead of the price");
            Check(!Entities.Collect<ItemInfo>().ContainsKey(Dagger), "the game drops the dagger");
            Check(ModFileService.ReadText(Owner, OwnerFile) == ownerText, "the owner's file is still the same");
        }

        private void ResetDagger()
        {
            var dagger = window.Document.FindForeign(Dagger);
            Check(dagger != null, "the deleted dagger stays in the list");
            window.Select(dagger);
            Press("tools/entity_editor/reset");
            Check(window.Document.PatchOf(Dagger) == null, "Reset to inherited removes the patch entry");
            Press("tools/common/save");
            Check(ModFileService.ReadText(Mod, PatchFile) == null, "the empty patch file is removed");
            var folder = ModFileService.GetWritablePath(Mod, "patches");
            Check(folder == null || !System.IO.Directory.Exists(folder), "with the folders it leaves empty");
            Check(Entities.Collect<ItemInfo>().TryGetValue(Dagger, out var back) && back.Cost == 18, $"the game sees the owner's price again ({back?.Cost})");
            Check(ModFileService.ReadText(Owner, OwnerFile) == ownerText, "the owner's file never changed");
        }

        private IEnumerable<Label> Visible()
            => window.FindChildren("*", "", true, false).OfType<Label>().Where(l => l.IsVisibleInTree());

        private FieldOriginBadge Badge(string key)
            => window.FindChildren("*", "", true, false).OfType<FieldOriginBadge>().First(b => b.Key == key);

        private IEnumerable<Node> Row(string labelKey)
        {
            var text = TranslationServer.Translate(labelKey).ToString();
            return window.FindChildren("*", "", true, false).OfType<Label>().First(l => l.Text == text).GetParent().FindChildren("*", "", true, false);
        }

        private void Cleanup()
        {
            window?.QueueFree();
            if (patchBackup != null)
                ModFileService.WriteText(Mod, PatchFile, patchBackup);
            else
                ModFileService.DeleteFile(Mod, PatchFile);
            foreach (var backup in backups)
            {
                if (ModFileService.ReadText(Mod, backup.Key) != backup.Value)
                    ModFileService.WriteText(Mod, backup.Key, backup.Value);
            }
            ModManager.Instance.GetResolver<PatchResolver>()?.ClearCache();
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
            if (step >= 0 && step < 4)
                Cleanup();
            step = -1;
            GameLogger.Log("TEST", $"[TEST_RESULT] name=editor_foreign_patch passed={passed} failed={failures.Count}");
            ApplicationExit.Quit(GetTree(), failures.Count == 0 ? 0 : 1);
        }
    }
}
