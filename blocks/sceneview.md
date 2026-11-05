# シーン操作 UI（L3）

ツール操作中に SceneView 上に出す UI。DennokoMeshEditor の操作パネルや MaskMakerV4 のツールバー・ラジアルメニューを、
全ツール共通の部品として定義する。見た目は `assets/SceneView/DennokoSceneUI.uss`（ウィンドウと同じテーマの色を使う）。

- ルートには `DennokoSceneUI.SetupRoot(root)` で `dennoko-root dennoko-scene` を付け、テーマ → ブロック → シーンの順に USS を足す。シーンを透かすので背景色は付けない。
- ツールごとの独自色（緑枠・緑の太字ヒント・ピンクのミラー円など）は使わない。

## 1. 3 つの置き場所の分担

| 置き場所 | 担当すること | 置かないもの |
| --- | --- | --- |
| ウィンドウ / Inspector | 開始・終了、対象、作業の前後に決める設定、出力 | 作業中に何度も変えるもの |
| シーンの直接操作 | クリック・ドラッグ・ホイール（作業の本体） | 設定値の変更 |
| シーン操作 UI（本書） | 作業中に切り替えるモード・ツール・トグル・数値、今の状態の表示、終了 | 出力先など作業の前後に決める設定 |

- **状態は 1 か所（ツールの状態クラスまたはコンポーネント）にだけ持つ。** ウィンドウ・シーン操作 UI・ショートカットはどれもそこを書き換え、`Changed` を受けて表示を読み直す。

## 2. コンポーネント一覧

| # | コンポーネント | 実装 | 役割 | アセット |
| --- | --- | --- | --- | --- |
| S1 | シーンツールバー（カプセル） | `Overlay` + `ITransientOverlay` | モード・ツール・トグルをアイコンで切り替える。状態の表示 | `YourSceneToolbar.cs` / `.uxml` |
| S2 | シーンツールパネル | `Overlay` + `ITransientOverlay`（パネル表示） | 数値（半径・強さ）を作業しながら変える。作業中の表示と終了 | `YourScenePanel.cs` / `.uxml` |
| S3 | ラジアルメニュー | SceneView に重ねた HUD + `ClutchShortcut` | キーボード派の切替。カーソル直下に出る | `DennokoRadialMenu.cs` + `YourRadialMenu.cs`（[radial-menu.md](radial-menu.md)） |
| S4 | シーンフィードバック | 同じ HUD | トースト、カーソルヒント、修飾キーのバッジ、キー一覧 | `DennokoSceneFeedback.cs` |
| S5 | シーン描画の色と線 | C# 定数 | ホバー・選択・除外の予告・ミラー・ブラシ・影響範囲 | `DennokoSceneColors.cs`（描き方の例は `YourSceneInput.cs`） |
| S6 | 情報オーバーレイ | `Overlay`（常設、ユーザーが ON/OFF） | 読み取り専用の数値表示 | `YourInfoOverlay.cs` / `.uxml` |
| — | セッション規約 | §4 | 編集モードの開始・終了・自動終了・ショートカット | `DennokoSceneSession.cs` + `YourSceneToolState.cs` |

ツールの性質ごとの組み合わせ：

| ツールの性質 | 使うもの | 該当ツール |
| --- | --- | --- |
| シーンに描くだけ | S5 | BoxWeightTransfer / SocksTuku-ru / DenEmo の頂点ガイド |
| クリックで選ぶだけ（設定はウィンドウ） | S5 + セッション規約 + S4 のヒント | MeshSplitter / HairChimera / CableGenerator |
| 数値をいじりながら編集する | S2 + S5 + セッション規約 | DennokoMeshEditor / DennokoNormalEditor / DenLattice |
| モード・ツールが多い | S1 + S3 + S4 + S5 + セッション規約（数値が多ければ S2 も） | MaskMakerV4 |
| 常に見せたい情報 | S6 | HowManyPolygons |

## S1 シーンツールバー（カプセル）

```
╭──────────────────────────────────────────────────────────────╮
│ [選][塗] │ [島][パーツ][面] │ [⇋] │ [👁][◎] │ [↶][↷][🗑] │ [💾]  12/480 │
╰──────────────────────────────────────────────────────────────╯
```

