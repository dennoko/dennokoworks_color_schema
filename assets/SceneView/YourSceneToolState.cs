using System;
using UnityEditor;
using UnityEngine;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// ツールの状態を 1 か所に持つクラス (雛形)。ウィンドウ・ツールバー (S1)・パネル (S2)・ラジアル (S3)・
    /// ショートカット・シーン入力は、どれもここを書き換えて Raise() し、Changed を受けて表示を読み直す。
    /// UI 側に状態のコピーを持たない (どこで変えても全部が追従するように)。
    ///
    /// 開始 / 終了の約束 (自動終了・同時に 1 つ・ショートカットのタグ・ハンドルを隠す) は DennokoSceneSession が守る。
    /// ここではツール固有の部分 (保存・Esc で選択を解除) だけを書く。
    /// </summary>
    [InitializeOnLoad]
    internal static class YourSceneToolState
    {
        public const string ToolId      = "YourTool";                       // ← 変更する
        public const string DisplayName = "Your Tool";                      // ← 変更する
        public const string ShortcutTag = "Dennokoworks.YourTool.Editing";  // ← 変更する (ツールごとに別の名前)

        /// <summary>値が変わった・作業を始めた / 終えた。表示はこれで読み直す。</summary>
        public static event Action Changed;

        /// <summary>このツールで作業中か (別のツールのセッション中は false)。</summary>
        public static bool Active => DennokoSceneSession.Active && DennokoSceneSession.ToolId == ToolId;

        // ─── 作業中に切り替える値 (Undo に載せないもの) ───────────────
        // ツールバー (S1) の例: モード / 選択の単位 / ペイントツール / トグル
        public static bool PaintMode;
        public static int Unit = 1;        // 0 島 / 1 パーツ / 2 面
        public static int PaintTool;       // 0 ブラシ / 1 矩形 / 2 投げ縄 / 3 消しゴム
        public static bool Symmetry = true;
        public static bool XRay;
        public static int TotalCount;      // 選べるものの総数 (ツールバー右端の「12/480」)

        // ─── Undo に載せる値 ─────────────────────────────────────────
        private static YourSceneToolData _data;

        public static YourSceneToolData Data
        {
            get
            {
                if (_data == null)
                {
                    _data = ScriptableObject.CreateInstance<YourSceneToolData>();
                    _data.hideFlags = HideFlags.HideAndDontSave;
                    _data.name = DisplayName + " Data";
                }
                return _data;
            }
        }

        static YourSceneToolState()
        {
            DennokoSceneSession.ActiveChanged += Raise;
            Undo.undoRedoPerformed += Raise;
        }

        // ─── 開始 / 終了 ─────────────────────────────────────────────

        /// <summary>ウィンドウ / Inspector の主ボタン、Hierarchy の右クリックから呼ぶ。シーンに開始ボタンを出さない。</summary>
        public static void Begin(GameObject target)
        {
            DennokoSceneSession.Begin(new DennokoSceneSession.Options
            {
                ToolId = ToolId,
                DisplayName = DisplayName,
                Target = target,
                ShortcutTag = ShortcutTag,
                OnEnd = OnEnd,
                HandleEscape = HandleEscape,
                // ラジアルが開いている間の Esc はラジアルを閉じるのに使う
                IsInputBlocked = () => YourRadialMenu.Menu.IsOpen,
            });
            DennokoSceneFeedback.LostHintText = () => PaintMode ? "ドラッグで塗る" : "クリックで選択";
            DennokoSceneSession.FrameTarget();
        }

        /// <summary>ウィンドウの「終了」、パネルの ✕ から呼ぶ (Esc は DennokoSceneSession が呼ぶ)。</summary>
        public static void End()
        {
            if (Active) DennokoSceneSession.End(DennokoSessionEndReason.User);
        }

        // どの終わり方でも必ず呼ばれる。作業を保存してから終わる
        private static void OnEnd(DennokoSessionEndReason reason)
        {
            // TODO: 作業の保存 (コンポーネントへの書き戻し・ファイル出力など)
            YourRadialMenu.Menu.Close(false);
        }

        // Esc: 選択があればまず解除する (true = 終了しない)。選択が作業そのもののツールは false だけ返す
        private static bool HandleEscape()
        {
            if (Data.selection.Count == 0) return false;
            Modify("選択を解除", d => d.selection.Clear());
            return true;
        }

        // ─── 値の変更 ───────────────────────────────────────────────

        /// <summary>Undo に載せて値を変える。</summary>
        public static void Modify(string undoName, Action<YourSceneToolData> change)
        {
            Undo.RecordObject(Data, undoName);
            change(Data);
            Raise();
        }

        /// <summary>Undo に載せない値 (PaintMode など) を変えたあとに呼ぶ。</summary>
        public static void Raise()
        {
            Changed?.Invoke();
            SceneView.RepaintAll();
        }
    }
}
