# インスペクター (CustomEditor) 構造テンプレート (UI Toolkit)

dennokoworks フローティングデザインを **CustomEditor (Inspector)** に適用するためのガイドとテンプレート。
`window_structure_template.md`（EditorWindow 向け）と対になるドキュメント。

---

## EditorWindow との根本的な違い

| 項目 | EditorWindow | CustomEditor (Inspector) |
|---|---|---|
| エントリーポイント | `CreateGUI()` | `CreateInspectorGUI()` (override, 戻り値あり) |
| ルート要素 | `rootVisualElement` に追加 | `new VisualElement()` を作って **return** する |
| 背景の塗り方 | `.dennoko-root` の `background-color` で全面に塗れる | InspectorElement の余白が残るため `.dennoko-inspector-root` (ネガティブマージン) を併用する |
| スクロール | UXML に `ScrollView` を置く | Unity が自動管理（`ScrollView` 不要） |
| データ連携 | 手動で `Q<T>()` + コールバック | `PropertyField` + `container.Bind(serializedObject)` |
| ボタン推奨サイズ | `height: 34px / 26px` | **`height: 30px / 24px`**（インスペクターは幅が狭い） |

---

## ファイル構成

```
Editor/
├─ UI/
│   ├─ DennokoTheme.uss         ← uss_theme_template.md からコピー（Window と共有可）
│   └─ YourCustomEditor.uxml    ← 下記 UXML
└─ YourCustomEditor.cs          ← 下記 C#
```

---

## UXML テンプレート (`YourCustomEditor.uxml`)

`PropertyField` の `binding-path` に対象コンポーネントのシリアライズフィールド名を書く。
C# 側の `Bind(serializedObject)` により自動的に値が接続される。

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">

    <ui:VisualElement class="dennoko-card">
        <ui:Label text="GENERAL" class="dennoko-section-title dennoko-card-header" />
        <uie:PropertyField binding-path="displayName" />
        <uie:PropertyField binding-path="intensity" />
    </ui:VisualElement>

    <ui:VisualElement class="dennoko-card">
        <ui:Label text="ADVANCED" class="dennoko-section-title dennoko-card-header" />
        <uie:PropertyField binding-path="advancedSettings" />
        <ui:Button name="recalculate-button" text="Recalculate" class="dennoko-button-secondary" />
    </ui:VisualElement>

</ui:UXML>
```

---

## C# テンプレート (`YourCustomEditor.cs`)

```csharp
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
            container.style.backgroundColor = new Color32(0x12, 0x12, 0x12, 0xFF);

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
```

---

## Inspector 固有の注意点

### 1. 背景の塗り — `.dennoko-inspector-root`

インスペクターでは、Unity の `InspectorElement` が持つ左右余白のぶん、
`.dennoko-root` の背景の外側にエディタテーマの背景色が見えてしまう。
`DennokoTheme.uss` に定義済みの `.dennoko-inspector-root` がネガティブマージンで余白を打ち消す。

```css
/* DennokoTheme.uss に定義済み
   (ルート要素自身に dennoko-root と一緒に付与するため連結セレクタで書く) */
.dennoko-root.dennoko-inspector-root {
    margin-left: -15px;
    margin-right: -6px;
    padding: 8px 12px;
}
```

> **注意:** 余白量は Unity バージョンによって異なる。左右に隙間が残る・
> はみ出す場合は margin の値を調整すること。数 px の隙間を許容できる場合は
> このクラスを付けなくてもよい（カード自体は正しくダーク表示される）。

### 2. `PropertyField` はオーバーライドが自動で効く

`PropertyField` は内部で標準の `TextField` / `FloatField` / `Toggle` 等を生成するため、
`DennokoTheme.uss` の `.unity-base-field__input` / `.unity-toggle__checkmark` などの
オーバーライドがそのまま適用される。個別のスタイル指定は不要。

### 3. `Bind()` を忘れない

`CloneTree` しただけでは `PropertyField` は空のまま表示される。
`container.Bind(serializedObject)` を必ず呼ぶこと。
値の変更・Undo・複数選択 (`CanEditMultipleObjects`) はバインディングが自動処理する。

### 4. ボタンサイズはインスペクター向けに小さく

インスペクターは幅が狭いため、`dennoko-button-primary` (34px) が大きすぎる場合は
インライン style か専用クラスで高さを詰める。

```xml
<ui:Button text="Apply" class="dennoko-button-primary" style="height: 30px;" />
```

### 5. 複数の CustomEditor が衝突する場合

同一型に複数の `[CustomEditor]` があると片方しか使われない。
継承先も対象にする場合は `[CustomEditor(typeof(X), true)]` を確認する。