- `.dennoko-capsule`：高さ 32px・角丸 16px、Surface1 + Outline。まとまりは `.dennoko-segment`（右側の縦線）で区切る。
- ボタンは**アイコンのみ**（`.dennoko-icon-button`、シーンでは 26px 角）。名前とキーはツールチップ（`"ブラシ  B"`）。文字の選択肢は `.dennoko-choice`（1 ボタン 3 字まで）。
- 現在値・ON は selection 色の枠（`.dennoko-button-active`）。実行系の成功は 2 秒だけ緑枠（`.dennoko-success-frame`）。
- 常に出す数値は右端の 1 つだけ（`.dennoko-capsule__count`：選択数など）。
- モードで中身を差し替えるときは同じ位置を差し替え、ボタンの数を変えない。
- ボタンは 12 個まで。それ以上はラジアル（S3）かパネル（S2）へ。
- 既定の位置は左の列の下端（`DockZone.LeftColumn` / `DockPosition.Bottom`）。

## S2 シーンツールパネル

DennokoMeshEditor / DennokoNormalEditor / DenLattice の IMGUI パネルを置き換える、数値を扱う作業用パネル。
Unity の Overlay（パネル表示）にすることで、ドラッグ・画面内への収め込み・位置の記憶を Unity に任せる。

```
╭ Mesh Editor ──────────────────── ⌄ ╮  ← Unity の Overlay 見出し（名前・ドラッグ・折りたたみ）
│ ● 編集中  Body                [✕] │  ← セッション行（対象名 + 終了）
│ ───────────────────────────────── │
│ 半径  ━━━━━━●━━━━━━━   [0.025]     │  ← コンパクトスライダー
│ 減衰  [滑らか][直線][鋭い][一定]    │  ← 選択肢グループ
│ ミラー [X][Y][Z]                    │  ← トグル（ON は selection 色の枠）
│ ───────────────────────────────── │
│ [Wheel] 半径   [Esc] 終了           │  ← キーヒント（Tertiary、2 組まで）
│ ⚠ NDMF プレビュー未取得             │  ← 警告行（必要なときだけ。全文はツールチップ）
╰───────────────────────────────────╯
```

| 要素 | クラス | 見た目と動作 |
| --- | --- | --- |
| セッション行 | `.dennoko-scene-panel__session`（`__state` / `__target`） | selection 色の ● + 「編集中」+ 対象名（省略表示）+ ✕（アイコンボタン）。✕ は Esc と同じ終了処理。対象名クリックでフレーム |
| コンパクトスライダー | `.dennoko-scene-row`（`__label` / `__value`） | ラベル 44px（Tertiary）+ スライダー（伸びる）+ 数値欄 48px。ホイールなどシーン側で変えた値も即座に反映する |
| 整数の行 | `.dennoko-scene-row__int` | 格子数など、整数欄を横に並べる |
| 選択肢グループ | `.dennoko-segmented.dennoko-choice-group` | 現在値に selection 色の枠。enum はドロップダウンにせずこれを使う（4 つまで） |
| トグル | `.dennoko-choice` / `.dennoko-icon-button` | ON で selection 色の枠。ON/OFF を文字で書かない |
| 1 回適用スライダー | `.dennoko-scene-row` + `__percent` | ドラッグした量だけ効果をかけ、離すと確定して 0 に戻る（NormalEditor の効果）。数値欄は出さず、ドラッグ中だけ % を出す |
| 小見出し | `.dennoko-scene-panel__section`（`-title`） | 10px・Tertiary。行のまとまりが 2 つ以上あるときだけ（「効果」「ボックス」） |
| キーヒント | `.dennoko-keyhints` + `.dennoko-keycap` | 常設は 2 組まで。残りはツールチップと S4 のキー一覧へ |
| 警告行 | `.dennoko-scene-panel__warning`（`-text`） | `--dennoko-status-warning-bg` + ⚠ + 1 行（30 字まで）。全文はツールチップ |

- パネルに置くのは「作業中に変える値」と「終了」だけ。出力・保存・作業の前後に決める設定は置かない。
- 幅は 240px 固定、高さは中身に合わせる。行は 8 行まで。
- 既定の位置は右の列の下端。S1 と併用するツールでは S1 が左下、S2 が右下。
- スライダーのドラッグは 1 回で Undo 1 段にまとめる。UI Toolkit の Slider はドラッグ中も `ChangeEvent` を出し続けるので、最初の変更で Undo グループを開き、`PointerUpEvent` / `PointerCaptureOutEvent`（TrickleDown）で `CollapseUndoOperations` する。
- 「編集中」を枠の色で示さない。セッション行と「作業中だけ表示される」こと（`ITransientOverlay`）で示す。

