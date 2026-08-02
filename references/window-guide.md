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
│ │  [スライダーなど]                 │ │  ← name="...-content"
│ └──────────────────────────────────┘ │
│ ──────────────────────────────────── │  ← .dennoko-separator
│ ┌──────────────────────────────────┐ │
│ │ [      Apply & Save (Primary)   ]│ │  ← .dennoko-card .dennoko-footer
│ │ [         Reset All             ]│ │
│ └──────────────────────────────────┘ │
│ [ステータスメッセージ]                │  ← .dennoko-status
└──────────────────────────────────────┘
```

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
3. Unity インポート後、UXML / USS の `.meta` から GUID を控えて
   C# の `UXML_GUID` / `USS_GUID` 定数に設定する（プレースホルダーのままにしない）
   - GUID の調べ方: `.meta` の `guid:` 行、または Project ビュー右クリック → Copy GUID

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
    <ui:VisualElement name="section-content">
        <!-- コンテンツ -->
    </ui:VisualElement>
</ui:VisualElement>
```

- `toggle = true` → コンテンツ有効（通常表示）
- `toggle = false` → `SetEnabled(false)` により Unity が自動でグレーアウト・操作不可

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

**セパレーター**

```xml
<ui:VisualElement class="dennoko-separator" />
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
- 実装後は references/troubleshooting.md 末尾のチェックリストで Light / Dark 両テーマを確認する。
