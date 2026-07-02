# UI Toolkit 実装テクニック集

Unity Editor 拡張 (UI Toolkit / UXML / USS) で dennokoworks カラースキーマを表現するための具体的な手法と、実装時によくある罠。

---

## 1. テーマ非依存が成立する仕組み

Unity エディタのビルトインコントロールは、エディタテーマ（Personal Light / Dark）の
デフォルトスタイルシートで装飾されている。`DennokoTheme.uss` は
**`.dennoko-root` を起点とする子孫セレクタ**でビルトインクラスを上書きする。

```css
/* Unity デフォルト: .unity-button { ... }  (テーマ依存) */
/* 本テーマ:        .dennoko-root .unity-button { ... }  (常にこちらが勝つ) */
```

子孫セレクタはクラス数が多いぶん詳細度 (specificity) が高く、
テーマのデフォルト定義より常に優先される。これが成立する条件は 1 つだけ:

> **すべての UI のルート要素に `dennoko-root` クラスを付与すること。**

`dennoko-root` は同時に USS 変数（`--dennoko-*`）の定義元でもある。
変数は子孫にのみ継承されるため、このクラスを忘れるとスタイル全体が無効になる。

補足:
- USS に `!important` は存在しない。優先させたい場合は詳細度を上げる
  （セレクタを長くする）か、後から `styleSheets.Add` したシートに書く。
- カラーのハードコーディングは `.dennoko-root` の変数定義部のみに限定し、
  それ以外はすべて `var(--dennoko-*)` を経由する。

---

## 2. GUID によるアセットロード

`AssetDatabase.LoadAssetAtPath("Assets/....uxml")` のパス直書きは、
フォルダ移動やリネームで壊れる。**GUID → パス解決**を使えばアセットを移動しても壊れない。

```csharp
private const string UXML_GUID = "0123456789abcdef0123456789abcdef";

string path = AssetDatabase.GUIDToAssetPath(UXML_GUID); // 見つからなければ ""
var uxml = string.IsNullOrEmpty(path)
    ? null
    : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
```

### GUID の調べ方

- `.meta` ファイルをテキストエディタで開き `guid:` 行を見る
- または Project ビューでアセットを右クリック → Copy GUID (Unity 2022.2+)

> **罠:** テンプレートの `YOUR_UXML_GUID_HERE` を置き換え忘れると
> 実行時に「UXML Asset が見つかりません」となる。配置後に必ず置き換えること。

---

## 3. Foldout の矢印が「白い箱」になる罠

Foldout の展開矢印は内部的に Toggle のチェックマーク要素
(`.unity-toggle__checkmark`) を流用している。そのため、チェックボックスの
オーバーライド（背景色・枠線・checked 時の白背景）が矢印にもかかってしまう。

`DennokoTheme.uss` では以下のリセットで対処済み。テーマを自作・改変する場合は必ず入れること。

```css
.dennoko-root .unity-foldout__toggle .unity-toggle__checkmark,
.dennoko-root .unity-foldout__toggle:checked .unity-toggle__checkmark,
.dennoko-root .unity-foldout__toggle:hover .unity-toggle__checkmark {
    background-color: transparent;
    border-width: 0;
    -unity-background-image-tint-color: var(--dennoko-text-secondary);
}
```

`:checked` / `:hover` のセレクタも併記するのは、疑似クラス付きセレクタのほうが
詳細度が高く、素の打ち消しだけでは展開時・ホバー時に負けてしまうため。

---

## 4. アイコン画像のテーマ依存を断つ — tint-color

ドロップダウンの矢印、チェックマーク、Foldout の三角形などはテクスチャ画像であり、
Light テーマでは黒い画像が使われて暗背景で見えなくなる。
`-unity-background-image-tint-color` で色を強制する。

```css
.dennoko-root .unity-base-popup-field__arrow {
    -unity-background-image-tint-color: var(--dennoko-text-secondary);
}
```

対象になりやすい要素:

