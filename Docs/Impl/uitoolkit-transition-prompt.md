# dennokoworks デザインシステム (UI Toolkit 移行・実装ガイドライン)

このドキュメントは、Unity Editor 拡張 (EditorWindow / CustomEditor) の UI を **Unity UI Toolkit (UXML/USS)** を用いて実装・改修する際の AI 向け指示プロンプトです。
AI は Unity UI の実装または改修を求められた際、このガイドラインを厳守してコードおよび UXML/USS を生成してください。

---

## 1. ターゲット環境と基本要件

- **ターゲット**: Unity 2022.3.22f1 ～ Unity 6
- **最優先要件 (テーマ非依存)**:
  Unity エディタのテーマ設定 (Personal Light, Personal Dark, Professional 等) に左右されず、常に dennokoworks の「フローティングデザイン (ダークテーマ)」を完全に維持すること。
  Unity デフォルトのスタイル (明るい背景、黒いテキストなど) が混入しないよう、すべての要素のカラー・スタイルを明示的に上書きすること。
- **実装形態**: 
  - UI 構造は **UXML** で定義する。
  - スタイリングは **USS** で定義する。
  - C# コードは UI の生成 (`CreateGUI` / `CreateInspectorGUI`)、アセットのロード、およびロジックのバインディングに専念させる。

---

## 2. dennokoworks カラーパレット (USS 変数定義)

すべてのカラーは、ルート要素である `.dennoko-root` に定義されたカスタムプロパティ (USS 変数) を経由して使用してください。直接のカラーコード指定 (ハードコーディング) は禁止します。

```css
.dennoko-root {
    /* ニュートラル・レイヤー (サーフェス) */
    --dennoko-surface-0: #121212; /* 最背面背景 */
    --dennoko-surface-1: #1e1e1e; /* カード・入力欄背景 */
    --dennoko-surface-2: #2c2c2c; /* ホバー・ツールバー背景 */
    --dennoko-outline: #3a3a3a;   /* 境界線・セパレーター */

    /* タイポグラフィ */
    --dennoko-text-primary: #ffffff;   /* タイトル・強調 */
    --dennoko-text-secondary: #cccccc; /* 本文・標準ラベル */
    --dennoko-text-tertiary: #aaaaaa;  /* 補足・注記 */
    --dennoko-text-disabled: #555555;  /* 無効状態 */

    /* セマンティック (状態・警告) */
    --dennoko-semantic-error: #9b1b30;   /* エラー */
    --dennoko-semantic-warning: #ffb74d; /* 警告 */
    --dennoko-semantic-success: #4caf50; /* 成功 */
    --dennoko-semantic-info: #64b5f6;    /* 情報 */

    /* インタラクション */
    --dennoko-accent: #ffffff;                  /* アクセントカラー */
    --dennoko-hover-overlay: rgba(255, 255, 255, 0.05); /* ホバー時の重ね色 */
}
```

---

## 3. テーマ非依存 (Personal Light テーマ対応) のための USS ルール

Unity の Personal Light テーマでは、ビルトインの UI 要素に白系の背景や黒系のテキストが適用されます。これを完全に遮断するため、以下の **強制オーバーライド USS** を適用してください。

### ① テキスト要素の強制リセット
Unity のすべてのテキスト要素 (`.unity-text-element`) の色を上書きします。
```css
.dennoko-root .unity-text-element {
    color: var(--dennoko-text-secondary);
}
.dennoko-root .dennoko-text-primary {
    color: var(--dennoko-text-primary);
}
.dennoko-root .dennoko-text-tertiary {
    color: var(--dennoko-text-tertiary);
}
.dennoko-root .dennoko-text-disabled {
    color: var(--dennoko-text-disabled);
}
```

### ② ボタンの強制リセット
エディタテーマによって境界線や背景グラデーションが混入するのを防ぐため、ボタンの見た目を完全にフラットな dennokoworks デザインに固定します。
```css
.dennoko-root .unity-button {
    background-color: var(--dennoko-surface-1);
    border-color: var(--dennoko-outline);
    border-width: 1px;
    border-radius: 4px;
    color: var(--dennoko-text-primary);
    padding: 4px 12px;
}
.dennoko-root .unity-button:hover {
    background-color: var(--dennoko-surface-2);
}
.dennoko-root .unity-button:active {
    background-color: var(--dennoko-hover-overlay);
    border-color: var(--dennoko-accent);
}
.dennoko-root .unity-button:disabled {
    background-color: var(--dennoko-surface-0);
    border-color: var(--dennoko-outline);
    color: var(--dennoko-text-disabled);
}
```

