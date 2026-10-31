namespace Cthangover.Tools.ItemsEditor.Tests
{
    using System.Linq;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Utils;
    using Godot;

    /// <summary>Adds the foreign patch test driver when <c>--itest=editor_foreign_patch</c> is given.</summary>
    public class ForeignPatchTestInitializer : IModInitializer
    {
        private static bool driverQueued;

        /// <summary>Nothing per mod.</summary>
        public void OnModLoaded(string modId)
        {
        }

        /// <summary>Queues the driver once.</summary>
        public void OnModResourcesReady()
        {
            if (driverQueued || !OS.GetCmdlineUserArgs().Any(a => a == "--itest=editor_foreign_patch"))
                return;

            driverQueued = true;
            GameLogger.Log("TEST", "ForeignPatchTestInitializer: activating editor_foreign_patch");
            ModRoot.AddDeferred(new ForeignPatchTestDriver());
        }
    }
}
