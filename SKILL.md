---
name: dennokoworks-design
description: dennokoworks フローティングデザインシステムを Unity Editor 拡張（EditorWindow / CustomEditor / Inspector）に UI Toolkit (UXML/USS) で適用する。エディタテーマ (Light/Dark) に依存しない UI の新規実装・改修・IMGUI からの移行・GitHub バージョン表示更新チェックに使う。
---

# dennokoworks Design Skill

**dennokoworks カラースキーマ（フローティングデザイン）** を Unity Editor 拡張の
UI Toolkit (UXML/USS) コードとして実装・改修するスキル。

- **ターゲット環境**: Unity 2022.3 ～ Unity 6
- **最優先要件**: エディタテーマ (Personal Light / Dark) に左右されず、
  常に洗練されたフローティングデザイン（ダークテーマ固定）を維持すること。

---

## 標準ワークフロー（基本の 6 ステップ）

```
[1. テンプレート配置] ──> [2. GUID & 識別子設定] ──> [3. フォント適用]
         │
         ▼
[4. UXML 構造定義]    ──> [5. C# バインディング]  ──> [6. Light/Dark 検証]
```

1. **実ファイルのコピー**:
   - `assets/DennokoTheme.uss` と `assets/Shared/DennokoUIFont.cs` をプロジェクトの `Editor/` 配下にコピー。
   - 目的（EditorWindow / Inspector / VersionCheck）に応じたテンプレート実ファイルをコピー。
2. **GUID と識別子の置換**:
   - Unity インポート後、UXML/USS の `.meta` から GUID を取得し、C# の `UXML_GUID` / `USS_GUID` に設定。
   - `namespace`、クラス名、`[MenuItem]` または `[CustomEditor]` の対象型を変更。
3. **フォント適用**:
   - `CreateGUI()` または `CreateInspectorGUI()` で `DennokoUIFont.Apply(root)` を 1 行呼ぶ。
4. **UXML での構造定義**:
   - レイアウトは UXML で完結させる（C# で `new VisualElement()` 手組みは動的リスト以外原則禁止）。
   - 固定要素（ヘッダー/フッター/ステータス）と可変要素（`ScrollView.dennoko-scroll`）を分離。
5. **C# でのロジック接続**:
   - `root.Q<T>("name")` で取得し、イベントやコールバックを登録。
   - トグルセクションは `BindToggleSection()` ヘルパーを活用（OFF で折りたたむ。グレーアウトにしない）。
   - Inspector の場合は `container.Bind(serializedObject)` を必ず呼ぶ。
6. **テーマ切替・動作検証**:
   - Unity の Preferences でテーマを **Light / Dark 両方に切り替えて** 表示を確認（黒文字化・アイコン消失の防止）。
   - ウィンドウを縮小して固定クロームが潰れず `ScrollView` だけが縮むか確認。

---

## タスク別ルーティング

やりたいことに応じて、必要な実ファイルをコピーし、対応ガイドを参照する。

