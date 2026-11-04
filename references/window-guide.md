# EditorWindow 実装ガイド

コピー元コード: [assets/EditorWindow/YourEditorWindow.uxml](../assets/EditorWindow/YourEditorWindow.uxml) / [assets/EditorWindow/YourEditorWindow.cs](../assets/EditorWindow/YourEditorWindow.cs)
テーマ: [assets/DennokoTheme.uss](../assets/DennokoTheme.uss)

## ウィンドウ全体のレイアウト構成

```
┌──────────────────────────────────────┐
│ [ウィンドウタイトル]          [JA][EN] │  ← .dennoko-header (flex-shrink: 0)
│ ──────────────────────────────────── │  ← .dennoko-separator
│ ┌──────────────────────────────────┐ │
│ │ SECTION TITLE                    │ │  ← .dennoko-card
│ │ ────────────────────────────     │ │     + .dennoko-card-header
│ │  [コンテンツ]                    │ │
│ └──────────────────────────────────┘ │  ← ↑ ScrollView (.dennoko-scroll) の中
│ ┌──────────────────────────────────┐ │     縮小時はここだけが縮む
│ │ [☑] TOGGLE SECTION     [Reset]   │ │  ← .dennoko-toggle-header
│ │  [スライダーなど]                 │ │  ← .dennoko-toggle-section__content
│ └──────────────────────────────────┘ │     (トグル OFF で折りたたまれる)
│ ──────────────────────────────────── │  ← .dennoko-footer の上罫線
│ [Reset All] [  Apply & Save (白)  ]  │  ← .dennoko-footer (カードで囲まない)
│ [ステータスメッセージ]                │  ← .dennoko-status (枠線なし)
└──────────────────────────────────────┘
```

> 枠線は最小限にする。フッターを `.dennoko-card` で囲んだり、フッターの上に
> `.dennoko-separator` を足したりすると、区切り線・カード枠・ボタン枠・ステータスバー枠が
> 何重にも重なって下部が重く見える。

## 配置手順

```
Editor/
├─ UI/
│   ├─ DennokoTheme.uss        ← assets/DennokoTheme.uss をコピー
│   └─ YourEditorWindow.uxml   ← assets/EditorWindow/ からコピー
├─ DennokoUIFont.cs            ← assets/Shared/ からコピー（フォント管理・共通）
└─ YourEditorWindow.cs         ← assets/EditorWindow/ からコピー
```

1. 上記 4 ファイルをコピーして配置する
2. C# の変更箇所: `namespace` / クラス名 / `[MenuItem("Tools/Your Tool Name")]`
   - `DennokoUIFont.cs` も `namespace` を合わせる。Inspector も作る場合、このファイルは
     **プロジェクト内に 1 つだけ**置いて共有する（重複配置はコンパイルエラー）
   - `DennokoUIFont.WarmupJapanese` にツールの UI で使う日本語を書き足す
     （実行中のアトラス追加＝文字化けの原因を減らす。`references/troubleshooting.md` §10）
     - 「代表的な語をいくつか」では足りない。**UXML のテキストとローカライズ用 JSON に
       出現する日本語を全文字入れる**（数百文字でも問題ない）。UI 文言を追加したら更新する
3. Unity インポート後、UXML / USS の `.meta` から GUID を控えて
   C# の `UXML_GUID` / `USS_GUID` 定数に設定する（プレースホルダーのままにしない）
   - GUID の調べ方: `.meta` の `guid:` 行、または Project ビュー右クリック → Copy GUID

## ヘッダーの構成パターン

**標準ヘッダー（タイトルのみ）**
```xml
<ui:VisualElement class="dennoko-header">
    <ui:Label text="YOUR TOOL NAME" class="dennoko-title" />
</ui:VisualElement>
<ui:VisualElement class="dennoko-separator" />
```