| ツール | 今 | 置き換え後 |
| --- | --- | --- |
| DennokoMeshEditor | 半径スライダー、減衰 EnumPopup、ミラー ON/OFF の大きいボタン、ヒント 2 行、NDMF 警告 | 半径（コンパクトスライダー）、減衰（選択肢グループ）、ミラー（トグル）、キーヒント 2 組、警告行 |
| DennokoNormalEditor | 上記 + 効果スライダー 4 本 | 上記 + 小見出し「効果」+ 1 回適用スライダー 4 本（選択が無いときは無効） |
| DenLattice | 格子数、ミラー軸 XYZ、ボックスモード ON/OFF、移動 / 回転 / 拡縮、ケージのリセット | 格子数（整数の行）、ミラー（XYZ トグル）、モード（「ケージ / ボックス」）、ボックス時だけ「移動 / 回転 / 拡縮」、リセットは小見出し右の ↺ |

## S3 ラジアルメニュー

テンプレート `DennokoRadialMenu.cs` をそのまま使う。使い方・動き・確認項目は [radial-menu.md](radial-menu.md)。全ツールで揃える点：

- キーは全ツール共通で **C 長押し**（`ClutchShortcut` + セッション中だけ有効にするタグ）。ユーザーは Shortcut Manager で変更できる。
- 項目は常に 8 個。空きは薄く表示して選べなくする。↑ は「モード切替」に固定する（モードが無いツールは ↑ を空ける）。
- Undo / Redo のように Ctrl+Z で済むものは入れない。切り替える項目が 4 個以下のツールでは使わない。

## S4 シーンフィードバック

| 種類 | クラス | 出る場所 | 内容 | 消え方 |
| --- | --- | --- | --- | --- |
| トースト | `.dennoko-toast`（`__text` / `__action`） | シーンから実行したときはカーソルのすぐ脇、それ以外は SceneView 下端の中央 | アイコン + 1 行（30 字まで）+ 操作ボタン 1 つまで（例：「Body_mask.png [適用]」） | 4 秒。ホバー中は消えない |
| カーソルヒント | `.dennoko-cursor-hint` | カーソルの右下 12px | 1 行。迷っていそうなときに次の操作を案内（「クリックで選択」「X で左右同時」） | 操作したら消える。各ヒント 3 回まで |
| 修飾キーのバッジ | `.dennoko-mod-badge` | カーソルの右下 | Shift / Ctrl を押している間、足す（+）/ 引く（−） | キーを離すと消える |
| キー一覧 | `.dennoko-keyhints` | ラジアルの周囲、ラジアルが無いツールは S2 の下 | 主要なキーの一覧 | 求めたとき（C を押したまま止める / ? キー）だけ |

- 通知は実行した場所に 1 か所だけ出す。ウィンドウから実行したらステータスバー、シーンから実行したらトースト。
- トーストは Surface1 + Outline、成功・警告・エラーはアイコンの色だけで区別する（背景を塗らない）。
- 説明文を常設しない。案内はツールチップ → 修飾キーのバッジ → 迷ったときのヒント → キー一覧の順に、必要な人にだけ出す。
- HUD は各 `SceneView.rootVisualElement` に透明な全面のルート（`pickingMode = Ignore`、`.dennoko-hud`）を足して載せる。カーソル座標は `rootVisualElement.layout.height - camera.pixelHeight / pixelsPerPoint` だけずらす（`DennokoSceneFeedback` がやる）。

## S5 シーン描画の色と線（Handles）

Handles は USS の変数を読めないので、`colors.json` の値を C# 定数にした `DennokoSceneColors` を使う。

| 用途 | 定数 | 色 | 由来 |
| --- | --- | --- | --- |
| ホバー（クリックで足す） | `HoverAdd` | 白 0.9 の縁 | accent |
| ホバー（クリックで外す） | `HoverRemove` | `#ff6b6b` の縁 + 点線 | semantic-error-text |
| 選択済み | `SelectedFill` / `SelectedLine` | `#64b5f6` 塗り 0.35 + 縁 0.9 | selection-border |
| ミラー側 | 本体と同じ色の点線（`dotted: true`） | — | — |
| ブラシ円 | `BrushInner` / `BrushOuter` | 白 0.8 の実線（半径）+ 白 0.3（減衰の外側） | accent |
| 影響度 | `InfluenceLow` → `InfluenceHigh` | selection 0.25（弱）→ warning 0.9（強） | selection → warning |
| 注意（対象外・壊れた面） | `Warning` | `#ffb74d` | semantic-warning |

