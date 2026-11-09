using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace YourNamespace   // ← 変更する
{
    public class YourEditorWindow : EditorWindow
    {
        // 配置した UXML / USS の .meta ファイルに記載されている GUID を設定する
        private const string UXML_GUID = "YOUR_UXML_GUID_HERE";
        private const string USS_GUID  = "YOUR_USS_GUID_HERE";
        // 大きい単位のブロック (blocks/README.md) を使う場合だけ設定する。空なら読まない
        private const string BLOCKS_USS_GUID = "";

        private const string ToolName = "Your Tool Name";   // ← 変更する

        private DennokoStatus _status;
        private DennokoAppBar _appBar;

        [MenuItem("Tools/Your Tool Name")]   // ← メニューパスを変更する
        public static void ShowWindow()
        {
            var window = GetWindow<YourEditorWindow>();
            window.titleContent = new GUIContent(ToolName);
            window.minSize = new Vector2(400, 600);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // テーマ非依存のためのルートクラスを適用
            root.AddToClassList("dennoko-root");
            // USS ロード失敗時も背景が明るくならないよう Surface0 を C# 側でも保証
            // (StyleColor への暗黙変換は Color のみ。Color32 のままでは CS0029 になる)
            root.style.backgroundColor = (Color)new Color32(0x12, 0x12, 0x12, 0xFF);
            root.style.flexGrow = 1;

            // 標準フォント: OS のメイリオを全体に適用（全テキスト要素へ継承される）。
            // 生成・アトラス保護・キャッシュ消失時の再適用はすべて DennokoUIFont が行う。
            // ⚠ ここで FontAsset を直接生成しないこと（troubleshooting.md §8〜§10）。
            DennokoUIFont.Apply(root);

            // USS のロードと適用 (テーマ → ブロックの順。後に足したものが同じ詳細度で勝つ)
            AddStyleSheet(root, USS_GUID);
            if (!string.IsNullOrEmpty(BLOCKS_USS_GUID)) AddStyleSheet(root, BLOCKS_USS_GUID);

            // UXML のロードとインスタンス化
            string uxmlPath = AssetDatabase.GUIDToAssetPath(UXML_GUID);
            var uxml = string.IsNullOrEmpty(uxmlPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (uxml == null)
            {
                root.Add(new Label("UXML Asset が見つかりません。GUID を確認してください。"));
                return;
            }
            uxml.CloneTree(root);

            InitializeUI(root);
        }

        private static void AddStyleSheet(VisualElement root, string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var uss = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (uss != null) root.styleSheets.Add(uss);
            else Debug.LogWarning($"[{nameof(YourEditorWindow)}] USS が見つかりません。GUID を確認してください: {guid}");
        }

        // ─── バインディング ─────────────────────────────────────────────

        private void InitializeUI(VisualElement root)
        {
            // 標準フッターのステータスバー
            _status = new DennokoStatus(root.Q<Label>("status-label"));

            // 標準ヘッダー (トップバー)。使わない要素は UXML から消せば無視される
            _appBar = new DennokoAppBar(root, ToolName)
            {
                ManualUrl  = "https://example.com/manual",     // ← 変更する
                ReleaseUrl = "https://example.booth.pm/",      // ← 公開配布ページ（Booth 等）を指定（GitHub 直リンクは置かない）
                ResetAll   = ResetAll,
                AddToolMenuItems = menu =>
                {
                    // ツール固有の項目 (⋮ の上段)。無ければ何もしない
                },
            };
            _appBar.LanguageChanged += english => { /* TODO: 文言を切り替える */ };
            // バージョンチェックを付ける場合 (references/version-check-guide.md):
            //   _appBar.CheckUpdateRequested += () => { YourToolVersion.ForceRecheck(); LoadVersionResultFromSessionState(); };

            // トグル付きセクション: トグル OFF でセクションを折りたたむ
            BindToggleSection(root,
                toggleName:  "color-correction-toggle",
                contentName: "color-correction-content",
                resetName:   "color-correction-reset",
                onReset: () =>
                {
                    root.Q<Slider>("hue-slider").value = 0f;
                    root.Q<Slider>("sat-slider").value = 1f;
                });

            // 出力ドック (使わない場合は UXML の output-dock ごと消し、この行も消す)
            var outputPath = root.Q<TextField>("output-path");
            var browse = root.Q<Button>("output-browse");
            if (browse != null && outputPath != null)
            {
                browse.clicked += () =>
                {
                    string picked = EditorUtility.OpenFolderPanel("出力先", outputPath.value, string.Empty);
                    if (string.IsNullOrEmpty(picked)) return;
                    if (!picked.StartsWith(Application.dataPath))
                    {
                        _status.Set("プロジェクトの Assets 内を選んでください", DennokoStatusType.Warning);
                        return;
                    }
                    outputPath.value = "Assets" + picked.Substring(Application.dataPath.Length);
                };
            }

            root.Q<Button>("apply-button").clicked += ApplyAndSave;
            root.Q<Button>("reset-all-button").clicked += ResetAll;
        }

        private const string CollapsedClass = "dennoko-toggle-section--collapsed";

        /// <summary>
        /// トグル・コンテンツ・Reset ボタンを接続する共通ヘルパー。
        /// トグル OFF でセクションを折りたたむ (カード = コンテンツの親に
        /// .dennoko-toggle-section--collapsed を付ける。見た目は USS 側で定義)。
        /// グレーアウト (SetEnabled) ではなく折りたたみにすることで、無効な項目を画面から消す。
        /// </summary>
        private static void BindToggleSection(
            VisualElement root, string toggleName, string contentName,
            string resetName, System.Action onReset)
        {
            var toggle  = root.Q<Toggle>(toggleName);
            var content = root.Q<VisualElement>(contentName);
            var section = content.parent;

            toggle.RegisterValueChangedCallback(evt => section.EnableInClassList(CollapsedClass, !evt.newValue));
            section.EnableInClassList(CollapsedClass, !toggle.value);

            var reset = root.Q<Button>(resetName);
            if (reset != null && onReset != null)
                reset.clicked += () => onReset();
        }

        // ─── アクション ─────────────────────────────────────────────────

        private void ApplyAndSave()
        {
            // TODO: 実装する
            _status.Set("Saved.", DennokoStatusType.Success);
        }

        private void ResetAll()
        {
            // TODO: 実装する
            _status.Set("Reset.");
        }
    }
}
