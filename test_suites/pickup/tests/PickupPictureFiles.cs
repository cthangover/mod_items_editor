namespace Cthangover.Tools.ItemsEditor.Tests.Pickup
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Tools.Services;

    /// <summary>Restores the fixture's object definition and derived picture bytes, including exceptional test exits.</summary>
    internal sealed class PickupPictureFiles : IDisposable
    {
        private const string Mod = "rpg_plus_game";

        private readonly Dictionary<string, byte[]> originals = new(StringComparer.Ordinal);

        internal PickupPictureFiles()
        {
            var paths = new[]
            {
                "objects/pickup_resin.json",
                "rpg_images/rpg_plus_game/pickup/resin.png",
            };
            foreach (var relative in paths)
            {
                var path = ModFileService.GetWritablePath(Mod, relative);
                originals.Add(path, File.Exists(path) ? File.ReadAllBytes(path) : null);
            }
        }

        /// <summary>Called after native windows release their borrowed textures, restoring only this fixture's named paths.</summary>
        public void Dispose()
        {
            foreach (var pair in originals)
            {
                if (pair.Value == null)
                {
                    File.Delete(pair.Key);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pair.Key));
                    File.WriteAllBytes(pair.Key, pair.Value);
                }
            }

            ModManager.Instance.GetResolver<EntityResolver>()?.ClearCache();
            ModManager.Instance.GetResolver<PatchResolver>()?.ClearCache();
            ModManager.Instance.GetResolver<FileListResolver>()?.ClearCache();
        }
    }
}
