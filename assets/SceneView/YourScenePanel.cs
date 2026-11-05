using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// S2 シーンツールパネルの雛形。数値を作業しながら変えるパネル (DennokoMeshEditor の IMGUI パネルの置き換え)。
    /// 作業中だけ表示する (ITransientOverlay)。既定は右の列の下端。
    /// ドラッグ・画面内への収め込み・位置の記憶・折りたたみは Unity の Overlay に任せる (自作しない)。
    /// 「編集中」を枠の色で示さない。セッション行と「作業中だけ出る」ことで示す。
    /// </summary>
    [Overlay(typeof(SceneView), "YourTool.ScenePanel", "Your Tool", true,   // ← id と表示名 (Overlay の見出し) を変更する
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Bottom, defaultLayout = Layout.Panel)]
    internal sealed class YourScenePanel : Overlay, ITransientOverlay
    {
        private const string UXML_GUID = "YOUR_SCENE_PANEL_UXML_GUID_HERE";   // YourScenePanel.uxml の GUID

        public bool visible => YourSceneToolState.Active;

        public override VisualElement CreatePanelContent()
        {
            if (DennokoSceneUI.CreateOverlayRoot(UXML_GUID, out var root)) InitializeUI(root);
            return root;
        }

        private static void InitializeUI(VisualElement root)
        {
            var data = YourSceneToolState.Data;

            // ─── セッション行: 対象名クリックでフレーム、✕ は Esc と同じ終了 ─────
            var target = root.Q<Label>("session-target");
            target.RegisterCallback<ClickEvent>(_ => DennokoSceneSession.FrameTarget());
            var end = DennokoIcons.Apply(root.Q<Button>("session-end"), DennokoIcons.Close, "✕", "編集を終了  Esc");
            end.clicked += YourSceneToolState.End;

            // ─── コンパクトスライダー: 1 ドラッグ = Undo 1 段 ─────────────
            var radius = root.Q<Slider>("radius-slider");
            var radiusField = root.Q<FloatField>("radius-field");
            DennokoSceneUI.BindUndoSlider(radius, "半径", v => YourSceneToolState.Modify("半径", d => d.radius = v));
            radiusField.RegisterValueChangedCallback(e =>
                YourSceneToolState.Modify("半径", d => d.radius = Mathf.Clamp(e.newValue, radius.lowValue, radius.highValue)));

            // ─── 選択肢グループ (enum は 4 つまでこれ。ドロップダウンにしない) ────
            var setFalloff = DennokoBlockUI.BindChoice(root.Q("falloff-group"), data.falloff,
                i => YourSceneToolState.Modify("減衰", d => d.falloff = i));
            var setMirror = DennokoBlockUI.BindChoice(root.Q("mirror-group"), data.mirror,
                i => YourSceneToolState.Modify("ミラー", d => d.mirror = i));

            // ─── 1 回適用スライダー + 小見出しの ↺ ──────────────────────
            var smooth = root.Q<Slider>("effect-smooth");
            var flatten = root.Q<Slider>("effect-flatten");
            DennokoSceneUI.BindImpulseSlider(smooth, root.Q<Label>("effect-smooth-percent"), amount =>
            {
                // TODO: 選択に効果をかける (Undo に載せる)
                DennokoSceneFeedback.Toast($"滑らか {Mathf.RoundToInt(amount * 100)}% を適用", DennokoIcons.Success, atCursor: false);
            });
            DennokoSceneUI.BindImpulseSlider(flatten, root.Q<Label>("effect-flatten-percent"), amount =>
            {
                // TODO: 選択に効果をかける (Undo に載せる)
                DennokoSceneFeedback.Toast($"平坦化 {Mathf.RoundToInt(amount * 100)}% を適用", DennokoIcons.Success, atCursor: false);
            });
            var reset = DennokoIcons.Apply(root.Q<Button>("effects-reset"), DennokoIcons.Refresh, "↺", "効果を元に戻す");
            reset.clicked += () =>
            {
                // TODO: 元に戻す (Undo に載せる。Ctrl+Z で戻せるので確認ダイアログは出さない)
                DennokoSceneFeedback.Toast("効果を元に戻しました (Ctrl+Z で戻せます)", DennokoIcons.Info, atCursor: false);
            };

            // ─── キーヒント (常設は 2 組まで) / 警告行 ─────────────────
            var keyhints = root.Q("keyhints");
            var warning = root.Q("warning");
            var warningText = root.Q<Label>("warning-text");
            DennokoIcons.SetImage(root.Q("warning-icon"), DennokoIcons.Warning);

            void Refresh()
            {
                var d = YourSceneToolState.Data;
                target.text = DennokoSceneSession.TargetName;

                radius.SetValueWithoutNotify(d.radius);   // シーン側 (ホイール) で変えた値もここで反映される
                radiusField.SetValueWithoutNotify(Mathf.Round(d.radius * 1000f) / 1000f);
                setFalloff(d.falloff);
                setMirror(d.mirror);

                // 使えないときは無効にし、理由をツールチップに出す
                bool hasSelection = d.selection.Count > 0;
                foreach (var s in new[] { smooth, flatten })
                {
                    s.SetEnabled(hasSelection);
                    s.tooltip = hasSelection ? string.Empty : "頂点を選ぶと使えます";
                }

                keyhints.Clear();
                DennokoBlockUI.AddKeyHint(keyhints, "Wheel", "半径 (ドラッグ中)");
                DennokoBlockUI.AddKeyHint(keyhints, "Esc", hasSelection ? "選択を解除" : "終了");

                // 警告は必要なときだけ (30 字まで。全文はツールチップ)
                string message = null;   // TODO: 例 "NDMF プレビュー未取得"
                warningText.text = message ?? string.Empty;
                warning.tooltip = message;
                DennokoSceneUI.Show(warning, message != null);
            }

            DennokoSceneUI.RefreshWhileAttached(root.Q("panel"), Refresh,
                h => YourSceneToolState.Changed += h, h => YourSceneToolState.Changed -= h);
        }
    }
}