**バージョン表記 + リロードボタン付きヘッダー（推奨）**
（C# 側の実装は `references/version-check-guide.md` を参照）
```xml
<ui:VisualElement class="dennoko-header">
    <ui:VisualElement class="dennoko-header-titlegroup">
        <ui:Label text="YOUR TOOL NAME" class="dennoko-title" />
        <ui:Label name="version-label" text="" class="dennoko-version-label" />
        <ui:Button name="version-reload-button" text="↻" class="dennoko-version-reload-button" />
    </ui:VisualElement>
</ui:VisualElement>
<ui:VisualElement class="dennoko-separator" />
```

## セクションの追加パターン

**常時表示セクション** — UXML の ScrollView 内に追加する。

```xml
<ui:VisualElement class="dennoko-card">
    <ui:Label text="SECTION TITLE" class="dennoko-section-title dennoko-card-header" />
    <!-- コンテンツ -->
</ui:VisualElement>
```

**ON/OFF トグル付きセクション** — UXML にトグル付きヘッダーを置き、
C# で `BindToggleSection()`（テンプレートに定義済み）を 1 行呼ぶ。

```xml
<ui:VisualElement class="dennoko-card">
    <ui:VisualElement class="dennoko-card-header dennoko-toggle-header">
        <ui:Toggle name="section-toggle" text="SECTION TITLE" value="true"
            class="dennoko-section-title" />
        <ui:Button name="section-reset" text="Reset" />
    </ui:VisualElement>
    <ui:VisualElement name="section-content" class="dennoko-toggle-section__content">
        <!-- コンテンツ -->
    </ui:VisualElement>
</ui:VisualElement>
```

- `toggle = true` → コンテンツを表示
- `toggle = false` → **折りたたむ**。`BindToggleSection()` がカード（コンテンツの親）に
  `.dennoko-toggle-section--collapsed` を付け、テーマ USS がコンテンツを `display: none` にし、
  見出しの下線と余白も消して見出し行だけのカードにする
- グレーアウト（`SetEnabled(false)`）にはしない。無効な項目は画面から消したほうが読みやすい
- 構造の前提: コンテンツはカードの**直下**に置き、`.dennoko-toggle-section__content` を付ける
  （折りたたみの USS は子セレクタ `>` で書かれている）

**トグル（チェックボックス）は囲まない** — チェックボックスとそのラベルは
「入力欄」ではなくテキスト行として見せる。トグル自体にも、トグルで開閉・
有効化する項目のまとまりにも枠や背景の囲みを付けない。階層を示したいときは
インデント（`margin-left`）を使う。

```xml
<ui:Toggle name="overwrite-toggle" text="上書き保存を有効にする" />
<!-- トグルで制御される項目。カードや枠では囲まず、インデントだけで従属を示す -->
<ui:VisualElement name="overwrite-group" style="margin-left: 16px;">
    <!-- ... -->
</ui:VisualElement>
```

囲みが出てしまう場合は references/troubleshooting.md §4-② を見る
（テーマ USS の §④で打ち消し済み。テーマを改変したときに再発しやすい）。

**Toggle は `text=` で書く** — `text=` は「左にチェックボックス、右に文字」の行になる。
`label=` は「左ラベル列（150px）+ 右にチェックボックス」の 2 カラムになり、
同じカードに混在すると左右が逆転してちぐはぐに見える。`label=` は他の入力欄と
同じ列に揃えたい場合だけ使う。

**ツールバー** — カードの最上部は常に見出し（`.dennoko-card-header`）にし、
ツールバーはその下に置く（どのカードも「見出し + 下線」から始まる構造を崩さない）。

```xml
<ui:VisualElement class="dennoko-card">
    <ui:Label text="SECTION TITLE" class="dennoko-section-title dennoko-card-header" />
    <ui:VisualElement class="dennoko-toolbar">
        <ui:Button text="New" />
        <ui:Button name="grid-button" text="Grid" />  <!-- ON 時は .dennoko-button-active -->
    </ui:VisualElement>
    <!-- コンテンツ -->
</ui:VisualElement>
```

