# dennoko.dev Color Schema — Unity UI Toolkit 実装ガイド

`colors_spec.md` / `design_reference.md` で定義したカラースキーマを **Unity Editor 拡張 (UI Toolkit / UXML / USS)** に適用するためのガイドです。

- **ターゲット環境**: Unity 2022.3 ～ Unity 6
- **最優先要件**: Unity エディタのテーマ設定 (Personal Light / Dark 等) に左右されず、常に dennokoworks の「フローティングデザイン (ダークテーマ)」を維持すること

---

## まず読むもの（このリポジトリだけで実装できる）

```
1. ../example/index.html             ← ビジュアルターゲット（ブラウザで開く）
2. ../Docs/design_reference.md       ← デザインコンセプト（フローティング・Elevation）
3. ../Docs/colors_spec.md            ← カラー仕様（各色の役割）
4. ../colors.json                    ← カラー実値（#RRGGBB）
5. forUnity/uss_theme_template.md    ← テーマ USS 全文（コピー元）
6. forUnity/window_structure_template.md     ← EditorWindow の UXML + C# 骨格（コピー元）
7. forUnity/inspector_structure_template.md  ← CustomEditor (Inspector) の骨格（コピー元）
8. forUnity/techniques.md            ← UI Toolkit 固有の実装テクニック詳説
```

---

## このフォルダのファイル構成

| ファイル | 内容 |
|---|---|
| `README.md` | このファイル。概要と全体手順 |
| `uss_theme_template.md` | コピーして使うテーマ USS (`DennokoTheme.uss`) 全文 |
| `window_structure_template.md` | EditorWindow の UXML + C# 骨格 |
| `inspector_structure_template.md` | CustomEditor (Inspector) の UXML + C# 骨格 |
| `topbar_version_template.md` | トップバーのバージョン表記 + GitHub アップデートチェック（UXML/USS + 自己完結 C# ヘルパー） |
| `techniques.md` | UI Toolkit 固有の実装テクニック・罠・IMGUI からの移行マッピング |

---

## 実装の役割分担（基本方針）

| レイヤー | 担当 |
|---|---|
| UI 構造 | **UXML** に書く。C# で `new VisualElement()` を手組みしない（動的リストは例外） |
| スタイル | **USS** (`DennokoTheme.uss`) に書く。カラーは USS 変数 `--dennoko-*` を経由し、ハードコード禁止 |
| ロジック | **C#** は UI の生成 (`CreateGUI` / `CreateInspectorGUI`)・アセットロード・イベント接続に専念 |

### テーマ非依存の仕組み

`DennokoTheme.uss` は `.dennoko-root` を起点とする子孫セレクタで
Unity ビルトインコントロール（`.unity-button`、`.unity-base-field__input` 等）を上書きする。
詳細度がエディタテーマのデフォルト定義より高いため、Light / Dark どちらのテーマでも
常に dennokoworks のダークデザインが勝つ。

> **重要: ルート要素に必ず `dennoko-root` クラスを付与すること。**
> USS 変数は `.dennoko-root` に定義されており子孫にのみ継承される。
> 付け忘れるとスタイル全体が無効になる。仕組みの詳細は `techniques.md` セクション 1 を参照。
>
> **重要: USS に `!important` は存在しない**（書くと宣言が破棄される）。
> 独自クラス（`.dennoko-*`）のセレクタは必ず `.dennoko-root` を前置し、
> 詳細度で汎用リセットに勝たせる規約とする。`techniques.md` セクション 1 を参照。

| CSS の概念 | Unity UI Toolkit での実現方法 |
|---|---|
| `background-color` (surface-0) | `.dennoko-root { background-color: var(--dennoko-surface-0); }` |
| `border: 1px solid` | `border-width: 1px; border-color: var(--dennoko-outline);`（ショートハンド不可） |
| `box-shadow` (Elevation) | USS に box-shadow はない。surface-1 と surface-0 の明度差で浮いて見せる |
| `hover` | `:hover` 疑似クラス |
| `color: typography.tertiary` | `.dennoko-text-tertiary` クラス、または `color: var(--dennoko-text-tertiary)` |

---

## 実装手順

### Step 1 — テーマ USS をコピーする

`uss_theme_template.md` のコードブロックを `Editor/UI/DennokoTheme.uss` として配置する。
このファイルは原則そのまま使う（プロジェクト固有クラスの追加は可）。

### Step 2 — UXML と C# の骨格をコピーする

- EditorWindow → `window_structure_template.md` の UXML / C# を配置
- CustomEditor (Inspector) → `inspector_structure_template.md` の UXML / C# を配置

変更箇所:
- `namespace` / クラス名
- `[MenuItem("Tools/Your Tool Name")]` のメニューパス（EditorWindow）
- `[CustomEditor(typeof(YourComponent))]` の型（Inspector）

### Step 3 — GUID を設定する

Unity にインポート後、UXML / USS の `.meta` ファイルから GUID を控え、
C# の `UXML_GUID` / `USS_GUID` 定数に設定する。
パス直書きではなく GUID ロードを使うことで、アセット移動に耐える。

### Step 4 — セクションを追加する

UXML に `.dennoko-card` ブロックを追加していく。

**常時表示セクション**

```xml
<ui:VisualElement class="dennoko-card">
    <ui:Label text="INPUT" class="dennoko-section-title dennoko-card-header" />
    <uie:ObjectField label="Source" name="source-field"
        type="UnityEngine.Texture2D, UnityEngine.CoreModule" />
</ui:VisualElement>
```

