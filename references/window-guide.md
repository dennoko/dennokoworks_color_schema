# EditorWindow 実装ガイド

コピー元コード: [assets/EditorWindow/YourEditorWindow.uxml](../assets/EditorWindow/YourEditorWindow.uxml) / [assets/EditorWindow/YourEditorWindow.cs](../assets/EditorWindow/YourEditorWindow.cs)
テーマ: [assets/DennokoTheme.uss](../assets/DennokoTheme.uss)

## ウィンドウ全体のレイアウト構成

画面の中身（どのカードをどの順に置くか）は画面パターンで決める（`blocks/patterns.md`）。
どのパターンでも、**上下は標準ヘッダー・標準フッターに固定する**。

```
┌──────────────────────────────────────┐
│ Tool Name  v1.2.0 ↻         EN ? ⋮  │  ← 標準ヘッダー: .dennoko-appbar (flex-shrink: 0)
├──────────────────────────────────────┤
│ ┌──────────────────────────────────┐ │
│ │ SECTION TITLE                    │ │  ← .dennoko-card
│ │ ────────────────────────────     │ │     + .dennoko-card-header
│ │  [コンテンツ]                    │ │
│ └──────────────────────────────────┘ │  ← ↑ ScrollView (.dennoko-scroll) の中
│ ┌──────────────────────────────────┐ │     縮小時はここだけが縮む
│ │ [☑] TOGGLE SECTION     [Reset]   │ │  ← .dennoko-toggle-header
│ │  [スライダーなど]                 │ │  ← .dennoko-toggle-section__content
│ └──────────────────────────────────┘ │     (トグル OFF で折りたたまれる)
├──────────────────────────────────────┤  ← 出力ドックの上罫線
│ 出力先 [Assets/...        ] [選択…] │  ← 出力ドック: .dennoko-output (任意・2 行まで)
│ [Reset All] [  Apply & Save (白)  ]  │  ← フッター: .dennoko-footer--joined (カードで囲まない)
│ [ステータスメッセージ]                │  ← ステータス: .dennoko-status (枠線なし)
└──────────────────────────────────────┘
```

> 枠線は最小限にする。フッターを `.dennoko-card` で囲んだり、フッターの上に
> `.dennoko-separator` を足したりすると、区切り線・カード枠・ボタン枠・ステータスバー枠が
> 何重にも重なって下部が重く見える。出力ドックの直後のフッターは `--joined` で罫線を重ねない。

## 配置手順

```
Editor/
├─ UI/
│   ├─ DennokoTheme.uss        ← assets/DennokoTheme.uss をコピー
│   └─ YourEditorWindow.uxml   ← assets/EditorWindow/ からコピー
├─ DennokoUIFont.cs            ← assets/Shared/ からコピー（フォント管理・共通）
├─ DennokoAppBar.cs            ← assets/Shared/ からコピー（標準ヘッダー）
├─ DennokoStatus.cs            ← assets/Shared/ からコピー（ステータスバー）
├─ DennokoIcons.cs             ← assets/Shared/ からコピー（アイコンボタン）
└─ YourEditorWindow.cs         ← assets/EditorWindow/ からコピー
```

1. 上記 7 ファイルをコピーして配置する（Shared の 4 つはプロジェクトに 1 組だけ置き、Window / Inspector で共有する）
2. C# の変更箇所: `namespace` / クラス名 / `[MenuItem("Tools/Your Tool Name")]` / `ToolName` / `ManualUrl` / `ReleaseUrl`
   - Shared の 4 ファイルも `namespace` を合わせる。Inspector も作る場合、このファイルは
     **プロジェクト内に 1 つだけ**置いて共有する（重複配置はコンパイルエラー）
   - `DennokoUIFont.WarmupJapanese` にツールの UI で使う日本語を書き足す
     （実行中のアトラス追加＝文字化けの原因を減らす。`references/troubleshooting.md` §10）
     - 「代表的な語をいくつか」では足りない。**UXML のテキストとローカライズ用 JSON に
       出現する日本語を全文字入れる**（数百文字でも問題ない）。UI 文言を追加したら更新する
