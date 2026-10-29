using Cthangover.Core.UI.Tool;
using Godot;

namespace Cthangover.Tools.ItemsEditor
{
    /// <summary>
    /// Registers the item editor window opened from the in-game toolbar
    /// through <see cref="ItemEditorToolBoxButton"/>.
    /// </summary>
    public class ItemEditorToolProvider : IToolProvider
    {
        /// <summary>Tool id.</summary>
        public string Id => "items_editor";

        /// <summary>Display name key.</summary>
        public string LocaleKey => "tools/items_editor/title";

        /// <summary>Creates the window.</summary>
        public Window CreateWindow() => new ItemEditorWindow();
    }
}
