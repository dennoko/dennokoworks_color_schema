using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// S1 シーンツールバー (カプセル) の雛形。作業中だけ表示する (ITransientOverlay)。
    /// 既定は SceneView の左の列の下端。ユーザーが動かした位置は Unity が覚える。
    /// 状態は持たず、YourSceneToolState.Changed で読み直す。
    /// ボタンはアイコンのみ。名前とキーはツールチップに「名前  キー」で出す。ボタンは 12 個まで。
    /// </summary>
    [Overlay(typeof(SceneView), "YourTool.SceneToolbar", "Your Tool ツールバー", true,   // ← id と表示名を変更する
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Bottom, defaultLayout = Layout.Panel)]
    internal sealed class YourSceneToolbar : Overlay, ITransientOverlay
    {
        private const string UXML_GUID = "YOUR_SCENE_TOOLBAR_UXML_GUID_HERE";   // YourSceneToolbar.uxml の GUID

        public bool visible => YourSceneToolState.Active;

        // ドック位置の変更や SceneView の作り直しのたびに呼ばれるので、毎回作り直せる形にする
        public override VisualElement CreatePanelContent()
        {
            if (DennokoSceneUI.CreateOverlayRoot(UXML_GUID, out var root)) InitializeUI(root);
            return root;
        }

        private static void InitializeUI(VisualElement root)
        {
            const string Active = DennokoSceneUI.ActiveClass;

            Button B(string name, string icon, string fallback, string tooltip, System.Action click)
            {
                var b = DennokoIcons.Apply(root.Q<Button>(name), icon, fallback, tooltip);
                if (b != null && click != null) b.clicked += () => { click(); YourSceneToolState.Raise(); };
                return b;
            }

            // モード (同じ位置の中身をモードで差し替える)
            var modeSelect = B("mode-select", "d_Grid.Default|d_RectTool", "選", "選択  Tab", () => YourSceneToolState.PaintMode = false);
            var modePaint  = B("mode-paint", "d_Grid.PaintTool", "塗", "ペイント  Tab", () => YourSceneToolState.PaintMode = true);

            // 文字の選択肢 (.dennoko-choice、1 ボタン 3 字まで)
            var units = new[] { root.Q<Button>("unit-island"), root.Q<Button>("unit-part"), root.Q<Button>("unit-face") };
            for (int i = 0; i < units.Length; i++)
            {
                int value = i;
                units[i].clicked += () => { YourSceneToolState.Unit = value; YourSceneToolState.Raise(); };
            }

            var tools = new[]
            {
                B("tool-brush", "d_Grid.PaintTool", "B", "ブラシ  B", () => YourSceneToolState.PaintTool = 0),
                B("tool-rect", "d_RectTool", "M", "矩形  M", () => YourSceneToolState.PaintTool = 1),
                B("tool-lasso", "d_Grid.FillTool", "L", "投げ縄  L", () => YourSceneToolState.PaintTool = 2),
                B("tool-eraser", "d_Grid.EraserTool", "E", "消しゴム  Shift+B", () => YourSceneToolState.PaintTool = 3),
            };

            var symmetry = B("symmetry", "d_align_horizontally_center", "⇋", "左右対称  X", () => YourSceneToolState.Symmetry = !YourSceneToolState.Symmetry);
            var xray = B("xray", "d_scenevis_visible_hover|d_scenevis_visible", "透", "X-Ray (裏の面も選べる)", () => YourSceneToolState.XRay = !YourSceneToolState.XRay);
            B("undo", "d_back|d_Animation.PrevKey", "↶", "元に戻す  Ctrl+Z", Undo.PerformUndo);
            B("redo", "d_forward|d_Animation.NextKey", "↷", "やり直す  Ctrl+Y", Undo.PerformRedo);
            B("clear", "d_TreeEditor.Trash", "✕", "選択をすべて外す", () => YourSceneToolState.Modify("クリア", d => d.selection.Clear()));

            var export = DennokoIcons.Apply(root.Q<Button>("export"), DennokoIcons.Save, "↓", "出力  Ctrl+Enter");
            export.clicked += () =>
            {
                // TODO: 出力する
                DennokoSceneUI.FlashSuccess(export);
                // シーンから実行したので、通知はカーソル脇のトーストに出す (ウィンドウのステータスバーには出さない)
                DennokoSceneFeedback.Toast("Output.png", DennokoIcons.Save);
            };

            var segUnits = root.Q("seg-units");
            var segTools = root.Q("seg-tools");
            var count = root.Q<Label>("count");

            // 状態 → 表示。選択中・ON は selection 色の枠 (.dennoko-button-active)
            void Refresh()
            {
                bool paint = YourSceneToolState.PaintMode;
                modeSelect.EnableInClassList(Active, !paint);
                modePaint.EnableInClassList(Active, paint);
                DennokoSceneUI.Show(segUnits, !paint);
                DennokoSceneUI.Show(segTools, paint);
                for (int i = 0; i < units.Length; i++) units[i].EnableInClassList(Active, YourSceneToolState.Unit == i);
                for (int i = 0; i < tools.Length; i++) tools[i].EnableInClassList(Active, YourSceneToolState.PaintTool == i);
                symmetry.EnableInClassList(Active, YourSceneToolState.Symmetry);
                xray.EnableInClassList(Active, YourSceneToolState.XRay);
                count.text = $"{YourSceneToolState.Data.selection.Count}/{YourSceneToolState.TotalCount}";
            }

            DennokoSceneUI.RefreshWhileAttached(root.Q("capsule"), Refresh,
                h => YourSceneToolState.Changed += h, h => YourSceneToolState.Changed -= h);
        }
    }
}
