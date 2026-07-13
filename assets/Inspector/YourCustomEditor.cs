using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace YourNamespace   // ← 変更する
{
    [CustomEditor(typeof(YourComponent))]   // ← 対象のコンポーネント型に変更する
    [CanEditMultipleObjects]
    public class YourCustomEditor : UnityEditor.Editor
    {
        private const string UXML_GUID = "YOUR_UXML_GUID_HERE";
        private const string USS_GUID  = "YOUR_USS_GUID_HERE";

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            // テーマ非依存のためのルートクラス + Inspector 用余白調整クラス
            container.AddToClassList("dennoko-root");
            container.AddToClassList("dennoko-inspector-root");
            // USS ロード失敗時も背景が明るくならないよう Surface0 を C# 側でも保証
            // (StyleColor への暗黙変換は Color のみ。Color32 のままでは CS0029 になる)
            container.style.backgroundColor = (Color)new Color32(0x12, 0x12, 0x12, 0xFF);

            // USS のロードと適用
            string ussPath = AssetDatabase.GUIDToAssetPath(USS_GUID);
            var uss = string.IsNullOrEmpty(ussPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (uss != null)
            {
                container.styleSheets.Add(uss);
            }

            // UXML のロードとインスタンス化
            string uxmlPath = AssetDatabase.GUIDToAssetPath(UXML_GUID);
            var uxml = string.IsNullOrEmpty(uxmlPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (uxml == null)
            {
                container.Add(new Label("UXML Asset が見つかりません。GUID を確認してください。"));
                return container;
            }
            uxml.CloneTree(container);

            // SerializedObject と PropertyField 群を接続する
            container.Bind(serializedObject);

            // 手動のイベント接続が必要な要素
            var recalcButton = container.Q<Button>("recalculate-button");
            if (recalcButton != null)
                recalcButton.clicked += OnRecalculate;

            return container;
        }

        private void OnRecalculate()
        {
            // target / serializedObject を使った処理を実装する
            // 例: var component = (YourComponent)target;
        }
    }
}
