# Window 構造テンプレート (UI Toolkit)

Unity Editor 拡張ウィンドウの全体骨格。**UXML + C# のコードをコピーして作業を開始する**。

- UI 構造 → UXML (`YourEditorWindow.uxml`)
- スタイル → USS (`uss_theme_template.md` の `DennokoTheme.uss`)
- ロジック → C# (`YourEditorWindow.cs`)

## 完成イメージ

このUIが目指すビジュアルは `../example/index.html` をブラウザで開いて確認すること。
`Docs/design_reference.md` にデザインコンセプト、`Docs/colors_spec.md` にカラー仕様がある。

---

## ウィンドウ全体のレイアウト構成

```
┌──────────────────────────────────────┐
│ [ウィンドウタイトル]          [JA][EN] │  ← .dennoko-header
│ ──────────────────────────────────── │  ← .dennoko-separator
│ ┌──────────────────────────────────┐ │
│ │ SECTION TITLE                    │ │  ← .dennoko-card
│ │ ────────────────────────────     │ │     + .dennoko-card-header
│ │  [コンテンツ]                    │ │
│ └──────────────────────────────────┘ │  ← ↑ ScrollView の中
│ ┌──────────────────────────────────┐ │
│ │ [☑] TOGGLE SECTION     [Reset]   │ │  ← .dennoko-toggle-header
│ │ ────────────────────────────     │ │
│ │  [スライダーなど]                 │ │  ← name="...-content"
│ └──────────────────────────────────┘ │
│ ┌──────────────────────────────────┐ │
│ │ [      Apply & Save (Primary)   ]│ │  ← .dennoko-button-primary
│ │ [         Reset All             ]│ │  ← .dennoko-button-secondary
│ └──────────────────────────────────┘ │
│ [ステータスメッセージ]                │  ← .dennoko-status
└──────────────────────────────────────┘
```

---

## ファイル構成

```
Editor/
├─ UI/
│   ├─ DennokoTheme.uss        ← uss_theme_template.md からコピー
│   └─ YourEditorWindow.uxml   ← 下記 UXML
└─ YourEditorWindow.cs         ← 下記 C#
```

配置後、Unity が生成した `.meta` ファイルから UXML / USS それぞれの GUID を控え、
C# の定数に設定する。

---

## UXML テンプレート (`YourEditorWindow.uxml`)

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">

    <!-- ヘッダー -->
    <ui:VisualElement class="dennoko-header">
        <ui:Label text="YOUR TOOL NAME" class="dennoko-title" />
    </ui:VisualElement>
    <ui:VisualElement class="dennoko-separator" />

    <!-- 設定エリア (スクロール可能)。ウィンドウ縮小時はここだけが縮む -->
    <ui:ScrollView class="dennoko-scroll">

        <!-- 常時表示セクション -->
        <ui:VisualElement class="dennoko-card">
            <ui:Label text="INPUT" class="dennoko-section-title dennoko-card-header" />
            <uie:ObjectField label="Source" name="source-field"
                type="UnityEngine.Texture2D, UnityEngine.CoreModule" />
        </ui:VisualElement>

        <!-- ON/OFF トグル付きセクション -->
        <ui:VisualElement class="dennoko-card">
            <ui:VisualElement class="dennoko-card-header dennoko-toggle-header">
                <ui:Toggle name="color-correction-toggle" text="COLOR CORRECTION"
                    value="true" class="dennoko-section-title" />
                <ui:Button name="color-correction-reset" text="Reset" />
            </ui:VisualElement>
            <ui:VisualElement name="color-correction-content">
                <ui:Slider label="Hue" name="hue-slider"
                    low-value="-180" high-value="180" show-input-field="true" />
                <ui:Slider label="Saturation" name="sat-slider"
                    low-value="0" high-value="2" value="1" show-input-field="true" />
            </ui:VisualElement>
        </ui:VisualElement>

    </ui:ScrollView>

    <!-- フッター (アクションボタン)。dennoko-footer で縮小時の潰れを防ぐ -->
    <ui:VisualElement class="dennoko-card dennoko-footer">
        <ui:Button name="apply-button" text="Apply &amp; Save" class="dennoko-button-primary" />
        <ui:Button name="reset-all-button" text="Reset All" class="dennoko-button-secondary" />
    </ui:VisualElement>

    <!-- ステータスバー -->
    <ui:Label name="status-label" text="Ready" class="dennoko-status" />

</ui:UXML>
```

ポイント:

- レイアウト構造は原則すべて UXML に書く。C# で `new VisualElement()` を組み上げない
  （リストアイテムの動的追加など、動的な要素生成は例外）。
- ロジックから触る要素には `name` 属性を付け、C# 側で `root.Q<T>("name")` で取得する。
- `dennoko-*` クラスの見た目の定義は `uss_theme_template.md` を参照。

---

## C# テンプレート (`YourEditorWindow.cs`)

UXML / USS のパス変更に耐えるよう **GUID でロード**する。

```csharp
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
            root.style.backgroundColor = new Color32(0x12, 0x12, 0x12, 0xFF);
            root.style.flexGrow = 1;

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
```

---

## カスタマイズポイント

| 箇所 | 変更内容 |
|---|---|
| `namespace YourNamespace` / クラス名 | プロジェクトに合わせる |
| `UXML_GUID` / `USS_GUID` | 配置したアセットの `.meta` の GUID |
| `[MenuItem("Tools/Your Tool Name")]` | メニューパス |
| UXML のセクション | `.dennoko-card` ブロックを追加・削除 |
| `InitializeUI` | 要素の取得とイベント接続を追加 |

---

## セクションの追加パターン

**常時表示セクション** — UXML に以下のブロックを追加するだけでよい。

```xml
<ui:VisualElement class="dennoko-card">
    <ui:Label text="SECTION TITLE" class="dennoko-section-title dennoko-card-header" />
    <!-- コンテンツ -->
</ui:VisualElement>
```

**ON/OFF トグル付きセクション** — UXML にトグル付きカードを追加し、
C# で `BindToggleSection(...)` を 1 行呼ぶ。

- `toggle = true` → コンテンツが有効（通常表示）
- `toggle = false` → `SetEnabled(false)` により Unity が自動でグレーアウトし操作不可になる

---

## よくある注意点

- **`show-input-field="true"`**: Slider に数値入力欄を付ける。入力欄のスタイルは
  `.unity-base-field__input` のオーバーライドが自動適用される。
- **エディタ専用コントロール** (`ObjectField`, `PropertyField` 等) は
  `xmlns:uie="UnityEditor.UIElements"` 名前空間で書く。
- **テーマ確認**: Preferences でエディタテーマを Light / Dark 両方に切り替えて表示確認する。
  詳細チェックリストは `techniques.md` を参照。
