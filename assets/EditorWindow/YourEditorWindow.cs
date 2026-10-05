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

        public enum StatusType { Info, Success, Error }

        private Label _statusLabel;
        private IVisualElementScheduledItem _statusResetSchedule;

        [MenuItem("Tools/Your Tool Name")]   // ← メニューパスを変更する
        public static void ShowWindow()
        {
            var window = GetWindow<YourEditorWindow>();
            window.titleContent = new GUIContent("Your Tool Name");
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

            // USS のロードと適用
            string ussPath = AssetDatabase.GUIDToAssetPath(USS_GUID);
            var uss = string.IsNullOrEmpty(ussPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (uss != null)
            {
                root.styleSheets.Add(uss);
            }
            else
            {
                Debug.LogWarning($"[{nameof(YourEditorWindow)}] USS が見つかりません。GUID を確認してください: {USS_GUID}");
            }

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

        // ─── バインディング ─────────────────────────────────────────────

        private void InitializeUI(VisualElement root)
        {
            _statusLabel = root.Q<Label>("status-label");

            // トグル付きセクション: トグル OFF でコンテンツをグレーアウト
            BindToggleSection(root,
                toggleName:  "color-correction-toggle",
                contentName: "color-correction-content",
                resetName:   "color-correction-reset",
                onReset: () =>
                {
                    root.Q<Slider>("hue-slider").value = 0f;
                    root.Q<Slider>("sat-slider").value = 1f;
                });

            root.Q<Button>("apply-button").clicked += ApplyAndSave;
            root.Q<Button>("reset-all-button").clicked += ResetAll;
        }

        /// <summary>トグル・コンテンツ・Reset ボタンを接続する共通ヘルパー。</summary>
        private static void BindToggleSection(
            VisualElement root, string toggleName, string contentName,
            string resetName, System.Action onReset)
        {
            var toggle  = root.Q<Toggle>(toggleName);
            var content = root.Q<VisualElement>(contentName);

            toggle.RegisterValueChangedCallback(evt => content.SetEnabled(evt.newValue));
            content.SetEnabled(toggle.value);

            var reset = root.Q<Button>(resetName);
            if (reset != null && onReset != null)
                reset.clicked += () => onReset();
        }

        // ─── ステータスバー ─────────────────────────────────────────────

        /// <summary>ステータスを表示する。Success / Error は 3 秒後に Ready へ自動復帰。</summary>
        private void SetStatus(string message, StatusType type, long autoResetMs = 3000)
        {
            if (_statusLabel == null) return; // UXML ロード失敗時・要素名変更時の NRE 防止

            _statusLabel.text = message;
            _statusLabel.EnableInClassList("dennoko-status--success", type == StatusType.Success);
            _statusLabel.EnableInClassList("dennoko-status--error",   type == StatusType.Error);

            _statusResetSchedule?.Pause();
            if (type != StatusType.Info)
            {
                _statusResetSchedule = _statusLabel.schedule
                    .Execute(() => SetStatus("Ready", StatusType.Info))
                    .StartingIn(autoResetMs);
            }
        }

        // ─── アクション ─────────────────────────────────────────────────

        private void ApplyAndSave()
        {
            // TODO: 実装する
            SetStatus("Saved.", StatusType.Success);
        }

        private void ResetAll()
        {
            // TODO: 実装する
            SetStatus("Reset.", StatusType.Info);
        }
    }
}