### ③ 入力フィールド (TextField, IntegerField, DropdownField 等) のリセット
Unity 標準の入力フィールドのインプットエリア (`.unity-base-field__input`) をダークスタイルに固定します。
```css
.dennoko-root .unity-base-field__input {
    background-color: var(--dennoko-surface-1);
    border-color: var(--dennoko-outline);
    border-width: 1px;
    border-radius: 4px;
    color: var(--dennoko-text-primary);
    padding: 3px 6px;
}
.dennoko-root .unity-base-field__input:focus {
    border-color: var(--dennoko-accent);
}
.dennoko-root .unity-base-field:hover .unity-base-field__input {
    background-color: var(--dennoko-surface-2);
}
```

### ④ トグル (Toggle) のチェックボックスリセット
```css
.dennoko-root .unity-toggle__checkmark {
    background-color: var(--dennoko-surface-1);
    border-color: var(--dennoko-outline);
    border-width: 1px;
    border-radius: 3px;
}
.dennoko-root .unity-toggle:hover .unity-toggle__checkmark {
    background-color: var(--dennoko-surface-2);
}
.dennoko-root .unity-toggle:checked .unity-toggle__checkmark {
    background-color: var(--dennoko-accent);
    /* Unity6等でチェックマークの画像（白）がテーマ依存で黒くならないよう色を強制 */
    -unity-background-image-tint-color: var(--dennoko-surface-0);
}
```

### ⑤ フォールドアウト (Foldout) のヘッダーとコンテンツ
```css
.dennoko-root .unity-foldout__toggle {
    background-color: var(--dennoko-surface-1);
    margin: 2px 0;
    padding: 4px;
    border-radius: 4px;
}
.dennoko-root .unity-foldout__content {
    margin-left: 15px;
    padding: 6px;
    border-left-width: 1px;
    border-left-color: var(--dennoko-outline);
}
```

---

## 4. UI 構造化のための USS クラス (フローティングデザインの表現)

UXML を構築する際、情報を整理するためのセマンティックな USS クラスを定義・適用してください。

### カード (セクション) 構造
```css
/* セクションカード */
.dennoko-card {
    background-color: var(--dennoko-surface-1);
    border-color: var(--dennoko-outline);
    border-width: 1px;
    border-radius: 8px;
    padding: 12px;
    margin-bottom: 10px;
}

/* カード内ヘッダー */
.dennoko-card-header {
    border-bottom-width: 1px;
    border-bottom-color: var(--dennoko-outline);
    padding-bottom: 6px;
    margin-bottom: 10px;
}
```

---

## 5. C# 実装テンプレート (アセットの堅牢なロード)

Unity Editor 拡張において、UXML や USS のパスが変わっても壊れないよう、**GUID を用いたロード** を採用してください。

### ① EditorWindow テンプレート
```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dennokoworks.Editor
{
    public class DennokoEditorWindow : EditorWindow
    {
        // 開発するアセットのGUIDを事前に調べて定義しておく
        private const string UXML_GUID = "YOUR_UXML_GUID_HERE";
        private const string USS_GUID = "YOUR_USS_GUID_HERE";

        [MenuItem("Tools/Dennokoworks Window")]
        public static void ShowWindow()
        {
            DennokoEditorWindow wnd = GetWindow<DennokoEditorWindow>();
            wnd.titleContent = new GUIContent("Dennokoworks");
            wnd.minSize = new Vector2(300, 400);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // テーマ非依存のためのルートクラスを適用
            root.AddToClassList("dennoko-root");
            root.style.flexGrow = 1;
            root.style.backgroundColor = new StyleColor(new Color(0.07f, 0.07f, 0.07f, 1f)); // Surface0 をC#側でも保証

            // USS のロードと適用
            string ussPath = AssetDatabase.GUIDToAssetPath(USS_GUID);
            if (!string.IsNullOrEmpty(ussPath))
            {
                StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (uss != null)
                {
                    root.styleSheets.Add(uss);
                }
            }

            // UXML のロードとインスタンス化
            string uxmlPath = AssetDatabase.GUIDToAssetPath(UXML_GUID);
            if (!string.IsNullOrEmpty(uxmlPath))
            {
                VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (uxml != null)
                {
                    uxml.CloneTree(root);
                }
            }
            else
            {
                root.Add(new Label("UXML Asset が見つかりません。GUID を確認してください。"));
                return;
            }

            // UI要素の取得とバインディング
            InitializeUI(root);
        }

        private void InitializeUI(VisualElement root)
        {
            // ここにボタンのクリックイベントや、テキストの初期化処理などを記述します。
            // 例: Button myButton = root.Q<Button>("my-button");
        }
    }
}
```

