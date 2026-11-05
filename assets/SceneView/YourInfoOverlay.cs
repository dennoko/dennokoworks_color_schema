using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// S6 情報オーバーレイの雛形 (HowManyPolygons のように常に見せたい読み取り専用の表示)。
    /// ITransientOverlay を付けない普通の Overlay にし、表示 / 非表示はユーザーが Overlay メニューで決める。
    /// 操作は置かない。設定はウィンドウ側か Preferences に置く。
    /// 例として、選択中のオブジェクトの三角形数を出す。
    /// </summary>
    [Overlay(typeof(SceneView), "YourTool.Info", "Your Tool 情報", true,   // ← id と表示名を変更する
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Top, defaultLayout = Layout.Panel)]
    internal sealed class YourInfoOverlay : Overlay
    {
        private const string UXML_GUID = "YOUR_INFO_OVERLAY_UXML_GUID_HERE";   // YourInfoOverlay.uxml の GUID
        private const long Good = 70000;    // ← 目安を変更する
        private const long Warn = 140000;

        public override VisualElement CreatePanelContent()
        {
            if (!DennokoSceneUI.CreateOverlayRoot(UXML_GUID, out var root)) return root;

            var box = root.Q("info");
            var value = root.Q<Label>("info-value");
            var sub = root.Q<Label>("info-sub");

            void Refresh()
            {
                var go = Selection.activeGameObject;
                if (go == null)
                {
                    value.text = "—";
                    sub.text = "オブジェクトを選ぶと三角形数を表示";
                    SetLevel(box, -1);
                    return;
                }
                long tris = 0;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) tris += Count(mf.sharedMesh);
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) tris += Count(smr.sharedMesh);
                value.text = tris.ToString("N0");
                sub.text = $"{go.name} · 三角形 (目安 {Good:N0} 以下)";
                SetLevel(box, tris <= Good ? 0 : tris <= Warn ? 1 : 2);
            }

            DennokoSceneUI.RefreshWhileAttached(box, Refresh,
                h => Selection.selectionChanged += h, h => Selection.selectionChanged -= h);
            return root;
        }

        private static long Count(Mesh mesh)
        {
            if (mesh == null) return 0;
            long n = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) n += mesh.GetIndexCount(i) / 3;
            return n;
        }

        // 段階の色は文字色だけに semantic 色を使う (背景を塗らない)
        private static void SetLevel(VisualElement box, int level)
        {
            box.EnableInClassList("dennoko-info-overlay--good", level == 0);
            box.EnableInClassList("dennoko-info-overlay--warn", level == 1);
            box.EnableInClassList("dennoko-info-overlay--bad", level == 2);
        }
    }
}
