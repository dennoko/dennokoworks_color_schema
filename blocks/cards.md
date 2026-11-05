# ブロック（L2・カード単位）

画面パターン（[patterns.md](patterns.md)）の中に並べるカード。見た目は `assets/Blocks/DennokoBlocks.uss`、
繰り返し使う接続処理は `assets/Blocks/DennokoBlockUI.cs`。マークアップの実例は `assets/Patterns/` の雛形にある。

| # | ブロック | 目印のクラス | 使うパターン | 置き換え対象 |
| --- | --- | --- | --- | --- |
| B1 | 対象カード | `.dennoko-target-card` | 全部 | 各ツールの「対象」「入力」カード、MaskMakerV4 の TargetCard |
| B2 | 設定カード | `.dennoko-options-card` | 全部 | 各ツールの設定カード、`.dennoko-toggle-header` のセクション |
| B3 | プレビューカード | `.dennoko-preview-card` | P1 / P2 / P4 | DennokoMAT / HairChimera / NormalMapGenerator / UniTex / MaskMakerV4 の UV ビュー |
| B4 | リストカード | `.dennoko-list-card` | P3（P2 の一部） | DenEmo / BatchRenamer / DennokoMAT の一覧 |
| B5 | 出力（ドック / カード） | `.dennoko-output` / `.dennoko-output-card` | P1 / P2 / P4 | NormalMapGenerator の OUTPUT、MaskMakerV4 の OutputCard |
| B6 | 実行前確認 | `.dennoko-list-card` の見出しの件数チップ | P3 | BatchRenamer のプレビュー、FolderCleaner の削除候補 |
| B7 | 作業中バナー | `.dennoko-session-banner` | P4 / P5 | MaskMakerV4 の対象カード（作業中）、各 Inspector の「編集中」表示 |

- どのカードも `.dennoko-card` に目印のクラスを足す（`class="dennoko-card dennoko-target-card"`）。目印のクラス自体にはスタイルが無いものもある（`-target-card` / `-options-card` / `-preview-card` / `-output-card`）。C# から `Q(className:)` で探す・後で見た目を足すための名前。
- 出力ドック（`.dennoko-output`）は標準フッターの一部なので `DennokoTheme.uss`（⑪）にある。

## カードの中で共通に使う部品

| 部品 | クラス | 使い方 |
| --- | --- | --- |
| 見出し行 | `.dennoko-card-header.dennoko-card-header--row` + `.dennoko-card-header__title` | 見出し + 右端に `?`（ヘルプ）と `↺`（このカードだけ初期値に戻す）の `.dennoko-icon-button` |
| ラベル付きの行 | `.dennoko-field-row` + `.dennoko-field-row__label` | 組み込みフィールドではない行（選択肢グループなど）をフィールドと同じラベル幅に揃える |
| 選択肢グループ | `.dennoko-segmented.dennoko-choice-group` | enum の項目が 4 つまでのとき。ドロップダウンにしない。`DennokoBlockUI.BindChoice` |
| 詳細 | `.dennoko-details`（`__header` / `__arrow` / `__title` / `__chips` / `__content`） | 閉じた状態が既定。閉じている間は `__chips` に要約チップ。`DennokoBlockUI.BindDetails` |
| 要約チップ | `.dennoko-summary-chip`（`--clickable`） | アイコン + 値。初期値のままの項目は出さない。`DennokoBlockUI.Chip` |
| 情報行 | `.dennoko-info-row` | Tertiary の小さな文字を `·` 区切りで並べる（サイズ・形式など） |
| 警告行 | `.dennoko-inline-warning`（`__text` / `__action`） | 1 行の警告 + 直すボタン 1 つ |
| 空の状態 | `.dennoko-empty-state`（`--overlay` / `__message`） | 中央に案内 1 行 + 次の操作のボタン |
| キーキャップ | `.dennoko-keycap` + `.dennoko-keyhint-label` | ツールチップ以外でキーを示す所すべて。`DennokoBlockUI.AddKeyHint` |
| タブ / 手順タブ | `.dennoko-tabbar`（`--steps`）+ `.dennoko-tab`（`__badge` / `__label`、`--done` / `--pending`） | スクロール領域の外（トップバーの直下）に固定 |
| 補足文 | `.dennoko-hint` | 10px・Tertiary・折り返しあり |