**タブ** — Unity 2022.3 には TabView がないため、ボタン列 + パネル切替で作る。
選択中のタブボタンに `.dennoko-button-active` を付け、非選択パネルは
`display: none` 用のクラスを付け外しする（`EnableInClassList`）。

**読み取り専用・複数行・HelpBox** — ビルトインクラスで判別できないため、独自クラスを併記する。

```xml
<ui:TextField label="Path" value="..." readonly="true" class="dennoko-field-readonly" />
<ui:TextField label="Memo" multiline="true" class="dennoko-field-multiline" />
<ui:HelpBox text="..." message-type="Warning" class="dennoko-helpbox--warning" />
```

**伸びる入力欄 + 固定幅ボタン** — `.dennoko-hrow` と `.dennoko-col-grow`（テーマ USS に定義済み）を使う。

```xml
<ui:VisualElement class="dennoko-hrow">
    <uie:ObjectField label="Target" class="dennoko-col-grow" />
    <ui:Button text="Clear" style="width: 64px; flex-shrink: 0;" />
</ui:VisualElement>
```

**セパレーター**

```xml
<ui:VisualElement class="dennoko-separator" />
```

## フッター

カードで囲まず、`.dennoko-footer` の上罫線だけでスクロール領域と仕切る。
ボタンは 1 行に並べ、**左に固定幅の Secondary（Reset All 等）、右に伸びる Primary（Apply 等）**を置く。
主アクションと補助・破壊的アクションを同じ幅で上下に並べると、優先順位が伝わらず誤クリックも誘発する。

```xml
<ui:VisualElement class="dennoko-footer">
    <ui:Button name="reset-all-button" text="Reset All" class="dennoko-button-secondary" />
    <ui:Button name="apply-button" text="Apply &amp; Save" class="dennoko-button-primary" />
</ui:VisualElement>
```

## ステータスバー

UXML 末尾に `<ui:Label name="status-label" text="Ready" class="dennoko-status" />` を置き、
C# の `SetStatus()` ヘルパー（テンプレートに定義済み）でクラスを切り替える。

```csharp
SetStatus("Saved.", StatusType.Success);   // .dennoko-status--success が付与され 3 秒後に Ready へ戻る
SetStatus("Failed.", StatusType.Error);    // .dennoko-status--error
```

## 注意点

- レイアウト構造は原則すべて UXML に書く。C# で `new VisualElement()` を組み上げない
  （動的リストは例外。references/uss-conventions.md 参照）。
- ロジックから触る要素には `name` 属性を付け、C# 側で `root.Q<T>("name")` で取得する。
- `show-input-field="true"`: Slider に数値入力欄を付ける。スタイルは自動適用される。
- エディタ専用コントロール (`ObjectField`, `PropertyField` 等) は
  `xmlns:uie="UnityEditor.UIElements"` 名前空間で書く。
  - Unity 2022.3 で `ui:` (UnityEngine.UIElements) にあるもの: `TextField` / `IntegerField` / `FloatField` /
    `LongField` / `Vector2〜4Field` / `Vector2IntField` / `RectField` / `BoundsField` / `DropdownField` /
    `EnumField` / `RadioButton(Group)` / `ProgressBar` / `HelpBox` / `ListView`
  - `uie:` (UnityEditor.UIElements) にあるもの: `ObjectField` / `PropertyField` / `ColorField` /
    `CurveField` / `GradientField` / `EnumFlagsField` / `MaskField` / `LayerField` / `LayerMaskField` / `TagField`
- `DropdownField` / `MaskField` / `RadioButtonGroup` の選択肢と `EnumField` の型 (`Init()`) は C# で与える
  （UXML の属性はバージョン差が大きいため）。
- 実装後は references/troubleshooting.md 末尾のチェックリストで Light / Dark 両テーマを確認する。