| やりたいこと | コピー元アセット | 参照ガイド | 注意点 / カスタマイズ箇所 |
|---|---|---|---|
| **EditorWindow を作る** | `assets/DennokoTheme.uss`<br>`assets/EditorWindow/` (UXML + C#)<br>`assets/Shared/DennokoUIFont.cs` | `references/window-guide.md` | GUID 2 箇所・`namespace`・クラス名・メニューパスを書き換える。<br>`DennokoUIFont.Apply(root)` を呼ぶ |
| **Inspector (CustomEditor) を作る** | `assets/DennokoTheme.uss`<br>`assets/Inspector/` (UXML + C#)<br>`assets/Shared/DennokoUIFont.cs` | `references/inspector-guide.md` | `[CustomEditor(typeof(T))]`、`PropertyField` の `binding-path`、`container.Bind()`。<br>`DennokoUIFont.Apply(container)` を呼ぶ |
| **バージョン表記 + 更新チェックを付ける** | `assets/VersionCheck/`<br>（Checker + Version の C# × 2） | `references/version-check-guide.md` | リモートリポジトリに `version.json` を配置。owner / repo / branch 定数を設定 |
| **IMGUI 実装を移行する** | 上記 Window または Inspector 一式 | `references/imgui-migration.md` | `UniTexTheme` / `PushEditorTheme` / テクスチャ生成は全廃し UXML+USS に置き換え |
| **独自の USS クラスを追加・改変する** | — | `references/uss-conventions.md` | **必読**。必ず `.dennoko-root` を前置し詳細度 (0,2,0) で勝たせる（`!important` 不可） |
| **表示崩れ・テーマ切替問題を解決する** | — | `references/troubleshooting.md` | 症状別インデックスと動作確認チェックリストを確認 |
| **デザイン思想・カラー仕様の確認** | — | `Docs/design_reference.md`<br>`Docs/colors_spec.md`<br>`colors.json` | 明度差による Elevation、タイポグラフィ階層、コントラスト比の意図を確認 |

テンプレートは**実ファイルとして `assets/` 配下にある**。内容を転写せずファイルごとコピーし、
ガイド記載のカスタマイズポイント（namespace・クラス名・GUID 等）だけ書き換える。

---

## 絶対規則（必ず守ること）

1. **ルート要素に `dennoko-root` クラスを必ず付与する。**
   - USS 変数（`--dennoko-*`）の定義元かつテーマ非依存スタイルの起点。忘れると全スタイルが無効化される。
   - EditorWindow: `rootVisualElement.AddToClassList("dennoko-root")`
   - Inspector: return する container に `container.AddToClassList("dennoko-root")`
2. **カラーは USS 変数 `var(--dennoko-*)` を経由する。**
   - カラーコードの直書きは `DennokoTheme.uss` の変数定義部のみ。
   - UXML のインライン style や C# の `style.color` にカラーコードを直書きしない。
3. **USS に `!important` は存在しない（書くと宣言が無効化される）。**
   - 優先度はすべてセレクタの詳細度と定義順で制御する。独自クラスは必ず `.dennoko-root .your-class` で定義する。
4. **役割分担の厳守: UXML = 構造 / USS = スタイル / C# = ロジック。**
   - C# で `new VisualElement()` を手組みしてスタイリングしない（動的リストアイテム生成のみ例外）。
5. **UXML / USS は GUID でロードする。**
   - テンプレートの `YOUR_*_GUID_HERE` は Unity インポート後に `.meta` の GUID へ必ず置き換える（パス指定はアセット移動で壊れるため禁止）。
6. **標準フォントは `assets/Shared/DennokoUIFont.cs` を配置して `DennokoUIFont.Apply(root)` だけ呼ぶ。**
   - OS のメイリオを SDF FontAsset として動的参照する（フォントアセットは同梱しない）。
   - ルート要素 1 つにつき 1 回、`CreateGUI()` / `CreateInspectorGUI()` で呼ぶだけでよい。未搭載環境ではエディタ標準フォントに自動フォールバックする。
   - **EditorWindow / Inspector 側で FontAsset を直接生成・キャッシュしない。**
   - Window と Inspector を両方作る場合も `DennokoUIFont.cs` は 1 つだけ配置して共有する。
   - （背景: レガシー Font のグリフ消失、`Resources.UnloadUnusedAssets` によるアトラス破棄と `MissingReferenceException`、追加アトラスやドメインリロードのリークなどの事故をこのクラスが一手に防いでいる。詳細は `references/troubleshooting.md` §8〜§10）。
7. **固定クロームには `flex-shrink: 0`、スクロール領域には `.dennoko-scroll` を指定する。**
   - ヘッダー、フッター、ステータスバーがウィンドウ縮小時に押し潰されるのを防ぐ。

---

## カラーパレット（クイックリファレンス）

すべて `DennokoTheme.uss` の `.dennoko-root` に定義済み（マスターデータ: `colors.json`）。

| 役割 | HEX | USS 変数 | 専用ユーティリティクラス | 用途 |
|---|---|---|---|---|
| アプリ背景・文字入力欄 | `#121212` | `--dennoko-surface-0` | — | 最背面の背景。TextField / 数値欄 / ObjectField も「沈めて」この色にする |
| カード背景 | `#1e1e1e` | `--dennoko-surface-1` | — | 浮かび上がるカード、ステータスバー |
| ボタン・選択欄・ツールバー | `#2c2c2c` | `--dennoko-surface-2` | — | ボタン、Dropdown / Enum 等の選択欄、ツールバー、HelpBox（「浮かせる」） |
| 浮かせた要素のホバー | `#383838` | `--dennoko-surface-3` | — | Surface2 の要素のホバー |
| 境界線・セパレーター | `#484848` | `--dennoko-outline` | — | カード枠線、セパレーター |
| 入力欄ホバー時の境界線 | `#5e5e5e` | `--dennoko-outline-hover` | — | 沈めた入力欄のホバー（背景ではなく枠線で示す） |
| タイトル・強調 | `#ffffff` | `--dennoko-text-primary` | `.dennoko-text-primary` | 最重要テキスト、見出し、アクティブ文字 |
| 本文・標準ラベル | `#cccccc` | `--dennoko-text-secondary` | （デフォルト） | 通常ラベル・説明文（クラス指定不要） |
| 補足・見出し | `#aaaaaa` | `--dennoko-text-tertiary` | `.dennoko-text-tertiary` | メタデータ、小さな注記、サブ情報、読み取り専用の値 |
| 無効状態 | `#555555` | `--dennoko-text-disabled` | `.dennoko-text-disabled` | 非活性テキスト（全コンポーネント共通） |
| エラー（塗り・枠） | `#9b1b30` | `--dennoko-semantic-error` | — | エラーの背景・枠線。**文字には使わない**（暗背景で読めない） |
| エラー（文字） | `#ff6b6b` | `--dennoko-semantic-error-text` | `.dennoko-text-error` | 危険・異常・取得失敗の文字 |
| 警告 | `#ffb74d` | `--dennoko-semantic-warning` | `.dennoko-text-warning` | 注意・要確認 |
| 成功 | `#4caf50` | `--dennoko-semantic-success` | `.dennoko-text-success` | 完了・最新版・正常 |
| 情報 | `#64b5f6` | `--dennoko-semantic-info` | `.dennoko-text-info` | ヒント・通知 |
| アクセント | `#ffffff` | `--dennoko-accent` | — | フォーカス枠・Primary ボタンの塗り・チェック ON の塗り |
| チェックマーク | `#121212` | `--dennoko-checkmark` | — | チェックマーク / ラジオの点（白 accent 塗りの上） |
| 選択中の枠線 | `#64b5f6` | `--dennoko-selection-border` | — | 排他ボタン・タブ・ON/OFF ボタンの選択中、MinMaxSlider の範囲バー |
| 選択中の背景 | 青 16% | `--dennoko-selection-bg` (`-bg-hover`) | — | 選択中ボタンの背景ティント、リストの選択行 |

ほかに派生色として `--dennoko-hover-overlay`（リスト行ホバー）、`--dennoko-list-alt-bg`（リスト交互行）、
`--dennoko-progress-fill`（ProgressBar）、`--dennoko-status-*-bg` / `--dennoko-status-error-text`
（ステータスバー・HelpBox）がある。詳細は `Docs/colors_spec.md`。

---

## デザインコンセプト・重要ルール（要点）

- **フローティング表現**: 全体背景は最も暗い Surface0。コンテンツは Surface1 のカードとして「浮いて」見せる。
- **Elevation は明度差で表現**: USS に `box-shadow` は存在しない。Surface 間の明度差（0 → 1 → 2 → 3）で浮遊感と階層を作る。
- **入力は沈め、操作は浮かせる**（テーマ USS が自動適用）:
  - 文字入力欄（TextField / 数値欄 / ObjectField / Color 等）は Surface0 に「沈める」。ホバーは枠線の明度で示す。
  - ボタンと選択欄（Dropdown / Enum / Mask / Layer / Tag）は Surface2 に「浮かせる」。ホバーは Surface3。
  - 同じトーンにすると、クリック対象か入力対象かが見分けられなくなる。
- **コントラスト管理**: 表面間のコントラストは低め、テキストは高コントラスト（純白系）にして視認性を確保。エラー文字は `#9b1b30` ではなく `--dennoko-semantic-error-text` を使う。
- **境界線は最小限**: `--dennoko-outline` (`#484848`) で輪郭をさりげなく示すが、同じ方向の線を何重にも重ねない（フッターはカードで囲まず上罫線のみ、複合フィールドは末端の数値欄だけに枠）。
- **Primary は白で塗る**: `.dennoko-button-primary` は accent（白）塗り + 暗色文字。画面で最優先のアクションに 1 つだけ使う。
- **選択中・アクティブ状態は青（selection）で示す**:
  - モード切替やサブツール（例: Select/Paint、Brush/Rect/Lasso/Eraser、Add/Remove）、タブのように複数の中から 1 つを選ぶボタン群には、選択中のボタンに必ず `.dennoko-button-active` を付ける（`EnableInClassList("dennoko-button-active", isSelected)`）。
  - 青枠（`--dennoko-selection-border`）+ 薄い青の背景（`--dennoko-selection-bg`）になる。背景だけ・枠線だけで状態を表現しない。
  - 青は accent（白 = フォーカス・最優先アクション）とは別のトークン。混同しない。
- **チェックボックス / ラジオ**（テーマ USS が自動適用）:
  - OFF = 暗色の箱（丸）+ 枠線のみ、ON = 白塗り + 暗色のマーク。OFF を白塗りにすると ON と見分けがつかない。
  - チェックボックスとそのラベルは「入力欄」ではなくテキスト行として見せる。トグル自体にも、トグルで制御する項目のまとまりにも枠や背景の囲みを付けない。階層はインデント（`margin-left`）で示す（`references/troubleshooting.md` §4-②）。
  - **Toggle は `text=`（左にチェックボックス、右に文字）で書くのが原則**。`label=` は他の入力欄と同じ 2 カラム（左ラベル列 150px）に並べたい場合だけ使う。
- **トグル付きセクションは OFF で折りたたむ**: グレーアウト（`SetEnabled(false)`）ではなく、見出し行だけを残して中身を隠す（`BindToggleSection()` が `.dennoko-toggle-section--collapsed` を付け外しする）。
- **無効状態は色で統一する**: テーマ USS 末尾の `:disabled` ルールが文字・枠・塗りを一律に落とす。`opacity` は入れ子で掛け算になるため使わない。

---

## 主要 UI 部品クラス一覧（`DennokoTheme.uss` 定義済み）

### 構造・レイアウト
- `.dennoko-root`: ルート要素必須クラス（変数定義 & Surface0 背景）。
- `.dennoko-card`: セクションカード（Surface1、枠線、角丸 8px、内側パディング 12px）。
- `.dennoko-card-header`: カード内見出しの下線付きヘッダー行。カードの最上部に置く（ツールバー等は見出しの下）。
- `.dennoko-toggle-header`: トグルと Reset ボタンを両端配置する横並びヘッダー。
- `.dennoko-toggle-section__content`: トグル付きセクションの中身（OFF で非表示になる部分）。
- `.dennoko-toggle-section--collapsed`: 折りたたみ状態（`BindToggleSection()` がカードに付け外しする。手書き不要）。
- `.dennoko-header`: ウィンドウ上部ヘッダー行（`flex-shrink: 0`）。
- `.dennoko-header-titlegroup`: タイトルとバージョンラベルを横並びにするグループ。
- `.dennoko-separator`: 1px の水平区切り線（Outline 色、`flex-shrink: 0`）。
- `.dennoko-scroll`: スクロール領域（ScrollView に付与。縮小時はここだけが縮む）。
- `.dennoko-footer`: ウィンドウ下部アクション領域。**カードとは併用しない**（上罫線のみで仕切る）。1 行に左 Secondary（固定幅）・右 Primary（伸びる）を並べる。`flex-shrink: 0`。
- `.dennoko-toolbar`: ツールバー行（Surface2 背景）。中のボタンは枠なしのゴースト表示になる。
- `.dennoko-hrow`: 折り返さない横並び行（伸びる入力欄 + 固定幅ボタン等）。
- `.dennoko-col-grow`: 横並びの中で伸びて縮む子要素（`flex-grow` + `flex-shrink` + `overflow: hidden`）。
- `.dennoko-inspector-root`: Inspector 専用の左右余白打ち消しクラス（ネガティブマージン）。

### ボタン・入力
- `.dennoko-button-primary`: 主アクションボタン（Apply & Save 等。白塗り + 暗色太字、34px）。画面に 1 つ。
- `.dennoko-button-secondary`: 副アクションボタン（Reset 等。高さは通常ボタンと同じ 26px、文字を小さく控えめに）。
- `.dennoko-button-active`: トグル型ボタン・排他選択・タブの選択中表示（青枠 + 青ティント背景）。
- `.dennoko-field-readonly`: 読み取り専用の入力欄（`readonly="true"` と併せて付ける。背景透明・文字 Tertiary）。Unity 2022.3 には読み取り専用を示すビルトインクラスがないため明示する。
- `.dennoko-field-multiline`: 複数行入力欄（`multiline="true"` と併せて付ける。ラベルを 1 行目に揃える）。
- `.dennoko-helpbox--info` / `--warning` / `--error`: HelpBox の種類別の色分け（`message-type` と併せて付ける。ルート要素に種類を示すビルトインクラスがないため明示する）。
- ※ 次のビルトイン要素はテーマ USS が自動装飾するためクラス付与不要:
  `TextField` / 数値欄 / `Vector*Field` / `RectField` / `BoundsField` / `DropdownField` / `EnumField` / `MaskField` 系 /
  `ObjectField` / `ColorField` / `CurveField` / `GradientField` / `Slider` / `SliderInt` / `MinMaxSlider` / `ProgressBar` /
  `Toggle` / `RadioButton` / `RadioButtonGroup` / `Foldout` / `ListView` / `HelpBox` / スクロールバー。

### ステータス・バージョン
- `.dennoko-status`: ステータスバー（Ready 表示等。枠線なし、Surface1 背景）。
  - `.dennoko-status--success`: 成功時ハイライト（半透明グリーン背景）。
  - `.dennoko-status--error`: エラー時ハイライト（半透明レッド背景 + 読みやすい文字色）。
- `.dennoko-version-label`: バージョン表示テキスト（10px、Tertiary 色）。
  - `.dennoko-version-label--update`: 更新あり表示（太字グリーン）。
  - `.dennoko-version-label--error`: 取得失敗表示（警告色）。
- `.dennoko-version-reload-button`: バージョン再確認ボタン（↻。20px 角、ホバーで枠と背景を表示）。

---

## 完了前チェックリスト（必ず実施）

1. **Light / Dark テーマ両対応**: Unity の Preferences でテーマを切り替え、白文字化け・黒文字化け・アイコン消滅がないか。
2. **フォント適用**: `DennokoUIFont.Apply(root)` が呼ばれているか（メイリオ SDF の自動適用）。
3. **GUID 置換完了**: `YOUR_*_GUID_HERE` が `.meta` の実 GUID にすべて置き換わっているか。
4. **ルートクラス付与**: ルートに `dennoko-root` が付与されているか（Inspector は `.dennoko-inspector-root` も併用）。
5. **バインディング確認**: Inspector の場合は `container.Bind(serializedObject)` が呼ばれているか。
6. **ウィンドウ伸縮テスト**: 極端に小さくしたときに固定クロームが潰れず、スクロール領域だけがスクロールするか。
7. **状態表現**: 選択中のボタン・タブに `.dennoko-button-active` が付いているか。Primary ボタンが画面に 1 つだけか。
8. **トグル付きセクション**: 中身に `.dennoko-toggle-section__content` を付け、`BindToggleSection()` で OFF 時に折りたたまれるか。
9. **独自クラスの付け忘れ**: `readonly` の入力欄に `.dennoko-field-readonly`、`multiline` に `.dennoko-field-multiline`、HelpBox に `.dennoko-helpbox--*` を付けたか。