3. Unity インポート後、UXML / USS の `.meta` から GUID を控えて
   C# の `UXML_GUID` / `USS_GUID` 定数に設定する（プレースホルダーのままにしない）
   - GUID の調べ方: `.meta` の `guid:` 行、または Project ビュー右クリック → Copy GUID

## 標準ヘッダー（トップバー）

```
┌ Tool Name  v1.2.0 ↻                    EN  ?  ⋮ ┐
   appbar-title / version-label / version-reload-button   appbar-lang / appbar-manual / appbar-settings
```

```xml
<ui:VisualElement name="appbar" class="dennoko-appbar">
    <ui:VisualElement class="dennoko-appbar__title">
        <ui:Label name="appbar-title" text="YOUR TOOL NAME" class="dennoko-title" />
        <ui:Label name="version-label" text="" class="dennoko-version-label" />
        <ui:Button name="version-reload-button" class="dennoko-icon-button" />
    </ui:VisualElement>
    <ui:VisualElement class="dennoko-appbar__actions">
        <ui:Button name="appbar-lang" text="EN" class="dennoko-appbar-lang" />
        <ui:Button name="appbar-manual" class="dennoko-icon-button" />
        <ui:Button name="appbar-settings" class="dennoko-icon-button" />
    </ui:VisualElement>
</ui:VisualElement>
```

| 要素 | 決まり |
| --- | --- |
| ツール名 | `.dennoko-title`（13px 太字）。ウィンドウが狭いときはツール名だけが省略表示になり、右側のアイコンは縮まない |
| バージョン + ↻ | 文言と状態クラスは `references/version-check-guide.md` の `ApplyVersionLabel` が入れる。更新ありのときだけラベルをクリックで配布ページを開く（`DennokoAppBar.ReleaseUrl`） |
| 言語 | **切り替え先**の言語を出す文字ボタン 1 つ（日本語表示中は `EN`）。切り替えないツールは消す |
| マニュアル `?` | `DennokoAppBar.ManualUrl` を開く |
| 設定 `⋮` | メニューは「ツール固有の項目（`AddToolMenuItems`）→ 区切り → 設定を初期値に戻す…（確認あり）/ 更新を確認」。三点リーダーからの配布ページ導線は含めず、GitHub リポジトリへの参照はツールの公開ページ（Booth 等）側のみに記載し、Unity ツール内からは直接 GitHub リンクへの導線を作らない方針とする |

- 下罫線（`--dennoko-outline-subtle`）でスクロール領域と仕切るので、`.dennoko-separator` は足さない。
- アイコンは `DennokoAppBar` が `DennokoIcons` で付ける（Unity 組み込みの `d_Refresh` / `d__Help` / `d__Menu`。見つからなければ ↻ / ? / ⋮ の文字）。名前はツールチップに出す。
- 要らない要素は UXML から消すだけでよい（`DennokoAppBar` は見つからない要素を無視する）。
- タブ（`.dennoko-tabbar`）や作業中バナーは、トップバーの直下・スクロール領域の外に置く（`blocks/patterns.md`）。
- Inspector では `.dennoko-appbar--compact`（ツール名を省いた細い版）を使う（`blocks/patterns.md` P5）。
- 旧ヘッダー（`.dennoko-header` + `.dennoko-header-titlegroup` + `.dennoko-separator`）はテーマに残っているが、新しく作るときは使わない。

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
`label=` は「左ラベル列（100px〜30%）+ 右にチェックボックス」の 2 カラムになり、
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

## 標準フッター

スクロール領域の下に、**出力ドック（任意）→ フッター → ステータス**の順で固定する。