| 要素 | クラス |
|---|---|
| ドロップダウン矢印 | `.unity-base-popup-field__arrow` |
| トグルのチェックマーク画像 | `.unity-toggle__checkmark` (checked 時) |
| ObjectField のピッカー | `.unity-object-field__selector` |
| スクロールバーの矢印 | `.unity-scroller__low-button` / `__high-button` |

---

## 5. 動的な要素生成 (C# で作ってよいケース)

レイアウト構造は原則 UXML に書くが、**リストアイテムのような動的な繰り返し要素**は
C# で生成してよい。その場合もインラインスタイルではなく USS クラスを付与する。

```csharp
// ❌ 悪い例: C# にスタイルをハードコード
var item = new VisualElement();
item.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);

// ✅ 良い例: USS クラスを付与し、見た目は USS に集約
var item = new VisualElement();
item.AddToClassList("dennoko-card");
```

行テンプレートが複雑な場合は、アイテム専用の UXML を用意して
`itemUxml.CloneTree()`（または `ListView.makeItem`）で複製する。

```csharp
listView.makeItem = () => itemUxml.CloneTree();
listView.bindItem = (element, index) =>
{
    element.Q<Label>("item-name").text = _items[index].name;
};
```

---

## 6. USS でよくある構文の罠 (CSS との違い)

| やりたいこと | CSS | USS |
|---|---|---|
| 枠線 | `border: 1px solid #3a3a3a;` | ショートハンド不可。`border-width: 1px; border-color: #3a3a3a;` に分ける |
| 背景 | `background: #121212;` | `background` 不可。`background-color` を使う |
| 太字 | `font-weight: bold;` | `-unity-font-style: bold;` |
| 文字揃え | `text-align: center;` | `-unity-text-align: middle-center;` |
| 折り返し | `overflow-wrap` 等 | `white-space: normal;` |
| 最優先 | `!important` | 存在しない。詳細度で解決する |
| box-shadow | `box-shadow: ...;` | 存在しない。Surface の明度差で浮遊感を出す（フローティングデザインの流儀） |

`padding: 4px 12px;` のような 1〜4 値ショートハンドは margin / padding では使える。

---

## 7. レイアウトは Flexbox

UI Toolkit のレイアウトエンジンは Flexbox (Yoga)。IMGUI の
`BeginHorizontal/Vertical` に相当する概念は `flex-direction`。

```css
/* IMGUI: GUILayout.BeginHorizontal() 相当 */
.my-row { flex-direction: row; align-items: center; }

/* 右寄せ: GUILayout.FlexibleSpace() 相当 */
.spacer { flex-grow: 1; }
```

- デフォルトは `flex-direction: column`（縦積み）
- `justify-content: space-between` でタイトルとボタンを両端に配置
  （`.dennoko-toggle-header` / `.dennoko-header` が使用）
- 幅いっぱいに広げる: 親が column なら子はデフォルトで stretch。
  row 内では `flex-grow: 1` を指定する

---

## 8. IMGUI (旧 UniTexTheme) からの移行マッピング

旧来の IMGUI 実装（`UniTexTheme` / `DrawSection` パターン）を UI Toolkit へ変換する際の対応表。

