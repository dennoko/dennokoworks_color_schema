using UnityEditor;
using UnityEngine;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// シーンの直接操作 (クリック・ドラッグ・ホイール) と S5 シーン描画の雛形。
    /// 作業の本体はここに書く。設定値の変更はパネル / ツールバーに置き、ここでは扱わない (ホイールで半径だけは例外)。
    ///
    /// 守ること:
    ///   - Alt + ドラッグ・中ボタン・右ボタンのドラッグはカメラ操作なので消費しない
    ///   - AddDefaultControl でクリックが別のオブジェクトの選択にならないようにする
    ///   - ラジアルメニューが開いている間は入力を止め、描画だけ続ける
    ///   - クリックしたら DennokoSceneSession.MarkInteracted() (迷い検知のヒントを止める)
    ///   - 色と線は DennokoSceneColors (黒の外線 + 色の内線の 2 重線)
    /// </summary>
    [InitializeOnLoad]
    internal static class YourSceneInput
    {
        private static bool _pointerDown;

        static YourSceneInput()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView view)
        {
            if (!YourSceneToolState.Active) return;
            var e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);   // クリックで別のオブジェクトを選ばない

            // ラジアルが開いている間はクリックをラジアルに任せる (Esc は DennokoSceneSession が IsInputBlocked で見送る)
            if (YourRadialMenu.Menu.IsOpen && e.type != EventType.Repaint) return;

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) view.Repaint();

            // Shift = 足す / Ctrl = 外す をカーソル脇に出す
            DennokoSceneFeedback.SetModifier(view, e.mousePosition, e.shift, e.control || e.command);

            bool hasPoint = TryGetPoint(e.mousePosition, out var point, out var normal);
            bool camera = e.alt || e.button != 0;   // カメラ操作は通す

            if (!camera)
            {
                switch (e.type)
                {
                    case EventType.MouseDown when hasPoint:
                        GUIUtility.hotControl = id;
                        _pointerDown = true;
                        DennokoSceneSession.MarkInteracted();
                        // TODO: クリックした位置で選択 / 塗り (Undo に載せる: YourSceneToolState.Modify)
                        e.Use();
                        break;
                    case EventType.MouseDrag when _pointerDown:
                        // TODO: ドラッグ中の処理
                        e.Use();
                        break;
                    case EventType.MouseUp when _pointerDown:
                        GUIUtility.hotControl = 0;
                        _pointerDown = false;
                        e.Use();
                        break;
                }
            }

            // 押したままホイールで半径 (押していないときのホイールはズームなので通す)
            if (e.type == EventType.ScrollWheel && _pointerDown)
            {
                float r = Mathf.Clamp(YourSceneToolState.Data.radius * (e.delta.y > 0 ? 0.9f : 1.1f), 0.01f, 0.3f);
                YourSceneToolState.Modify("半径", d => d.radius = r);
                e.Use();
            }

            if (e.type == EventType.Repaint && hasPoint) Draw(point, normal, e.control || e.command);
        }

        private static void Draw(Vector3 point, Vector3 normal, bool removing)
        {
            float radius = YourSceneToolState.Data.radius;
            // ブラシ円: 半径は白 0.8 の実線、減衰の外側は白 0.3。「外す」予告は赤の点線
            if (removing)
            {
                DennokoSceneColors.Circle(point, normal, radius, DennokoSceneColors.HoverRemove, DennokoSceneColors.Bold, dotted: true);
            }
            else
            {
                DennokoSceneColors.Circle(point, normal, radius, DennokoSceneColors.BrushInner);
                DennokoSceneColors.Circle(point, normal, radius * 1.4f, DennokoSceneColors.BrushOuter, DennokoSceneColors.Thin);
            }
            // ミラー側は本体と同じ色の点線
            if (YourSceneToolState.Data.mirror == 1)
            {
                var mirrored = new Vector3(-point.x, point.y, point.z);
                DennokoSceneColors.Circle(mirrored, new Vector3(-normal.x, normal.y, normal.z), radius, DennokoSceneColors.BrushInner, DennokoSceneColors.Thin, dotted: true);
            }
        }

        // TODO: 対象のメッシュとの交点を求める。雛形では対象の位置を通る、カメラ向きの平面を使う
        private static bool TryGetPoint(Vector2 gui, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = Vector3.forward;
            var target = DennokoSceneSession.Target;
            var cam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
            if (target == null || cam == null) return false;
            normal = -cam.transform.forward;
            var plane = new Plane(normal, target.transform.position);
            var ray = HandleUtility.GUIPointToWorldRay(gui);
            if (!plane.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }
    }
}