```xml
<!-- 出力ドック: 出力先など 2 行まで。出力しないツールは丸ごと消し、フッターの --joined も外す -->
<ui:VisualElement name="output-dock" class="dennoko-output">
    <ui:VisualElement class="dennoko-output-row">
        <ui:Label text="出力先" class="dennoko-output__label" />
        <ui:TextField name="output-path" value="Assets" readonly="true" class="dennoko-field-readonly" />
        <ui:Button name="output-browse" text="選択…" class="dennoko-output__action" />
    </ui:VisualElement>
    <ui:VisualElement class="dennoko-output-row">
        <ui:Label text="形式" class="dennoko-output__label" />
        <ui:VisualElement name="output-format" class="dennoko-segmented dennoko-choice-group">
            <ui:Button text="PNG" />
            <ui:Button text="TGA" />
        </ui:VisualElement>
        <ui:VisualElement class="dennoko-spacer" />
        <ui:Toggle name="output-overwrite" text="上書き" />
    </ui:VisualElement>
</ui:VisualElement>

<ui:VisualElement class="dennoko-footer dennoko-footer--joined">
    <ui:Label name="footer-info" text="12 件を変更" class="dennoko-footer__info" />   <!-- 任意 -->
    <ui:Button name="reset-all-button" text="Reset All" class="dennoko-button-secondary" />
    <ui:Button name="apply-button" text="Apply &amp; Save" class="dennoko-button-primary" />
</ui:VisualElement>

<ui:Label name="status-label" text="Ready" class="dennoko-status" />
```

**出力ドック** — 出力先・形式・上書きなど、出力の直前に確かめたい設定を 2 行まで常に見せる。
3 行以上になるならスクロール領域の最後に出力カードを置き、ドックには要約チップだけを出す（`blocks/cards.md` B5）。

- 行は `.dennoko-output__label`（44px）+ 伸びる欄 + `.dennoko-output__action`（56px）。
- 形式などの選択肢は `.dennoko-segmented.dennoko-choice-group` をそのまま置く。出力行の中では文字の幅で並ぶ
  （テーマが `flex-basis: auto` に直している。等幅の `flex-basis: 0` のままだと伸びる親が無いので幅 0 に潰れる）。

**フッター** — カードで囲まず、上罫線だけでスクロール領域と仕切る（出力ドックの直後は `--joined` で罫線を重ねない）。
ボタンは 1 行に並べ、**左に固定幅の Secondary（Reset All 等）、右に伸びる Primary（Apply 等。最小 96px）**を置く。
主アクションと補助・破壊的アクションを同じ幅で上下に並べると、優先順位が伝わらず誤クリックも誘発する。

- 件数など状況を示す短い文字は左端の `.dennoko-footer__info`（最大 40%、省略表示）に置く。
- Primary は画面に 1 つ。状態によって文言と処理を差し替える（「作業開始」→「出力」、「次へ」→「出力」）。

**ステータス** — 最下段の 1 行。`DennokoStatus` で文言と状態を切り替える。

```csharp
_status = new DennokoStatus(root.Q<Label>("status-label"));
_status.Set("Saved.", DennokoStatusType.Success);   // .dennoko-status--success。3 秒後に Ready へ戻る
_status.Set("出力先がありません", DennokoStatusType.Warning);
_status.Set("Failed.", DennokoStatusType.Error);
_status.Set("ベイク中… 40%", DennokoStatusType.Busy, 0.4f);   // 下端に進捗線。自動では戻らない
_status.Ready();
```

- 通知は実行した場所に 1 か所だけ出す。ウィンドウから実行したらステータス、シーンから実行したらトースト（`blocks/sceneview.md` S4）。
- ⚠ ステータスの Label に子要素（進捗バーなど）を足さない。Label は子要素があると文字の高さを測らなくなり、
  上下の余白（8px）だけの高さに潰れる。進捗線は `DennokoStatus` が `generateVisualContent` で Label 自身に描く。

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