## B1 対象カード

ツールが何に対して働くかを決めるカード。常にスクロール領域の先頭に置く。

```
┌─────────────────────────────────────┐
│ [Body (SkinnedMeshRenderer)] [選択から] │  .dennoko-target-row（ObjectField + __action）
│ [Body_skin ▼]  [UV0 ▼]               │  .dennoko-target-sub（下位の選択。あるときだけ）
│ 頂点 12,480 · Read/Write 有効          │  .dennoko-info-row（任意）
└─────────────────────────────────────┘
```

| 状態 | 表示 |
| --- | --- |
| 空 | 対象を選ぶ行だけ。主ボタンは無効 |
| 指定済み | 対象を選ぶ行 + 下位の選択 + 情報行 |
| 問題あり | 情報行の代わりに警告行 + 修正ボタン 1 つ（例：「Read/Write が無効です [有効にする]」）。自動で直せるものは直して情報行で知らせる |
| 候補が複数 | ルートオブジェクトを選んだとき、行の下に子 Renderer の一覧（`.dennoko-candidates` / `.dennoko-candidate`）を出し、クリックで決定 |
| 作業中（P4） | ObjectField をやめて「アイコン + 名前 ▼」（`.dennoko-target-working`）にし、▼ で対象を切り替える |

- Hierarchy / Project の選択からの自動入力は、**欄が空のときだけ**行う。指定済みの値は勝手に変えない。
- 見出しは付けない（中身で対象と分かる）。P1 で入力が 2 つ以上あるときだけ「入力」「参照」などの見出しを付ける。
- `ObjectField` のプレースホルダ（対象の種類）は Unity が型名から出す。TextField の案内文は `DennokoBlockUI.AddPlaceholder`。

## B2 設定カード

```
┌ ブラシ ──────────────────────── ? ↺ ┐  見出し + ヘルプ + このカードだけのリセット
│ 半径     ━━━━━●━━━━━   [0.025]       │
│ 減衰     [滑らか][直線][鋭い][一定]   │  選択肢グループ（.dennoko-field-row の中）
│ ☑ ミラー                              │
│ ▸ 詳細   [X 軸] [ルート基準]          │  閉じているときは値のチップで要約
└─────────────────────────────────────┘
```

- 1 枚に置く行は 7 行まで。超えるならカードを分けるか、「詳細」に入れる。
- 「詳細」は閉じた状態を既定にし、閉じている間は今の値を要約チップで見出し行に並べる。初期値のままの項目はチップを出さない。
- 見出し右の ↺ はそのカードだけを初期値に戻す。Undo で戻せるので確認ダイアログは出さない（トップバー ⋮ の「設定を初期値に戻す…」は全体なので確認を出す）。
- トグル付きセクション（OFF なら中身を使わない）は見出し行にトグルを置き、OFF のときは中身を折りたたむ（`BindToggleSection`、`references/window-guide.md`）。
- ラベル幅はカード内で揃える。値だけで意味が通る項目はラベルを省く。

## B3 プレビューカード

```
┌─────────────────────────────────────┐
│                          [◎][◐][□] │  .dennoko-preview__toolbar（アイコンボタン 3 個まで）
│          （プレビュー）              │  .dennoko-preview__image
│                                     │
├─────────────────────────────────────┤
│ 1024 × 1024 · 0.4 秒前に更新          │  .dennoko-info-row
└─────────────────────────────────────┘
```

| 状態 | 表示 |
| --- | --- |
| 空 | 中央に案内 1 行（`.dennoko-preview__message`：「入力を選ぶとここに表示されます」）。ドロップを受けるならドロップ領域を兼ねる |
| 計算中 | 前の画像を残したまま、上端に細い進捗バー（`.dennoko-preview__progress`）。前の画像が無ければ中央に「計算中…」 |
| 古い | 設定を変えたが未更新のとき、右上に「更新」チップ（`.dennoko-preview__stale`。重いプレビューのみ） |
| エラー | 中央に原因 1 行（`.dennoko-preview__message--error`） |

- 更新の方針は重さで決める：軽い → 値の変更ごとに即時、中くらい → 最後の変更から 300ms 後、重い → 手動の「更新」と「古い」表示。
- 縦横比は「正方形」「入力に合わせる」の 2 種類。カードの幅に合わせて高さを決め、ウィンドウを縮めたら一緒に縮む。
- 大きく見たいツールは右上のツールバーに「別ウィンドウで開く」を置く。

