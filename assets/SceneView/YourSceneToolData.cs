using System.Collections.Generic;
using UnityEngine;

namespace YourNamespace   // ← 変更する（ツールの namespace にする）
{
    /// <summary>
    /// Undo に載せる作業中の値 (雛形)。Undo.RecordObject を使うために ScriptableObject にしている。
    /// コンポーネントに値を持つツール (P5) は、これを作らずコンポーネントを RecordObject する。
    /// ⚠ ScriptableObject はクラス名とファイル名を一致させる (ドメインリロード後にスクリプトを見失わないように)。
    /// </summary>
    internal sealed class YourSceneToolData : ScriptableObject
    {
        // パネル (S2) で変える値
        public float radius = 0.08f;
        public int falloff;          // 0 滑らか / 1 直線 / 2 鋭い / 3 一定
        public int mirror = 1;       // 0 なし / 1 X / 2 Y / 3 Z

        // シーンで選んだもの (頂点・面など)
        public List<int> selection = new List<int>();
    }
}