| IMGUI 概念 (旧 UniTexTheme) | UI Toolkit (UXML 要素 / USS クラス) |
|---|---|
| `OnGUI()` + `EditorGUI.DrawRect(..., Surface0)` | `CreateGUI()` + ルートに `.dennoko-root`（背景は USS が塗る） |
| `PushEditorTheme()` / `PopEditorTheme()` | **不要**。USS の子孫セレクタが常時適用されるため Push/Pop の概念自体がない |
| `Initialize()` + テクスチャキャッシュ | **不要**。テクスチャ生成・ドメインリロード対策は不要になる |
| `CardStyle` / `DrawSection(title, ...)` | `<ui:VisualElement class="dennoko-card">` + 見出し `Label` |
| `DrawToggleSection(...)` | `.dennoko-toggle-header` + `Toggle` + `BindToggleSection()` ヘルパー |
| `YourTheme.Surface0` 等の Color 定数 | `var(--dennoko-surface-0)` 等の USS 変数 |
| `YourTheme.TextPrimary` | `.dennoko-text-primary` クラス |
| `YourTheme.TextSecondary` | 指定不要（`.unity-text-element` のデフォルト） |
| `YourTheme.TextTertiary` | `.dennoko-text-tertiary` クラス |
| `YourTheme.SemanticError` 等 | `.dennoko-text-error` 等、または `var(--dennoko-semantic-error)` |
| `ActionButtonStyle` / `GUILayout.Button` | `<ui:Button class="dennoko-button-primary">` |
| `SecondaryButtonStyle` | `<ui:Button class="dennoko-button-secondary">` |
| `EditorGUILayout.TextField(...)` | `<ui:TextField>`（`.unity-base-field__input` の上書きが自動適用） |
| `EditorGUILayout.ObjectField(...)` | `<uie:ObjectField>` |
| `EditorGUILayout.Slider(...)` | `<ui:Slider show-input-field="true">` |
| `GetStatusStyle(StatusType)` | `.dennoko-status--success` 等のクラス切り替え（`EnableInClassList`） |
| `DrawSeparator()` | `<ui:VisualElement class="dennoko-separator" />` |
| `serializedObject.Update()` / `ApplyModifiedProperties()` | `PropertyField` + `Bind(serializedObject)` が自動処理 |

移行によって**不要になる**もの: `MakeTex` / `MakeBorderedTex`、`FixAllTextColors`、
`GUIStyleBackup`、`_initialized` フラグ、テクスチャの null チェック。
これらはすべて IMGUI の制約に対する回避策であり、UI Toolkit では USS が代替する。

---

## 10. ライトテーマ対応およびコントロール描画のバグ回避テクニック

Unity の Preferences でエディタのテーマを Light に切り替えた際にも、UI が崩れたり文字が読めなくなったりしないように、以下の技術的対策を講じる必要があります。

### 10.1. UI Toolkit コントロールのライトテーマ文字色上書き対策
UI Toolkit で構築するフローティング UI の場合、ルート要素の背景色がダークテーマ（`--dennoko-surface-1` 等）に固定されていても、`TextField`、`ObjectField`、`DropdownField` などのビルトインコントロールの文字色が Unity 標準の Light テーマによって強制的に黒（`#000000` などの暗い色）に上書きされてしまう。
これを防ぐため、USS では単に入力親要素クラスだけではなく、コントロール内部のテキスト要素 `.unity-text-element` や `.unity-label` などの子要素に対しても強制的に白文字色を適用するように優先度を確保する。

```css
/* 例：入力欄と内部文字のカラーを強制固定 */
.dennoko-root .unity-base-field__input,
.dennoko-root .unity-base-field__input .unity-text-element,
.dennoko-root .unity-base-field__input .unity-label,
.dennoko-root .unity-object-field__input .unity-object-field-display__label {
    color: var(--dennoko-text-primary) !important;
}
```

### 10.2. トグル (Toggle) をボタン型トグルにする設計
標準のチェックマーク付きトグルは、ライトテーマでラベルやチェックマークが崩れたり見えなくなったりしやすい。また、設定カードのツールバー等で視覚的に馴染ませるため、他のアクションボタンと揃えた **トグルボタン**（Button をベースにした ON/OFF 表示）を推奨する。

C# 側でクリックイベントを拾って bool 値をトグルし、状態に応じてテキストと USS クラス（`.dennoko-button-active`）を動的に切り替える。

```csharp
// トグル状態の更新ヘルパー例
void UpdateToggleState(Button button, bool enabled, string textOn, string textOff)
{
    if (button == null) return;
    if (enabled)
    {
        button.text = textOn;
        button.AddToClassList("dennoko-button-active");
    }
    else
    {
        button.text = textOff;
        button.RemoveFromClassList("dennoko-button-active");
    }
}
```

