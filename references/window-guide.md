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
└─ YourEditorWindow.cs         ← assets/EditorWindow/ からコピー
```

1. 上記 3 ファイルをコピーして配置する
2. C# の変更箇所: `namespace` / クラス名 / `[MenuItem("Tools/Your Tool Name")]`
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