- シーンの背景は明るいことも暗いこともあるので、線は **黒 0.5 の外線 + 色の内線** の 2 重にする（`DennokoSceneColors.Polyline` / `Quad` / `Circle`）。
- 太さは 2px（`Bold`：ホバー・選択の縁）と 1px（`Thin`：補助線）の 2 種類。`EditorGUIUtility.pixelsPerPoint` を掛けて、高 DPI でも細くならないようにする。
- 色だけで意味を伝えない。「外す」予告は赤に加えて縁を点線にする。

## S6 情報オーバーレイ

HowManyPolygons のように、作業とは関係なく常に見せたい読み取り専用の表示。

- `ITransientOverlay` を付けない普通の `Overlay` にし、表示 / 非表示はユーザーが Overlay メニューで決める。
- 中身（`.dennoko-info-overlay`）は大きい数値 1 つ（`__value`）+ 補足 1 行（`__sub`）。段階の色は `--good` / `--warn` / `--bad` で、semantic 色を文字色にだけ使う。
- クリックしても何も起きない（操作を置かない）。設定はウィンドウ側か Preferences に置く。

## 4. セッション規約（編集モードの約束）

シーンで何かを編集する「編集モード」を持つツールは、次の約束を守る。共通部分は `DennokoSceneSession` が守るので、ツールは `YourSceneToolState.Begin` / `End` と、保存（`OnEnd`）・Esc の扱い（`HandleEscape`）だけを書く。

| 項目 | 約束 |
| --- | --- |
| 開始 | ウィンドウ / Inspector の主ボタン、Hierarchy の右クリックから。シーンに開始ボタンを出さない |
| 終了 | Esc（選択があればまず選択を解除し、何も無ければ終了）、S2 の ✕、B7 の「終了」。どれも同じ処理 |
| 自動終了 | Play モードの開始、シーンの切替、再コンパイルの前（`beforeAssemblyReload`）、対象の削除、ウィンドウを閉じたとき。作業は保存してから終わる |
| 同時に 1 つ | dennokoworks の編集セッションは同時に 1 つだけ。別のツールで開始したら、前のセッションを終了してから始める |
| 作業中だけ有効 | S1 / S2 は `ITransientOverlay` で作業中だけ出す。ショートカットはタグ付きで登録し、作業中だけ `ShortcutManager.RegisterTag` する |
| Unity のハンドル | 作業中は移動 / 回転ツールのハンドルを隠し（`Tools.hidden`）、終了で元に戻す |
| カメラ操作 | Alt + ドラッグ・中ボタン・右ボタンのドラッグは消費しない |
| 選択の保護 | 作業中は Hierarchy の選択を変えても対象を変えない（切り替えは B1 / B7 の ▼ から） |
| Undo | 編集は Unity の Undo に積む。スライダーは 1 ドラッグ 1 段 |
| 表示 | 作業中はウィンドウ / Inspector に B7、シーンに S2 のセッション行（S2 が無いツールは S1 の右端に ● を出す） |

## 5. ファイルと組み込み手順

`assets/SceneView/` のファイルは 2 種類ある。**共通部品はそのままコピー**し（namespace と GUID だけ変える）、**雛形はツールに合わせて書き換える**。

