namespace Cthangover.Tools.ItemsEditor
{
    using System;
    using System.IO;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Core.Utils;
    using Cthangover.Tools.Services;

    /// <summary>Owns a new map-picture copy until the pickup definition is written; existing identical pictures remain borrowed.</summary>
    internal sealed class ItemPickupPicture : IDisposable
    {
        private readonly string path;

        private readonly bool created;

        private bool committed;

        internal string Id { get; }

        private ItemPickupPicture(string path, string id, bool created)
        {
            this.path = path;
            Id = id;
            this.created = created;
        }

        internal static ItemPickupPicture Prepare(string mod, string sprite, string id, out string problem)
        {
            problem = "tools/items_editor/place_no_art";
            using var image = TextureUtils.LoadImageFromModGroup("items", sprite);
            if (image == null || image.IsEmpty())
            {
                return null;
            }

            var key = ModPath.Normalize(sprite).ToLowerInvariant();
            if (key.StartsWith("items/", StringComparison.Ordinal))
            {
                key = key.Substring("items/".Length);
            }

            var files = Files.Collect("items");
            var index = new TextureFileIndex(files.Keys, ModConfig.Instance.GetTextureExtensionSet());
            var matched = index.Find(key);
            if (matched == null || !files.TryGetValue(matched, out var entry))
            {
                return null;
            }

            var bytes = ModManager.Instance.ReadFileBinary(entry.ModId, entry.FullPath);
            if (bytes == null)
            {
                return null;
            }

            var path = ModFileService.GetWritablePath(mod, "rpg_images/" + id + Path.GetExtension(matched));
            problem = "tools/common/readonly";
            if (path == null)
            {
                return null;
            }

            string temporary = null;
            try
            {
                if (File.Exists(path))
                {
                    if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
                    {
                        problem = "tools/items_editor/pickup_picture_conflict";
                        return null;
                    }

                    return new ItemPickupPicture(path, id, false);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, path);
                temporary = null;
                return new ItemPickupPicture(path, id, true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                return null;
            }
            finally
            {
                if (temporary != null && File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        internal void Commit()
        {
            committed = true;
        }

        /// <summary>
        /// Rolls back only a new uncommitted copy; the source inventory picture and preexisting assets are never deleted.
        /// </summary>
        public void Dispose()
        {
            if (created && !committed)
            {
                File.Delete(path);
                ModManager.Instance.GetResolver<FileListResolver>()?.ClearCache();
            }
        }
    }
}