### ② CustomEditor (Inspector) テンプレート
```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dennokoworks.Editor
{
    [CustomEditor(typeof(MonoBehaviour))] // 対象のコンポーネント型に書き換えてください
    public class DennokoCustomEditor : UnityEditor.Editor
    {
        private const string UXML_GUID = "YOUR_UXML_GUID_HERE";
        private const string USS_GUID = "YOUR_USS_GUID_HERE";

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("dennoko-root");

            // USS のロードと適用
            string ussPath = AssetDatabase.GUIDToAssetPath(USS_GUID);
            if (!string.IsNullOrEmpty(ussPath))
            {
                StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (uss != null)
                {
                    container.styleSheets.Add(uss);
                }
            }

            // UXML のロードと適用
            string uxmlPath = AssetDatabase.GUIDToAssetPath(UXML_GUID);
            if (!string.IsNullOrEmpty(uxmlPath))
            {
                VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (uxml != null)
                {
                    uxml.CloneTree(container);
                }
            }

            // バインディング (SerializedObject と UI 要素の接続)
            container.Bind(serializedObject);

            return container;
        }
    }
}
```

---

## 6. IMGUI (旧UniTexTheme) から UI Toolkit へのマッピング

旧来の IMGUI 実装を UI Toolkit へ変換する際は、以下のルールに従ってください。

| IMGUI 概念 (旧 UniTexTheme) | UI Toolkit (UXML 要素 / USS クラス) |
|---|---|
| `YourTheme.Surface0` | `root` 要素の背景 (`background-color: var(--dennoko-surface-0)`) |
| `DrawSection(title, ...)` / カード | UXMLの `VisualElement` に `.dennoko-card` を付与し、内部に見出しラベルとコンテンツを配置 |
| `YourTheme.TextPrimary` | `.dennoko-text-primary` |
| `YourTheme.TextSecondary` | `.unity-text-element` (デフォルト) |
| `YourTheme.TextTertiary` | `.dennoko-text-tertiary` |
| `YourTheme.SemanticError` | `color: var(--dennoko-semantic-error)` または背景色等に使用 |
| `GUILayout.Button(...)` | `Button` (USS で自動的に `.unity-button` のオーバーライドが適用される) |
| `EditorGUILayout.TextField(...)` | `TextField` (USS で自動的に `.unity-base-field__input` 等のオーバーライドが適用される) |

---

## 7. 実装時によくある罠とチェックリスト

1. **UnityのLightテーマを適用して表示確認したか？**
   - 開発時は必ず Unity エディタの Preferences でテーマを Light と Dark の両方に切り替えて動作確認をしてください。
   - ラベルの文字が黒くなって読めなくなったり、ボタン背景が真っ白になって浮いてしまっていないかを確認してください。
2. **UXML/USS の GUID は一意か？**
   - テンプレートコードをコピーした際、`YOUR_UXML_GUID_HERE` 等のプレースホルダーを実際のメタファイルの GUID に置き換えたかを必ず確認してください。
3. **C#コードだけでUIを構築しようとしていないか？**
   - 動的な要素生成（リストアイテムの動的追加など）を除き、UIのレイアウト構造は原則 UXML ファイル内にカプセル化してください。C#コードで `new VisualElement()` を手動で組み上げてスタイリングすることは避けてください。