## B4 リストカード

```
┌ 確認 ─────────── [変更 12] [変更なし 3] [衝突 1] ┐  見出し + 件数チップ（B6）
│ [検索            ✕]                    12 / 480 │  .dennoko-searchbar（固定）
│ ☐ [icon] 名前                補足           [⋮] │  .dennoko-list-row（ListView の行）
│ ☑ [icon] 名前                補足           [⋮] │
└─────────────────────────────────────────────────┘
```

- 検索行は伸びる領域の外（リストの上）に固定する。右端の数字（`.dennoko-searchbar__count`）は「表示中 / 全体」。
- 一覧は `.dennoko-list-card__body` の中の ListView。**P3 ではこのカードを伸びる領域にし、ページ全体の ScrollView に入れない。**
- 空の状態は 2 種類を区別する。対象が無い →「フォルダを選ぶと一覧が出ます」+ 次の操作のボタン。絞り込みで 0 件 →「一致する項目がありません [絞り込みを解除]」（`.dennoko-empty-state--overlay`）。
- 行の右端に置く操作は 2 個まで。それ以上は ⋮ のメニューに入れる。一括操作は行に置かずフッターに置く。
- クリックで選択、ダブルクリックで Ping / 開く、Space で選択の ON/OFF、↑↓ で移動。複数選択は Ctrl / Shift。
- 変更前 → 変更後を並べる行は `.dennoko-list-row__name` → `__arrow` → `__new`、補足は `__sub`。変わらない行は `--unchanged`、衝突は `--conflict`。

## B5 出力（ドックとカード）

| 出力設定の量 | 置き方 | 例 |
| --- | --- | --- |
| 2 行まで（出力先 + 上書き程度） | 出力ドック（`.dennoko-output`）だけ。常に見える | NormalMapGenerator / UniTex |
| 3 行以上（名前・解像度・チャンネルなど） | スクロール領域の最後に出力カード、ドックには要約チップ（`.dennoko-output__chips`）だけ。チップをクリックするとカードへスクロール | MaskMakerV4 |

- 出力ドックの行は `.dennoko-output-row` + `.dennoko-output__label`（44px）+ 伸びる欄 + `.dennoko-output__action`（56px）。選択肢（形式など）は `.dennoko-segmented.dennoko-choice-group` をそのまま置くと文字の幅で並ぶ。
- ドックの要約チップはカードと同じ値を出す。カードが画面内に見えているときはチップを隠す（同じ情報を 2 か所に出さない）。
- 出力ドックの直後のフッターには `.dennoko-footer--joined` を付けて上罫線を重ねない。

## B6 実行前確認

取り消せない・影響が大きい操作の前に、何が起きるかを一覧で見せる。B4 リストカードの見出しに種類別の件数チップ（`.dennoko-chip`、警告は `--warning`）を並べた形にする。

- 件数チップをクリックするとその種類だけに絞る（`.dennoko-button-active` で選択中を示す）。
- 衝突・エラーがあるときは主ボタンを無効にせず、確認ダイアログに件数を出す（「1 件は衝突のためスキップされます」）。
- 削除は「削除」ではなく「ゴミ箱へ移動」にできるならそうする（`AssetDatabase.MoveAssetToTrash`）。

## B7 作業中バナー

シーンで作業中であることを、ウィンドウ・Inspector 側に示す 1 行。P4 / P5 の Working で、スクロール領域の外（トップバー / タブの直下）に固定する。

```
│ ● 編集中  Body        [Esc] で終了   [終了] │
```

- 構成：`.dennoko-session-dot` + `__state`（「編集中」）+ `__target`（対象名）+ `__hint`（キーキャップ）+ `.dennoko-button-secondary`（「終了」）。
- 背景は `--dennoko-selection-bg`、左端に 3px の `--dennoko-selection-border`。「●」も selection 色。
- 対象名をクリックすると SceneView をその対象にフレームする。
- 「終了」は Secondary ボタン。シーンのツールパネルの ✕・Esc と同じ処理を呼ぶ。
- 同じ情報はシーン側のツールパネル（S2）のセッション行にも出す。ウィンドウとシーンのどちらを見ていても作業中と分かるようにするため、ここは重複を許す。
