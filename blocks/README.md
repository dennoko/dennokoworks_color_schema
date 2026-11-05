# 大きい単位の UI 定義（画面パターン・ブロック・シーン操作 UI）

`references/` と `DennokoTheme.uss` が決めるのは**部品**（ボタン・入力欄・カード 1 枚の見た目）。
このフォルダが決めるのは、その部品を組み合わせた**大きい単位**：

- どのカードをどの順に置くか
- 主ボタンはどの状態で何になるか
- シーンビューの操作 UI をどこに出すか

新しいツールを作るときや画面構成を決めるときは、ここから読む。
部品の細かい見た目・詳細度・不具合は `references/` を読む。

## 層

| 層 | 単位 | 例 | 決めること | 定義元 |
| --- | --- | --- | --- | --- |
| L0 | トークン・基本クラス | `--dennoko-surface-1`、`.dennoko-card` | 色・余白・文字 | `assets/DennokoTheme.uss` |
| L1 | 部品 | 標準ヘッダー・標準フッター、アイコンボタン、Segmented | 見た目と 1 つの動作 | `assets/DennokoTheme.uss`、`references/` |
| **L2** | **ブロック** | 対象カード、設定カード、リストカード | 部品の並び、状態ごとの出し分け、空の状態 | **[cards.md](cards.md)** |
| **L3** | **画面パターン** | 変換、手順、一覧編集、シーン作業、コンポーネント編集 | カードの並び順、主ボタンの移り変わり、Idle / Working | **[patterns.md](patterns.md)** |
| **L3** | **シーン操作 UI** | ツールバー、ツールパネル、ラジアル、シーン描画の色 | SceneView 上の置き場所、編集セッションの始め方と終わり方 | **[sceneview.md](sceneview.md)** |

## 使い方（新しいツールを作る順）

1. **画面パターンを 1 つ選ぶ**（[patterns.md](patterns.md) §0 の判定表）。
2. **パターンの雛形をコピー**する（`assets/Patterns/P*.uxml`）。上下の標準ヘッダー・標準フッターはどのパターンでも同じ。
3. **ブロックを並べる**（[cards.md](cards.md)）。足りないところだけ部品で組む。
4. シーンで作業するツールは、**シーン操作 UI を組み合わせる**（[sceneview.md](sceneview.md) §2 の組み合わせ表）。
   パイメニューは [radial-menu.md](radial-menu.md) のテンプレートを使う。

## ファイル

| やりたいこと | コピー元アセット | 読むもの |
| --- | --- | --- |
| 画面全体の構成を決める | `assets/Patterns/P1_Convert.uxml` 〜 `P5_Inspector.uxml` | [patterns.md](patterns.md) |
| カード（ブロック）を置く | `assets/Blocks/DennokoBlocks.uss`<br>`assets/Blocks/DennokoBlockUI.cs`（選択肢・詳細・チップ・キーヒントの接続） | [cards.md](cards.md) |
| シーン上に操作 UI を出す | 共通：`assets/SceneView/Dennoko*.cs` / `DennokoSceneUI.uss`<br>雛形：`assets/SceneView/Your*.cs` / `.uxml`（状態・入力・ツールバー・パネル・情報オーバーレイ） | [sceneview.md](sceneview.md) §5 |
| パイメニューを付ける | `assets/SceneView/DennokoRadialMenu.cs` + `YourRadialMenu.cs` | [radial-menu.md](radial-menu.md) |

- USS は **`DennokoTheme.uss` → `DennokoBlocks.uss` → `DennokoSceneUI.uss`** の順に足す（後ろのファイルが同じ詳細度のルールを上書きする）。
  EditorWindow テンプレートでは `BLOCKS_USS_GUID` に GUID を入れると `DennokoBlocks.uss` も読み込まれる。
- 標準ヘッダー・標準フッターは L1 なので `DennokoTheme.uss`（⑪）にある。ブロックを使わないツールでも使える（`references/window-guide.md`）。
- 雛形の UXML は見本の値（ツール名・文言）が入っている。`name` は C# から `Q` で探すので、変えるときは C# も合わせる。

## 参照実装

UI_Design プロジェクトの **UI Design Test**（`dennokoworks/UI_design_test`）の 14〜16 で、すべて動く状態で確認できる。

| セクション | 中身 | コード |
| --- | --- | --- |
| 14 SCREEN PATTERNS | P1〜P5 を切り替えて表示。空・警告・エラーなどの状態は枠内の ⋮ →「モック」 | `Editor/Mock/MockPatternHost*.cs` |
| 15 SCENE UI | ツールバー型・パネル型の作業を開始して S1〜S6 をシーンに出す | `Editor/Mock/Scene/` |
| 16 RADIAL MENU | パイメニューをシーン中央に固定表示 | `Editor/SceneUI/DennokoRadialMenu.cs`、`Editor/Mock/Scene/MockRadialMenu.cs` |

skill の `assets/SceneView/` は、モックの C# からモック専用の部分（見本の作業面・3 ツール分の切り替え）を外して汎用化したもの。
共通部品（`Dennoko*`）はそのまま使い、雛形（`Your*`）をツールに合わせて書き換える。

## まだ決めていないこと

- パターンは 5 つで足りるか（P1 を「プレビューが主役の型」と「設定だけの型」に分けるか）
- 対象の自動入力：「欄が空のときだけ」に揃えるか、MaskMakerV4 のように「Idle 中は常に選択に追従」にするか
- 作業中の示し方：S2 のセッション行だけで足りるか
- S1 / S2 の既定の位置（S1 左下・S2 右下）、ラジアルのキーを全ツール C に揃えるか
- キーヒントの常設（S2 に 2 組まで常設か、常設 0 か）
- 同時に 1 セッションの制限でよいか
- シーン用アイコンボタン（26px）をウィンドウ用（20px）とクラス名で分けるか（今は `.dennoko-scene` 前置で上書き）
