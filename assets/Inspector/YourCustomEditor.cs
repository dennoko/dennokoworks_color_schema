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

        // ─── 標準フォント: OS のメイリオ ─────────────────────────────────
        // フォントアセットを同梱せず、端末インストール済みのメイリオを動的参照する。
        // ⚠ UI Toolkit のテキストは TextCore で描画されるため、レガシー Font
        //   (Font.CreateDynamicFontFromOSFont) を FontDefinition.FromFont() で渡すと
        //   グリフ生成に失敗し文字が一切表示されなくなる。必ず OS フォントから
        //   直接 SDF FontAsset を生成すること (CreateFontAsset は Unity 2022.3 で public)。
        // 未搭載環境 (Mac/Linux 等) では null を返し、エディタ標準フォントのままになる。
        private const string UI_FONT_FAMILY = "Meiryo";
        private static UnityEngine.TextCore.Text.FontAsset _uiFontAsset;
        private static bool _uiFontSearched;

        private static UnityEngine.TextCore.Text.FontAsset GetUIFontAsset()
        {
            if (_uiFontSearched) return _uiFontAsset;
            _uiFontSearched = true;

            try
            {
                _uiFontAsset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(UI_FONT_FAMILY, "Regular");
                if (_uiFontAsset != null)
                {
                    MarkFontAssetDontSave(_uiFontAsset);
                }
            }
            catch
            {
                _uiFontAsset = null;
            }
            return _uiFontAsset;
        }

        // 動的生成した FontAsset とその内部オブジェクト（アトラス用 material / atlasTextures）
        // すべてに HideAndDontSave を伝播させる。
        // ⚠ FontAsset 本体にだけ hideFlags を付けても不十分。CreateFontAsset が実行時に生成する
        //   フォントアトラスの material / Texture2D は FontAsset とは別の UnityEngine.Object で
        //   hideFlags は自動伝播しない。放置すると「未参照の一時アセット」と見なされ、
        //   Resources.UnloadUnusedAssets()（AssetDatabase.Refresh、プレイモード遷移、
        //   シーン保存などで暗黙的に呼ばれる）で破棄される。すると FontAsset は破棄済み material を
        //   参照し続け、次のテキスト描画で
        //   「MissingReferenceException: ... Material ... UIRStylePainter.DrawTextInfo /
        //    Material.get_mainTexture」が発生し UI のテキストが崩れる。
        private static void MarkFontAssetDontSave(UnityEngine.TextCore.Text.FontAsset fontAsset)
        {
            fontAsset.hideFlags = HideFlags.HideAndDontSave;

            if (fontAsset.material != null)
                fontAsset.material.hideFlags = HideFlags.HideAndDontSave;

            var atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures != null)
            {
                foreach (var tex in atlasTextures)
                {
                    if (tex != null)
                        tex.hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            // テーマ非依存のためのルートクラス + Inspector 用余白調整クラス
            container.AddToClassList("dennoko-root");
            container.AddToClassList("dennoko-inspector-root");
            // USS ロード失敗時も背景が明るくならないよう Surface0 を C# 側でも保証
            // (StyleColor への暗黙変換は Color のみ。Color32 のままでは CS0029 になる)
            container.style.backgroundColor = (Color)new Color32(0x12, 0x12, 0x12, 0xFF);

            // 標準フォント: OS のメイリオが使えれば全体に適用（全テキスト要素へ継承される）
            var uiFontAsset = GetUIFontAsset();
            if (uiFontAsset != null)
            {
                container.style.unityFontDefinition = FontDefinition.FromSDFFont(uiFontAsset);
            }

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