| 種類 | ファイル | 役割 |
| --- | --- | --- |
| 共通 | `DennokoSceneUI.cs` | ルートにテーマを当てる、UXML の読み込み、1 ドラッグ = Undo 1 段のスライダー、1 回適用スライダー、パネルに付いている間だけ購読、成功の緑枠 |
| 共通 | `DennokoSceneSession.cs` | §4 セッション規約（開始・終了・自動終了・同時に 1 つ・タグ・ハンドル・Esc） |
| 共通 | `DennokoSceneFeedback.cs` | S4（トースト・カーソルヒント・迷い検知ヒント・修飾キーのバッジ） |
| 共通 | `DennokoRadialMenu.cs` | S3 本体 |
| 共通 | `DennokoSceneColors.cs` | S5 の色定数と 2 重線（`Polyline` / `Quad` / `Circle`） |
| 共通 | `DennokoSceneUI.uss` | S1〜S6 の見た目 |
| 雛形 | `YourSceneToolState.cs` / `YourSceneToolData.cs` | 状態の 1 か所（`Changed`）と Undo に載せる値。`Begin` / `End`、保存、Esc の扱い |
| 雛形 | `YourSceneInput.cs` | シーンの直接操作（クリック・ドラッグ・押したままホイール）と描画 |
| 雛形 | `YourSceneToolbar.cs` / `.uxml` | S1 |
| 雛形 | `YourScenePanel.cs` / `.uxml` | S2 |
| 雛形 | `YourRadialMenu.cs` | S3 の項目とショートカット（作業中だけ有効なタグ付き） |
| 雛形 | `YourInfoOverlay.cs` / `.uxml` | S6 |

共通部品は `DennokoTheme.uss` / `DennokoUIFont` / `DennokoIcons`（`assets/Shared/`）と、
`DennokoBlocks.uss` / `DennokoBlockUI`（`assets/Blocks/`：選択肢グループ・キーヒント）を使う。

組み込みの手順：

1. 共通部品と、使う雛形をコピーする（使わない S1 / S2 / S3 / S6 の雛形はコピーしない）。全ファイルの `namespace` をツールに合わせる。
2. Unity が取り込んだら、`.meta` の GUID を入れる。
   - `DennokoSceneUI.cs`：テーマ / ブロック / シーンの USS（3 つ）
   - 各 Overlay の `UXML_GUID`
3. `YourSceneToolState` の `ToolId` / `DisplayName` / `ShortcutTag` を変え、Overlay の id と表示名、ショートカットのパス（`"Your Tool/..."`）も変える。
   HUD のルート名（`DennokoSceneFeedback.RootName`）とラジアルのルート名もツールごとに変えておくと、2 つのツールを入れても重ならない。
4. ウィンドウ（P4）/ Inspector（P5）の主ボタンから `YourSceneToolState.Begin(target)` を呼ぶ。終了はバナーの「終了」から `YourSceneToolState.End()`。
   ウィンドウは `OnDisable` で `DennokoSceneSession.End(DennokoSessionEndReason.WindowClosed)` を呼び、
   `DennokoSceneSession.Ended` を受けてステータスバーに `EndMessage(reason)` を出す。
5. `YourSceneToolState.OnEnd` に保存を書く。どの終わり方（Esc・Play モード・再コンパイル・別のツールの開始など）でも必ず呼ばれる。
6. ツールの値はすべて `YourSceneToolState` に置き、どこで変えても `Modify`（Undo に載せる）か `Raise`（載せない）を呼ぶ。UI 側に値のコピーを持たない。

```csharp
// P4 のウィンドウ側の例
root.Q<Button>("primary-button").clicked += () =>
{
    if (YourSceneToolState.Active) Export();
    else YourSceneToolState.Begin(targetField.value as GameObject);
};
DennokoSceneSession.Ended += reason => _status.Set(DennokoSceneSession.EndMessage(reason));
YourSceneToolState.Changed += RefreshBannerAndPrimaryButton;   // B7 バナーと主ボタンの文言（作業開始 ⇔ 出力）
```

参照実装：UI_Design の UI Design Test「15. SCENE UI」（`Editor/Mock/Scene/`）で、ツールバー型・パネル型の両方が動く。

## 6. 確認すること

- [ ] Light / Dark どちらのエディタテーマでも、Overlay の見出し・枠（Unity 側が描く部分）とテーマの中身が浮かないか
- [ ] SceneView を 2 つ開いたとき、S1 / S2 が両方に出ても操作が食い違わないか
- [ ] 再コンパイル・Play モード・シーン切替で自動終了し、ハンドル・タグ・HUD が残らないか
- [ ] スライダーの 1 ドラッグが Undo 1 段になるか。シーン側のホイールで変えた値がパネルに出るか
- [ ] 表示スケール 150% / 200% でカーソル脇の表示とラジアルの位置がずれないか
- [ ] 背の低い SceneView でパネルが画面外にはみ出さないか
- [ ] 明るい背景・暗い背景どちらでもシーンの線（2 重線）が見えるか