**ON/OFF トグル付きセクション** — UXML にトグル付きヘッダーを置き、
C# で `BindToggleSection()`（`window_structure_template.md` 参照）を呼ぶ。

```xml
<ui:VisualElement class="dennoko-card">
    <ui:VisualElement class="dennoko-card-header dennoko-toggle-header">
        <ui:Toggle name="cc-toggle" text="COLOR CORRECTION" value="true"
            class="dennoko-section-title" />
        <ui:Button name="cc-reset" text="Reset" />
    </ui:VisualElement>
    <ui:VisualElement name="cc-content">
        <ui:Slider label="Hue" name="hue-slider" low-value="-180" high-value="180"
            show-input-field="true" />
    </ui:VisualElement>
</ui:VisualElement>
```

- `toggle = true` → コンテンツ有効（通常表示）
- `toggle = false` → `SetEnabled(false)` で自動的にグレーアウト・操作不可

### Step 5 — ステータスバーを使う

UXML 末尾に `<ui:Label name="status-label" text="Ready" class="dennoko-status" />` を置き、
C# の `SetStatus()` ヘルパー（`window_structure_template.md` 参照）でクラスを切り替える。

```csharp
SetStatus("Saved.", StatusType.Success);   // .dennoko-status--success が付与され 3 秒後に Ready へ戻る
SetStatus("Failed.", StatusType.Error);    // .dennoko-status--error
```

### Step 6 — セパレーターを追加する

```xml
<ui:VisualElement class="dennoko-separator" />
```

---

## カラーパレット早見表

`colors.json` / `Docs/colors_spec.md` の内容を USS 変数名で対応させたもの。
すべて `.dennoko-root` に定義されている。

| 役割 | HEX | USS 変数 |
|---|---|---|
| アプリ背景 | `#121212` | `--dennoko-surface-0` |
| カード・入力欄 | `#1e1e1e` | `--dennoko-surface-1` |
| ツールバー・ホバー | `#2c2c2c` | `--dennoko-surface-2` |
| 境界線・セパレーター | `#3a3a3a` | `--dennoko-outline` |
| タイトル文字 | `#ffffff` | `--dennoko-text-primary` |
| 本文・ラベル | `#cccccc` | `--dennoko-text-secondary` |
| 補足・見出し | `#aaaaaa` | `--dennoko-text-tertiary` |
| 無効状態文字 | `#555555` | `--dennoko-text-disabled` |
| エラー | `#9b1b30` | `--dennoko-semantic-error` |
| 警告 | `#ffb74d` | `--dennoko-semantic-warning` |
| 成功 | `#4caf50` | `--dennoko-semantic-success` |
| 情報 | `#64b5f6` | `--dennoko-semantic-info` |
| アクセント | `#ffffff` | `--dennoko-accent` |
| ホバーオーバーレイ | `rgba(255,255,255,0.05)` | `--dennoko-hover-overlay` |

---

## よくある疑問

**Q: スタイルがまったく適用されない・エディタテーマの見た目のままになる**

A: 原因はほぼ次の 3 つ。

1. ルート要素に `dennoko-root` クラスを付け忘れている（USS 変数が継承されない）
2. `USS_GUID` がプレースホルダーのまま、または間違っている
3. `root.styleSheets.Add(uss)` を呼んでいない

**Q: Light テーマに切り替えると一部の文字やアイコンが見えなくなる**

A: `DennokoTheme.uss` のオーバーライドでカバーしていないビルトイン要素を使っている。
ドロップダウン矢印やチェックマーク等の**画像**は対策が必要。ただし
`-unity-background-image-tint-color` は**乗算**のため、Light テーマの黒いアイコンは
tint では明るくできない。`background-image: resource("d_dropdown")` のように
d_ 系（ダークスキン用）アイコンを明示指定して画像自体を差し替える。
`techniques.md` セクション 4 を参照。

**Q: ドロップダウンを開いたときのメニュー（ポップアップ）だけエディタテーマの見た目になる**

A: 既知の限界（仕様）。ポップアップメニューは別ウィンドウ（別パネル）として描画され
`.dennoko-root` の外側にあるため、USS を適用できない。制御できるのはフィールド本体まで。

**Q: Foldout の矢印が白い四角の箱になる**

A: Toggle のチェックボックス装飾が Foldout の矢印要素に波及している。
`DennokoTheme.uss` には打ち消しルールが定義済みなので、テーマを改変した場合は
そのルールを消していないか確認する。`techniques.md` セクション 3 を参照。

**Q: `border: 1px solid #3a3a3a` と書いたらエラーになる**

A: USS は CSS のショートハンド `border` / `background` / `font-weight` を
サポートしない。`border-width` + `border-color`、`background-color`、
`-unity-font-style: bold` に分けて書く。`techniques.md` セクション 6 の対応表を参照。

**Q: Inspector で PropertyField が空で表示される**

A: `container.Bind(serializedObject)` の呼び忘れ。`inspector_structure_template.md` を参照。

**Q: 旧 IMGUI 実装（UniTexTheme / DrawSection）からの移行方法は？**

A: `techniques.md` セクション 8 に IMGUI → UI Toolkit の対応表がある。
`PushEditorTheme` / テクスチャキャッシュ / `FixAllTextColors` などの
IMGUI 回避策は UI Toolkit ではすべて不要になる。
