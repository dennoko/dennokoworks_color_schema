# Color Schema Specification

このドキュメントは `colors.json` で定義されているブランドカラーの仕様書です。
GoogleのMaterial Design 3 (M3) とAppleのHuman Interface Guidelines (HIG) の考え方をベースにした「堅牢なモノトーン」を定義しています。

Unity 側の実体は `assets/DennokoTheme.uss` の `.dennoko-root` に定義された USS 変数です
（対応表は末尾）。値を変える場合は `colors.json` と USS の両方を更新します。

## JSON構造とカラーの役割

### `theme.surface` (ニュートラル・レイヤー)
ツール全体の基盤となる色です。情報の優先度を「背景とのコントラスト（明度）」や「重なり（Elevation）」で表現します。

*   **`level0` (`#121212`)**: アプリ全体の最背面の背景色。視覚的なノイズを極限まで抑えます。**文字入力欄**（TextField / 数値欄 / ObjectField）もこの色に「沈め」、カードの中で入力する場所であることを示します。
*   **`level1` (`#1e1e1e`)**: カード。背景より一段明るくし、浮いている印象を与えます（フローティングデザインの基盤）。
*   **`level2` (`#2c2c2c`)**: **ボタン・選択欄**（Dropdown / Enum 等）・ツールバー。カードより一段「浮かせ」、押せる・選べる要素であることを示します。
*   **`level3` (`#383838`)**: `level2` に浮かせた要素のホバー状態。

> 使い分けの要点: 「入力する場所は沈める (level0)、押す・選ぶものは浮かせる (level2)」。
> 同じトーンにすると、クリック対象か入力対象かが見分けられなくなります。

### `theme.outline` (`#484848`) / `theme.outlineHover` (`#5e5e5e`)
境界線やセパレーター。非常に低コントラストなグレーを使用し、要素を区切りつつも目立たせません。
沈めた入力欄のホバーは背景色ではなく、枠線を `outlineHover` に明るくして示します。

### `theme.typography` (タイポグラフィ)
情報の階層化を明確にするために文字色を分けています。

*   **`primary` (`#ffffff`)**: 最も重要な情報（タイトルや見出し）。
*   **`secondary` (`#cccccc`)**: 少し明度を落としたグレー。説明文や通常テキスト。
*   **`tertiary` (`#aaaaaa`)**: さらに暗いグレー。補足情報、メタデータ、キャプション、読み取り専用の値。
*   **`disabled` (`#555555`)**: 操作不能であることを示す文字色。無効状態はすべてのコンポーネントでこの色に統一します。

### `theme.semantic` (セマンティック・カラー)
意味や状態を伝えるためのアクセントカラーです。堅牢なモノトーンの世界観を壊さないよう、彩度を少し抑えています。

*   **`error` (`#9b1b30`)**: 致命的な問題や削除。暗く鮮やかな赤（ワインレッド）。**塗り・枠線専用**です。
*   **`errorText` (`#ff6b6b`)**: エラーの**文字**用。`error` は暗背景に対するコントラストが低く文字が読めないため、文字には必ずこちらを使います。
*   **`warning` (`#ffb74d`)**: 注意、推奨設定。
*   **`success` (`#4caf50`)**: 完了、正常。
*   **`info` (`#64b5f6`)**: 通知、ヒント。

### `theme.interaction` (インタラクション)
ユーザーのアクションに対するフィードバックです。

*   **`accent` (`#ffffff`)**: フォーカス枠、Primary ボタンの塗り、チェックボックス / ラジオ ON の塗り。画面内で最も目立たせるべき箇所に使用します。
*   **`checkmark` (`#121212`)**: チェックマーク / ラジオの点。ON 時の白 (`accent`) 塗りの上に重ねるため、暗色でコントラストを最大にします（OFF 時は暗色の箱 + 枠線のみで、白塗りにはしません）。
*   **`hoverOverlay` (`rgba(255, 255, 255, 0.05)`)**: 要素に重ねることで、ホバー時の視覚的なフィードバック（明るくなる効果）を付与します。リスト行のホバーに使用します。

### `theme.selection` (選択状態)
「複数の中から選ばれている」ことを示す色です。`accent`（フォーカス・最優先アクション）とは役割を分けます。

*   **`border` (`#64b5f6`)**: 選択中の枠線（排他選択ボタン・タブ・ON/OFF ボタン）、MinMaxSlider の範囲バー。
*   **`background` (`rgba(100, 181, 246, 0.16)`)**: 選択中の背景ティント、リストの選択行。
*   **`backgroundHover` (`rgba(100, 181, 246, 0.26)`)**: 選択中要素のホバー。

> 選択状態は枠線の色だけ・背景色だけで表現せず、両方を併用します。

### `theme.component` (コンポーネント派生色)

*   **`listAltBackground` (`rgba(255, 255, 255, 0.025)`)**: リストの交互行。差が大きいと交互行が選択中・ホバー中に見えるため、ごくわずかに留めます。
*   **`progressFill` (`rgba(100, 181, 246, 0.55)`)**: ProgressBar の塗り。上に重なる白文字が読める明度に抑えた `info`。

### `theme.status` (ステータスバー / HelpBox)
semantic 色を暗背景に馴染ませた派生色です。

*   **`successBackground`**: 成功時のステータスバー背景。
*   **`errorBackground`**: エラー時のステータスバー / HelpBox 背景。
*   **`errorText` (`#ffa6a6`)**: `errorBackground` の上で読める明度に調整したエラー文字色。
*   **`warningBackground`** / **`infoBackground`**: 警告 / 情報の HelpBox 背景。

## USS 変数との対応

| JSON | USS 変数 |
|---|---|
| `surface.level0`〜`level3` | `--dennoko-surface-0`〜`--dennoko-surface-3` |
| `outline` / `outlineHover` | `--dennoko-outline` / `--dennoko-outline-hover` |
| `typography.*` | `--dennoko-text-primary` / `-secondary` / `-tertiary` / `-disabled` |
| `semantic.*` | `--dennoko-semantic-error` / `-error-text` / `-warning` / `-success` / `-info` |
| `interaction.*` | `--dennoko-accent` / `--dennoko-checkmark` / `--dennoko-hover-overlay` |
| `selection.*` | `--dennoko-selection-border` / `-bg` / `-bg-hover` |
| `component.*` | `--dennoko-list-alt-bg` / `--dennoko-progress-fill` |
| `status.*` | `--dennoko-status-success-bg` / `-error-bg` / `-error-text` / `-warning-bg` / `-info-bg` |
