using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// S3 ラジアルメニューのツール側 (雛形)。本体 DennokoRadialMenu はそのまま使い、ここに書くのは 3 つだけ:
    ///   1. メニュー本体を 1 つ作る (static readonly)
    ///   2. [ClutchShortcut] で Begin → OpenAtPointer() / End → Release()
    ///   3. 項目を返す関数 (常に 8 個。0 = →、1 = ↗、2 = ↑、3 = ↖、4 = ←、5 = ↙、6 = ↓、7 = ↘)
    /// ショートカットは作業中だけ有効なタグ付き (タグは DennokoSceneSession が作業中だけ RegisterTag する)。
    /// 詳細は blocks/radial-menu.md。
    /// </summary>
    [InitializeOnLoad]
    internal static class YourRadialMenu
    {
        public static readonly DennokoRadialMenu Menu = new DennokoRadialMenu(
            "YourTool-Radial", BuildItems, DennokoSceneUI.SetupRoot)   // ← 名前は他のツールと重ならないものにする
        {
            CanOpen = () => YourSceneToolState.Active,
        };

        static YourRadialMenu()
        {
            Menu.Executed += YourSceneToolState.Raise;   // 実行後にツールバーなどの表示を読み直す
        }

        // 押している間だけ効くキー。タグ付きなので作業中以外は Unity の既定 (C) に戻る
        [ClutchShortcut("Your Tool/Radial Menu (Hold)", typeof(SceneView), YourSceneToolState.ShortcutTag, KeyCode.C)]
        private static void Shortcut(ShortcutArguments args)
        {
            if (args.stage == ShortcutStage.Begin) Menu.OpenAtPointer();
            else Menu.Release();
        }

        // 作業中だけ効く普通のショートカットも同じタグで登録する
        [Shortcut("Your Tool/Toggle Symmetry", typeof(SceneView), YourSceneToolState.ShortcutTag, KeyCode.X)]
        private static void SymmetryShortcut()
        {
            YourSceneToolState.Symmetry = !YourSceneToolState.Symmetry;
            YourSceneToolState.Raise();
        }

        [Shortcut("Your Tool/Toggle Mode", typeof(SceneView), YourSceneToolState.ShortcutTag, KeyCode.Tab)]
        private static void ModeShortcut()
        {
            YourSceneToolState.PaintMode = !YourSceneToolState.PaintMode;
            YourSceneToolState.Raise();
        }

        // ─── 項目 (ツールに合わせて書き換える部分) ─────────────────────

        private static DennokoRadialMenu.Item[] BuildItems()
        {
            var items = new DennokoRadialMenu.Item[DennokoRadialMenu.SliceCount];
            bool paint = YourSceneToolState.PaintMode;

            // ↑ モード切替・↗ 対称・↖ クリアは全モード共通 (方向を手が覚えられるように)
            items[2] = new DennokoRadialMenu.Item
            {
                Name = paint ? "選択へ" : "ペイントへ", Icon = paint ? "d_Grid.Default|d_RectTool" : "d_Grid.PaintTool",
                Fallback = paint ? "選" : "塗", Key = "Tab", Run = () => YourSceneToolState.PaintMode = !paint,
            };
            items[1] = new DennokoRadialMenu.Item
            {
                Name = "対称", Icon = "d_align_horizontally_center", Fallback = "⇋", Key = "X",
                Run = () => YourSceneToolState.Symmetry = !YourSceneToolState.Symmetry, Active = YourSceneToolState.Symmetry,
            };
            items[3] = new DennokoRadialMenu.Item
            {
                Name = "クリア", Icon = "d_TreeEditor.Trash", Fallback = "✕",
                Run = () => YourSceneToolState.Modify("クリア", d => d.selection.Clear()),
            };

            if (!paint)
            {
                items[0] = Unit("島", 0, "1");
                items[7] = Unit("パーツ", 1, "2");
                items[6] = Unit("面", 2, "3");
                // 4 / 5 は空き。Run を入れなければ薄く表示されて選べない
            }
            else
            {
                items[0] = Tool("ブラシ", 0, "d_Grid.PaintTool", "B");
                items[7] = Tool("矩形", 1, "d_RectTool", "M");
                items[6] = Tool("投げ縄", 2, null, "L");
                items[5] = Tool("消しゴム", 3, "d_Grid.EraserTool", "Shift+B");
            }
            return items;
        }

        private static DennokoRadialMenu.Item Unit(string name, int value, string key) => new DennokoRadialMenu.Item
        {
            Name = name, Fallback = name.Substring(0, 1), Key = key, Active = YourSceneToolState.Unit == value,
            Run = () => YourSceneToolState.Unit = value,
        };

        private static DennokoRadialMenu.Item Tool(string name, int value, string icon, string key) => new DennokoRadialMenu.Item
        {
            Name = name, Icon = icon, Fallback = key.Substring(key.Length - 1), Key = key, Active = YourSceneToolState.PaintTool == value,
            Run = () => YourSceneToolState.PaintTool = value,
        };
    }
}
