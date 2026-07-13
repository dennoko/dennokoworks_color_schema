---
name: dennokoworks-design
description: dennokoworks フローティングデザインシステムを Unity Editor 拡張（EditorWindow / CustomEditor / Inspector）に UI Toolkit (UXML/USS) で適用する。エディタテーマ (Light/Dark) に依存しない UI の新規実装・改修・IMGUI からの移行に使う。
---

# dennokoworks Design Skill

**dennokoworks カラースキーマ（フローティングデザイン）** を Unity Editor 拡張の
UI Toolkit (UXML/USS) コードとして実装する。

- **ターゲット環境**: Unity 2022.3 ～ Unity 6
- **最優先要件**: エディタテーマ (Personal Light / Dark) に左右されず、
  常にフローティングデザイン（ダークテーマ固定）を維持すること

## タスク別ルーティング — 必要なものだけ読む/コピーする

| やりたいこと | コピーするファイル | 読むガイド |
|---|---|---|
| EditorWindow を作る | `assets/DennokoTheme.uss` + `assets/EditorWindow/`（UXML/C#） | `references/window-guide.md` |
| Inspector (CustomEditor) を作る | `assets/DennokoTheme.uss` + `assets/Inspector/`（UXML/C#） | `references/inspector-guide.md` |
| テーマ USS だけ欲しい | `assets/DennokoTheme.uss` | —（下の絶対規則だけ守る） |
| 独自の USS クラス/スタイルを追加する | — | `references/uss-conventions.md`（**必読**） |
| バージョン表記 + 更新チェックを付ける | `assets/VersionCheck/`（C# × 2） | `references/version-check-guide.md` |
| IMGUI 実装を UI Toolkit へ移行する | 上記 Window/Inspector 一式 | `references/imgui-migration.md` |
| 見た目が崩れた・テーマ切替で壊れた | — | `references/troubleshooting.md`（症状別表あり） |
| デザインコンセプトを深く知る | — | `Docs/design_reference.md` / `Docs/colors_spec.md` / `example/index.html` |

テンプレートは**実ファイルとして assets/ にある**。内容を転写せずファイルごとコピーし、
ガイド記載のカスタマイズポイント（namespace・クラス名・GUID 等）だけ書き換える。

## 絶対規則（テンプレートを使うだけでも守ること）

1. **ルート要素に `dennoko-root` クラスを付与する。**
   USS 変数の定義元かつテーマ非依存の起点。忘れるとスタイル全体が無効になる。
   EditorWindow は `rootVisualElement`、Inspector は return するコンテナに
   `AddToClassList("dennoko-root")`。
2. **カラーは USS 変数 `var(--dennoko-*)` を経由する。**
   直書きは `DennokoTheme.uss` の変数定義部のみ。UXML インライン style や
   C# の `style.color` に色を書かない。
3. **USS に `!important` は存在しない（書くと宣言が破棄される）。**
   独自クラスのセレクタは必ず `.dennoko-root` を前置し、詳細度で解決する。
   詳細は `references/uss-conventions.md`。
4. **役割分担: UXML = 構造 / USS = スタイル / C# = ロジック。**
   C# で `new VisualElement()` を手組みしない（動的リストは例外）。
5. **UXML / USS は GUID でロードする。**
   テンプレートの `YOUR_*_GUID_HERE` プレースホルダーは、Unity インポート後に
   `.meta` の GUID（または右クリック → Copy GUID）へ必ず置き換える。

## カラーパレット（クイックリファレンス）

すべて `DennokoTheme.uss` の `.dennoko-root` に USS 変数として定義済み。
マスターデータは `colors.json`。

| 役割 | HEX | USS 変数 | 専用クラス |
|---|---|---|---|
| アプリ背景 | `#121212` | `--dennoko-surface-0` | — |
| カード・入力欄背景 | `#1e1e1e` | `--dennoko-surface-1` | — |
| ツールバー・ホバー背景 | `#2c2c2c` | `--dennoko-surface-2` | — |
| 境界線・セパレーター | `#3a3a3a` | `--dennoko-outline` | — |
| タイトル・強調 | `#ffffff` | `--dennoko-text-primary` | `.dennoko-text-primary` |
| 本文・ラベル | `#cccccc` | `--dennoko-text-secondary` | （デフォルト。指定不要） |
| 補足・見出し | `#aaaaaa` | `--dennoko-text-tertiary` | `.dennoko-text-tertiary` |
| 無効状態 | `#555555` | `--dennoko-text-disabled` | `.dennoko-text-disabled` |
| エラー | `#9b1b30` | `--dennoko-semantic-error` | `.dennoko-text-error` |
| 警告 | `#ffb74d` | `--dennoko-semantic-warning` | `.dennoko-text-warning` |
| 成功 | `#4caf50` | `--dennoko-semantic-success` | `.dennoko-text-success` |
| 情報 | `#64b5f6` | `--dennoko-semantic-info` | `.dennoko-text-info` |
| アクセント | `#ffffff` | `--dennoko-accent` | — |

## 主要な UI 部品クラス（DennokoTheme.uss に定義済み）

`.dennoko-card`（セクションカード） / `.dennoko-card-header` / `.dennoko-section-title` /
`.dennoko-toggle-header`（トグル + Reset の横並び） / `.dennoko-header`（ウィンドウヘッダー） /
`.dennoko-title` / `.dennoko-separator` / `.dennoko-scroll`（メインスクロール領域） /
`.dennoko-footer`（フッター。card と併用） / `.dennoko-status`（+ `--success` / `--error`） /
`.dennoko-button-primary` / `.dennoko-button-secondary` / `.dennoko-button-active` /
`.dennoko-toolbar` / `.dennoko-inspector-root`（Inspector 余白打ち消し）

使い方の実例は `assets/EditorWindow/YourEditorWindow.uxml` を見るのが最短。

## デザインコンセプト（要点）

- 全体背景は最も暗い Surface0。コンテンツは Surface1 のカードとして「浮いて」見せる
- USS に box-shadow はない。Elevation は**サーフェス間の明度差**で表現する
- 表面間のコントラストは低め、テキストは高コントラスト（白系）
- 境界線は `--dennoko-outline` で要素の輪郭を示す

## 実装完了時

`references/troubleshooting.md` 末尾のチェックリストを必ず実施する。
特に **Light / Dark 両テーマでの表示確認**は省略しない。
