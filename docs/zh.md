# Items Editor — 用表单编辑模组的物品

Items Editor 在游戏运行时编辑模组的物品：各种语言的名称和描述、价格、图片、物品类型，以及从背包使用时的动作。**保存**会写入模组的文件，游戏立刻显示变化——无需重启，无需重新加载。

它从工具箱（工具栏上的锤子）打开，叫**物品**。

## 一次编辑及其在游戏中的结果

RPG Plus 的背包里有村里商店卖的药水：

![背包中选中药剂：Targeted，价格 10。](assets/bag-before.jpg)

在编辑器里：顶部选择模组 `rpg_plus_game`，在列表中选**药剂**，输入新的英文名称，按**保存**（Ctrl+S）：

![rpg_plus_game 上的「物品」窗口：列表中有 柴火、石头、森林蘑菇 和 Dawn elixir；rpg_demo/elixir 的表单，名称键为 rpg_demo/item/elixir/name，文本 (en) Dawn elixir，其余六种语言未变，描述有七种语言，价格 10，图标 rpg_demo/healing_elixir 及其图片，类型 TargetUsed，动作 action/heal。状态行：「已保存：模组 rpg_plus_game 中 0 个文件、1 条文本；游戏已重新读取数据」。](assets/editor-saved.jpg)

编辑器插图来自较早的截图，其中的 `rpg_demo/` 键或 `rpg_tileset` 面板属于旧版。请使用正文中的当前 `rpg_plus/` 键与 `rpg_object` 模型。

只有模组 `locale/en.properties` 的英文一行变了；物品文件没有动。紧接着背包里的同一瓶药水：

![背包现在把该物品显示为 Dawn elixir，仍是 Targeted，价格 10。](assets/bag-after.jpg)

## 表单

- **Id** —— 物品的 id；剧本、商店、地图和其他模组都靠它找到物品。
- **名称**和**描述** —— 翻译键和模组每种语言的一行文字。新物品会得到由 id 组成的键；文字写入模组的本地化文件。
- **价格** —— 以金币计（商店可以自己定价）。
- **图标** —— 物品组的一张图片；**…** 打开网格挑选。
- **类型** —— Quest、Used、TargetUsed、Food、Resource、Recipe、CantDrop；可以同时勾选多个。
- **动作** —— 使用物品时做什么：`null` 或已加载模组注册的某个物品动作（`action/heal`、`action/equip`…）。
- **其他模组的字段** —— 其他模组在各自模式中为物品描述的字段（例如 Equipment 的装备类型、特性和可用职业），会在单独区域以同样的编辑器出现。
- **其他字段** —— 物品 JSON 里的其余内容，按 JSON 显示和编辑；原样保留。

工具栏：**新建**（Ctrl+N）添加 `<mod>/new_item`——一个价值一枚金币、没有动作的资源；**复制**（Ctrl+D）、**删除**、**撤销**/**重做**（Ctrl+Z / Ctrl+Y）。保存只写文字有变化的文件；新物品进入 `items/<mod>.json`。

## 其他模组的物品

列表显示所选模组的物品。勾选 **Entries of other mods** 可加入其依赖模组的物品，并标记 `↗ 所有者`。编辑这类物品不会改动所有者的文件：修改以补丁形式保存在你的模组中（`patches/item/<你的模组>.json`），只包含你改动的值。**Reset to inherited** 删除补丁，**Delete** 写入 `$delete`，**Copy** 生成一个新 id 的自有物品。只能写入以文件夹形式安装的模组。

每个字段都会显示其值的来源：标记指出该值来自你的补丁、其他模组的补丁，还是通过 `Extends` 继承；**↺** 按钮删除你对该字段的值，使继承的值重新生效。

## 放到地图上

**Place on map** 会在该物品所在模组的第一个地图文件上打开 RPG 地图编辑器，并已选中该物品的拾取对象，准备放置。拾取对象是类别为 `pickup`、且 id 以物品名结尾的对象：对于 `rpg_plus/amber`，即 `rpg_plus/pickup/amber`。若不存在，则选择游戏中的第一个拾取对象，因此请在地图编辑器的调色板中确认选择。

![在 rpg_plus_game 的第一张地图 rpg_desert 上由 Place on map 打开的 RPG 地图编辑器：Sprites 标签中选中了 amber 拾取物，Place 工具已启用。](assets/place-on-map.jpg)

地图编辑器不会自动添加交互。选择实例，进入**交互 → 行为 → 给予物品**，选择物品和数量并保存。事件内联命令也可用 `action item.add id=<item> count=1`。一次性拾取需要保存标志及状态或事件条件；仅选择给予物品会在每次使用时重复奖励。

## 出问题了怎么办

1. **模组列表是空的。** 没有可写的模组；打包模组不能编辑。
2. **背包里还是旧名字。** 物品保存所用语言与游戏语言不同；检查游戏语言那一行。
3. **动作列表里缺一个动作。** 注册它的模组没有在这个游戏里加载。