### 10.3. IMGUI コントロール併用時のライトテーマ対策
スライダー（`EditorGUILayout.Slider`）の数値入力欄など、一部で IMGUI (`OnGUI()`) コントロールを併用する場合、`PushEditorTheme` でテキストのフォント色を白に強制していると、ライトテーマ下では入力欄の背景画像が白いため「白い背景に白文字」となり数値が全く読めなくなる。
これを防ぐには、`PushEditorTheme` において、`EditorStyles.numberField` や `EditorStyles.textField` に対しても、入力欄専用の暗い背景テクスチャを強制適用する。

```csharp
// PushEditorTheme() 内での実装例
FixAllStateBackgrounds(EditorStyles.numberField, _texSearchField); // 暗い背景を強制
FixAllStateBackgrounds(EditorStyles.textField, _texSearchField);
FixAllStateBackgrounds(GUI.skin.textField, _texSearchField);
```

### 10.4. IMGUI `ObjectField` の Select ボタン重なりバグの回避
`typeof(Texture2D)` などの画像アセット型を対象とする `ObjectField` を描画する際、ラベルを空（`""`）にして `labelWidth = 1f` にした状態で 1行の高さ（20px程度）に押し込んで描画すると、Unity がサムネイルプレビュー表示モード（正方形）になろうとして描画領域が縦に潰れ、アセット選択ボタンである「Select」がテキスト表示領域と重なって崩れてしまうバグが発生する。
これを回避するため、画像選択の `ObjectField` にはラベル（例: `"画像"`) を直接指定し、代わりに `EditorGUIUtility.labelWidth` を設定して描画することで、通常の「1行テキスト＋丸ポチボタン」モードで安全に描画させる。

```csharp
// バグを回避する安全な 1 行描画の例
float originalLabelWidth = EditorGUIUtility.labelWidth;
EditorGUIUtility.labelWidth = 48f; // 他のコントロールのラベル幅と揃える
var tex = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("画像", "説明ツールチップ"),
                                                 currentTex, typeof(Texture2D), false, GUILayout.Height(RowH));
EditorGUIUtility.labelWidth = originalLabelWidth;
```

---

## 11. 動作確認チェックリスト

1. **Unity エディタの Preferences でテーマを Light / Dark 両方に切り替えたか？**
   - ラベルが黒くなって読めない、ボタン背景が白く浮く、
     ドロップダウン矢印やチェックマークが消える箇所がないか確認する。
   - 特に `TextField`、`ObjectField`、`DropdownField` の入力文字列やプレビュー選択テキストが、ライトテーマ下でも白文字（または暗い背景にしっかりとコントラストのある色）に維持されているか確認する。
   - IMGUI の数値入力欄やスライダーが「明るい背景に白文字」になって読めなくなっていないか確認する。
2. **画像・テクスチャ用の `ObjectField` で「Select」ボタンが崩れて被っていないか？**
   - ラベル無しで極端に狭い高さの `ObjectField` を作るとサムネイルモードの潰れバグが発生するため、ラベル付きで `labelWidth` を設定する手法に倣っているか確認する。
3. **UXML / USS の GUID プレースホルダーを実際の GUID に置き換えたか？**
4. **ルート要素に `dennoko-root` クラスを付けたか？**
   - 付け忘れると変数が解決されず、全要素がエディタテーマの見た目に戻る。
5. **C# で `new VisualElement()` を手組みしてスタイリングしていないか？**
   - 動的リスト以外のレイアウトは UXML へ。色指定は USS 変数へ。
6. **Inspector の場合、`Bind(serializedObject)` を呼んだか？**
7. **ウィンドウを極端に狭く / 広くしてレイアウト崩れがないか？**
   - Flexbox なので IMGUI より崩れにくいが、`flex-shrink` 由来の潰れに注意。
