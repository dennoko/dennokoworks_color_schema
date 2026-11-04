# トラブルシューティングと動作確認チェックリスト

見た目の崩れ・テーマ切替時の問題が起きたとき、および実装完了時の確認に読む。

## 症状別インデックス

| 症状 | 原因 | 対処 |
|---|---|---|
| スタイルが全く効かない | `dennoko-root` 付け忘れ / GUID がプレースホルダーのまま / `styleSheets.Add` 忘れ | §1 |
| Light テーマで文字が黒くなる | オーバーライド外の要素 / `!important` の使用（無効） | §2 |
| Light テーマでアイコンが消える | tint は乗算で黒画像を明るくできない | §3 |
| Foldout の矢印が白い箱になる | Toggle のチェックボックス装飾が波及 | §4-① |
| チェックボックスとラベルが入力欄のような箱で囲まれる | Toggle の `__input` に入力フィールド装飾が乗る | §4-② |
| 独自クラスの文字色が効かない | 詳細度で汎用リセットに負けている | references/uss-conventions.md §2 |
| 入力欄の内部が二重枠になる | 内側テキスト要素にボックススタイルを適用 | references/uss-conventions.md §2 |
| ウィンドウ縮小でヘッダー等が潰れる | `flex-shrink: 0` の欠落 | references/uss-conventions.md §5 |
| ドロップダウンのメニューだけテーマの見た目 | 仕様（既知の限界） | §5 |
| Inspector の PropertyField が空 | `Bind()` 忘れ | references/inspector-guide.md §3 |
| Inspector の左右に明るい隙間 | InspectorElement の余白 | references/inspector-guide.md §1 |
| IMGUI 併用部分が Light テーマで読めない | IMGUI はテーマ USS の対象外 | §7 |
| 文字が一切表示されない（レイアウトは正常） | OS フォントをレガシー Font 経由で適用 | §8 |
| 操作の途中で突然テキストが崩れ `MissingReferenceException: ... Material ... get_mainTexture` | 動的 FontAsset のアトラス material / texture に hideFlags 未伝播 → UnloadUnusedAssets で破棄 | §9 |
| しばらく使うと**一部の文字だけ**□ / 空白になる | 実行中に増えた追加アトラスが未保護のまま破棄された | §10-① |
| 一度テキストが崩れると再起動まで直らない | 破棄済み FontAsset をキャッシュから返し続けている | §10-② |
| コンパイルのたびにメモリが増える / 崩れやすくなる | ドメインリロードで FontAsset とアトラスが leak している | §10-③ |
| ドック解除／再ドックしたウィンドウだけ後からテキストが崩れて直らない | 再アタッチ時に自己修復の監視リストへ戻していない | §10-④ |
| 横並び行で入力欄がカードの幅を超え、隣のボタンが画面外に消える | `flex-grow` だけで `flex-shrink` を付け忘れている | references/uss-conventions.md §5(頻出ミス) |
| チェックボックス / ラジオの ON と OFF が見分けられない（無効時は特に） | OFF も白塗りにしている | §4-③ |
| RadioButtonGroup が外枠で囲まれ、各選択肢も入力欄のように枠で囲まれる | Toggle と同じく `__input` に入力フィールド装飾が乗る | §4-③ |
| Vector / Rect / Bounds が「全体枠 > 各数値枠」の二重・三重枠になる | 親の `__input` にも入力欄装飾が乗る | §11-① |
| X / Y / Z の入力欄が狭く右に余白が残る | サブフィールドが均等割りされていない | §11-① |
| BoundsField の Center / Extents が X と重なる（`CXen` のように見える） | サブラベルのビルトイン padding / margin が残っている | §11-② |
| Slider のレールが入力欄のような枠で囲まれ、数値欄と二重枠になる | Slider の `__input` に入力フィールド装飾が乗る | §12 |
| エラー文字だけ極端に読みにくい | 文字に `--dennoko-semantic-error` (#9b1b30) を使っている | §13 |
| 入力欄・選択欄・ボタンが同じトーンで役割が分からない | Elevation の使い分けをしていない | §13 |
| 無効状態のトーンがコンポーネントごとにばらばら | `:disabled` の色を統一していない / `opacity` の掛け算 | §14 |
| ListView の交互行が選択中・ホバー中に見える | 交互行の明度差が大きすぎる | §15 |
| 2 カラムのグリッドで右列の開始位置が行ごとにずれる / カードがスクロールバーの下にはみ出す | `width: 50%` の子に `min-width` を付け、コンテンツ幅が広がっている | §16 |

## 1. スタイルが全く適用されない

原因はほぼ次の 3 つ。

1. ルート要素に `dennoko-root` クラスを付け忘れている（USS 変数が継承されない）
2. `USS_GUID` / `UXML_GUID` がプレースホルダーのまま、または間違っている
3. `root.styleSheets.Add(uss)` を呼んでいない

## 2. Light テーマで文字色が崩れる

Unity 標準の Light テーマは、ビルトインコントロール内部のテキスト要素に黒文字を
直接適用する。テーマ USS は子孫セレクタの詳細度 (0,3,0) でこれを上書き済み。

```css
/* DennokoTheme.uss に定義済み。color のみを内側に適用する */
.dennoko-root .unity-base-field__input .unity-text-element,
.dennoko-root .unity-object-field__input .unity-object-field-display__label {
    color: var(--dennoko-text-primary);
}
```

カバー外のビルトイン要素で崩れた場合は、同じパターン
（`.dennoko-root` + 対象要素の子孫セレクタ + `color` のみ）で追加する。
`!important` は USS に存在しないので使わない。

キャレット・選択範囲の色も Light テーマで見えなくなる。テーマ USS の
`--unity-cursor-color` / `--unity-selection-color`（要実機確認）で対処済み。

## 3. アイコン画像が Light テーマで見えない — tint の限界

> **⚠ `-unity-background-image-tint-color` は「乗算」である。**
> 黒い画像にどれだけ明るい tint を掛けても黒のまま（0 × 1 = 0）なので、
> **tint だけでは Light テーマの黒アイコン問題は解決できない。**

対策は 2 段階:

1. **画像自体をテーマ非依存にする** — `background-image` にダークスキン用
   （`d_` 接頭辞）のビルトインアイコンを明示指定するか、自前テクスチャを同梱する。
2. その上で tint で色味を揃える。

```css
.dennoko-root .unity-base-popup-field__arrow {
    background-image: resource("d_dropdown"); /* 明るいアイコンに固定 (名前は要実機確認) */
    -unity-background-image-tint-color: var(--dennoko-text-secondary);
}
```

tint だけで成立する例外は「明るい背景 × 暗い tint」の組み合わせ。
例: チェック時のチェックマーク（白 accent 背景 + 暗色 `var(--dennoko-checkmark)` (#121212) tint + `resource("d_checkmark")`）は両テーマでクッキリ視認できる。
なお、チェックマークは `background-size: 80% 80%` で少し小さく表示している（線幅は画像に焼き込まれていて、
縮小でしか細くできないため）。Foldout の展開矢印は同じ要素を流用するので、`background-size: initial` で打ち消している。

対象になりやすい要素（**必ず Light テーマの実機で確認**）:
ドロップダウン矢印 `.unity-base-popup-field__arrow` / チェックマーク
`.unity-toggle__checkmark` / ObjectField ピッカー `.unity-object-field__selector` /
スクロールバー矢印 `.unity-scroller__low-button` `__high-button`

## 4. Toggle 由来の「箱」の崩れ

### ① Foldout の矢印が「白い箱」になる

Foldout の展開矢印は内部的に Toggle のチェックマーク要素
(`.unity-toggle__checkmark`) を流用しているため、チェックボックスの
オーバーライドが矢印にもかかる。テーマ USS では以下のリセットで対処済み。
テーマを自作・改変する場合は必ず入れること。

```css
.dennoko-root .unity-foldout__toggle .unity-toggle__checkmark,
.dennoko-root .unity-foldout__toggle:checked .unity-toggle__checkmark,
.dennoko-root .unity-foldout__toggle:hover .unity-toggle__checkmark {
    background-color: transparent;
    border-width: 0;
    -unity-background-image-tint-color: var(--dennoko-text-secondary);
}
```

`:checked` / `:hover` も併記するのは、疑似クラス付きセレクタのほうが詳細度が高く、
素の打ち消しだけでは展開時・ホバー時に負けるため。

### ② チェックボックスとラベルが入力欄のような箱で囲まれる

**トグルは「入力欄」ではなくテキスト行として見せる。周囲に囲みを付けない。**

Toggle の入力コンテナ `.unity-toggle__input` は `.unity-base-field__input` でも
あるため、§③の入力フィールド装飾（背景・枠線・角丸・パディング）がそのまま乗り、
チェックボックス + ラベル全体が箱で囲まれてしまう。テーマ USS の §④で打ち消し済み。

```css
.dennoko-root .unity-toggle .unity-base-field__input,
.dennoko-root .unity-toggle:hover .unity-base-field__input,
.dennoko-root .unity-toggle:focus .unity-base-field__input,
.dennoko-root .unity-toggle .unity-base-field__input:focus {
    background-color: transparent;
    border-color: transparent;
    border-width: 0;
    padding: 0;
}
```

`:hover` / `:focus` を併記するのは①と同じ理由。素の打ち消し (0,3,0) は
`.dennoko-root .unity-base-field:hover .unity-base-field__input` (0,4,0) に負けるため、
同じ詳細度のセレクタを §③ より後ろに置いて上書きする。

> **⚠ 併発しやすい派生バグ:** `Toggle.text` のラベルは `.unity-base-field__input` の
> **内側**に `Label`（= `.unity-label` かつ `.unity-text-element`）として生成される。
> そのため §③で内側のテキスト要素にボックススタイルを足すと、
> **ラベル文字そのものが箱で囲まれる。** §③の内側セレクタは `color` のみに留めること
> （references/uss-conventions.md §2 の「二重枠」と同じ原則）。
> このときコンテナ側だけをリセットしても直らない — 箱の実体は内側の
> `.unity-toggle__text` にある。テーマ USS §④に打ち消しを入れてある。

なお `Toggle(label)` コンストラクタで作った場合、文字は `__input` の外側
（`.unity-base-field__label`）に出るためこの問題は起きない。囲みが出るのは
`Toggle.text` / UXML の `text=` を使ったときだけ。
（それでも Toggle は `text=` で書くのが原則。`label=` はラベル列 150px の 2 カラムになり、
`text=` のトグルと混在すると左右が逆転して見える）

### ③ ON / OFF が見分けられない・ラジオボタンが箱で囲まれる

**OFF = 暗色の箱 + 枠線のみ、ON = 白 (accent) 塗り + 暗色のチェック** にする。
OFF も白で塗ると、白い箱 = チェック済みに見えて ON と区別できない。無効時は
ON をグレー塗り・OFF を暗い枠のみにして、無効でも判別できるようにする（§14）。

```css
.dennoko-root .unity-toggle .unity-toggle__checkmark {          /* OFF */
    background-color: var(--dennoko-surface-0);
    background-image: none;
    border-color: var(--dennoko-outline);
    border-width: 1px;
}
.dennoko-root .unity-toggle:checked .unity-toggle__checkmark {  /* ON */
    background-color: var(--dennoko-accent);
    border-color: var(--dennoko-accent);
    background-image: resource("d_checkmark");
    -unity-background-image-tint-color: var(--dennoko-checkmark); /* #121212 */
}
```

RadioButton / RadioButtonGroup も Toggle と同じ構造（`__input` が `.unity-base-field__input`）のため、
グループの外枠・各選択肢の枠がどちらも出る。テーマ USS §④-b で打ち消し、丸
(`.unity-radio-button__checkmark-background`) と点 (`.unity-radio-button__checkmark`) を背景色で描いている。
RadioButtonGroup は自身も BaseField なので、グループのホバーで内側の打ち消しが負ける
（祖先ホバー込みのセレクタを併記する。references/uss-conventions.md §2）。

## 5. ドロップダウンのポップアップメニューはスタイル不可（既知の限界）

ポップアップメニューは別ウィンドウ（別パネル）として描画され `.dennoko-root` の
外側にあるため、USS を適用できない。制御できるのはフィールド本体まで。

## 6. トグルをボタン型トグルにする設計（推奨パターン）

標準のチェックマーク付きトグルはテーマ切替で崩れやすい。ツールバー等では
Button ベースの ON/OFF 表示（`.dennoko-button-active` クラスの付け外し）を推奨する。

```csharp
void UpdateToggleState(Button button, bool enabled, string textOn, string textOff)
{
    if (button == null) return;
    if (enabled)
    {
        button.text = textOn;
        button.AddToClassList("dennoko-button-active");
    }
    else
    {
        button.text = textOff;
        button.RemoveFromClassList("dennoko-button-active");
    }
}
```

排他的な選択（モード切替、サブツール選択など）では、選択中のボタンにのみクラスが
付くように毎回まとめて更新する。`EnableInClassList` を使うと ON/OFF を 1 行で書ける。

```csharp
void RefreshModeUI()
{
    _selectBtn.EnableInClassList("dennoko-button-active", !_settings.IsPaintMode);
    _paintBtn.EnableInClassList("dennoko-button-active",   _settings.IsPaintMode);

    _brushBtn.EnableInClassList("dennoko-button-active", _settings.SubMode == SubMode.Brush);
    _rectBtn.EnableInClassList("dennoko-button-active",  _settings.SubMode == SubMode.Rect);
    // ...
}
```

### 選択中は「青枠 + 青ティント背景」で示す

通常のボタンも Surface2 に浮かせているため、明度差だけではどれが選択中か分からない。
DennokoTheme.uss の `.dennoko-button-active` は、枠線を `--dennoko-selection-border` (#64b5f6)、
背景を `--dennoko-selection-bg`（薄い青）にしてこれを解決している。

- 選択状態を**背景色だけ・枠線だけ**で表現しない。両方を併用する
- 独自にアクティブ用スタイルを書く場合も `--dennoko-selection-*` を使い、
  カラーコードを直書きしない。`--dennoko-accent`（白）はフォーカス・Primary 用で、選択中には使わない
- ツールバー内のボタンはゴースト表示（枠なし）になるため、ツールバー用のアクティブ指定
  `.dennoko-toolbar > .unity-button.dennoko-button-active` で青枠を復活させている
- `:hover` / `:active` の擬似クラスにも `border-color` を再指定する。汎用の
  `.dennoko-root .unity-button:active`（accent の白枠）と詳細度が同じ (0,3,0) のため、
  再指定しないと押下中だけ青枠が白枠に戻ってしまう

## 7. IMGUI (`OnGUI()`) を併用する場合のみの対策

UI Toolkit のみで構築する場合は不要（references/imgui-migration.md 参照）。

- **数値入力欄が「白背景に白文字」になる**: `PushEditorTheme` で文字を白に固定して
  いる場合、Light テーマでは入力欄の背景画像が白いため読めなくなる。
  `EditorStyles.numberField` / `textField` / `GUI.skin.textField` に
  入力欄専用の暗い背景テクスチャを強制適用する。
- **テクスチャ型 `ObjectField` の Select ボタンが潰れて重なる**: 高さが 20px 等と
  狭いと Unity が正方形サムネイルモード (`ObjectFieldThumb`) で描画しようとして崩れる。
  スタイル引数に `EditorStyles.objectField` を明示指定してサムネイルモードを無効化する。

```csharp
float originalLabelWidth = EditorGUIUtility.labelWidth;
EditorGUIUtility.labelWidth = 48f;
var tex = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("画像", "説明"),
                                                 currentTex, typeof(Texture2D), false,
                                                 EditorStyles.objectField, GUILayout.Height(RowH));
EditorGUIUtility.labelWidth = originalLabelWidth;
```

## 8. 文字が一切表示されない — OS フォントはレガシー Font 経由で適用しない

標準フォント（OS のメイリオ。SKILL.md 絶対規則 6）を適用する際、
`Font.CreateDynamicFontFromOSFont()` で作ったレガシー `Font` を
`FontDefinition.FromFont()` で渡してはならない。

UI Toolkit のテキストは TextCore（SDF）で描画されるが、OS 動的フォントは
フォントデータ本体を持たない参照オブジェクトのため、TextCore が FontAsset へ
変換できずグリフ生成が静かに失敗する。結果、レイアウトやスタイルは正常なまま
**テキストだけがすべて消える**（実際に発生した事故。エラーも出ない）。

正しい実装は `assets/Shared/DennokoUIFont.cs` に集約済み。呼び出し側は 1 行だけ:

```csharp
root.AddToClassList("dennoko-root");
DennokoUIFont.Apply(root);   // 生成・アトラス保護・再適用をすべて内包
```

内部で使っている API は次のもの（**自前で書き直さないこと**。§9・§10 の対策が抜ける）:

```csharp
// OS フォントから直接 SDF FontAsset を生成する（Unity 2022.3 で public）
var fontAsset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset("Meiryo", "Regular");
```

- フォントが見つからない場合は `Unable to find a font file...` というログと共に
  null が返るだけなので、そのままエディタ標準フォントにフォールバックする
- `DennokoUIFont` は取得失敗時に `unityFontDefinition` へ
  `StyleKeyword.Null` を入れ、破棄済みオブジェクトへのダングリング参照を残さない

## 9. 操作の途中でテキストが崩れる — 動的 FontAsset のアトラスが破棄される

`CreateFontAsset()` で作った SDF FontAsset に対し、**FontAsset 本体にだけ**
`hideFlags = HideAndDontSave` を付けると、しばらく操作した後に突然テキストが崩れ、
コンソールに次の例外が出ることがある（実際に発生した事故）:

```
MissingReferenceException: The object of type 'Material' has been destroyed
  but you are still trying to access it.
UnityEngine.Material.get_mainTexture ()
... UIRStylePainter.DrawTextInfo ...
```

原因は、`CreateFontAsset()` が実行時に生成する**フォントアトラスの `material` と
`atlasTextures`（Texture2D）が FontAsset 本体とは別の `UnityEngine.Object`** であり、
`hideFlags` が自動伝播しないこと。これらを放置すると「どのアセットからも参照されない
一時オブジェクト」と見なされ、`Resources.UnloadUnusedAssets()`（PNG 書き出し後の
`AssetDatabase.Refresh`、プレイモード遷移、シーン保存などで暗黙的に呼ばれる）で破棄される。
FontAsset は破棄済み material を参照し続けるため、次のテキスト描画で上記例外になる。

対処: 本体・`material`・`atlasTextures` すべてに `HideAndDontSave` を伝播させる
（`DennokoUIFont` の `Protect()` に実装済み）:

```csharp
private static void Protect(FontAsset fa)
{
    fa.hideFlags = HideFlags.HideAndDontSave;

    if (fa.material != null)
        fa.material.hideFlags = HideFlags.HideAndDontSave;

    var atlasTextures = fa.atlasTextures;
    if (atlasTextures != null)
    {
        foreach (var tex in atlasTextures)
            if (tex != null)
                tex.hideFlags = HideFlags.HideAndDontSave;
    }
}
```

> ⚠ **これを「生成直後に 1 回」だけ呼ぶのでは不十分。** アトラスは実行中に増えるため、
> 定期的に呼び直す必要がある（**§10 必読**）。

## 10. フォントキャッシュ消失と自己修復 — §9 対策だけでは再発する

§9 の `hideFlags` 伝播を入れても、次の 4 経路でキャッシュが失われ UI が崩れる。
`assets/Shared/DennokoUIFont.cs` はこの 4 つすべてに対処済みなので、
**フォント処理を自前で書かず必ずこのファイルを配置して使うこと。**

### ① 実行中に増えたアトラステクスチャが未保護のまま破棄される（最頻出）

`CreateFontAsset("Meiryo", "Regular")` が作るアトラスは既定設定では 1 枚に収まる
グリフ数が限られ、日本語 UI ではすぐ埋まる。新しい文字が出るたびに TextCore が**追加の
`Texture2D` を実行時に生成する**（`isMultiAtlasTexturesEnabled` が既定で true）。
この 2 枚目以降は §9 の伝播処理より**後**に生まれるので `hideFlags` が既定のまま残り、
次の `UnloadUnusedAssets()` で破棄される。

症状は §9 の例外に加えて、**先に焼かれた文字は出るのに後から出た文字だけ □ / 空白になる**。
オブジェクト名やエラーメッセージなど動的な文字列で顕在化しやすい。

対策は 2 つ併用する:

- **`Protect()` を定期的に呼び直す**（冪等）。`DennokoUIFont` はウィンドウが開いている間だけ
  `EditorApplication.update` で 2 秒ごとに回し、増えたアトラスを拾う
- **ウォームアップ**: 生成時に `TryAddCharacters()` で UI に出る文字をまとめて焼き、
  そもそもアトラスが増える機会を減らす。`DennokoUIFont.WarmupJapanese` に
  **ツール固有の日本語を書き足すこと**

```csharp
try { fa.TryAddCharacters(WarmupAscii + WarmupJapanese, out _); }
catch { /* Unity バージョン差があるため必ず握りつぶす */ }
```

> `CreateFontAsset(family, style, pointSize)` の第 3 引数（サンプリングサイズ。省略時は
> 大きめの既定値）を 48〜60 程度へ下げると 1 枚に入るグリフ数が増え、アトラス追加自体が
> 起きにくくなる。UI サイズなら SDF の品質劣化はほぼ見えないはずだが、
> **採用前に実機で確認すること**（このオーバーロードが public なのは Unity 2022.3 で確認済み）。

### ② 破棄済みキャッシュを返し続ける（崩れたまま復帰しない）

```csharp
// ❌ 禁止パターン
if (_uiFontSearched) return _uiFontAsset;   // 破棄済みでもそのまま返る
```

この「一度探したら二度と作り直さない」ラッチだと、①で FontAsset やアトラスが死んだあと
**再生成の経路が存在せず、Unity を再起動するまで直らない**。

正しくは毎回生存確認する。fake-null 対策として本体だけでなくアトラスまで見る:

```csharp
private static bool IsAlive(FontAsset fa)
{
    if (fa == null) return false;              // Unity の == が破棄済みも false にする
    if (fa.material == null) return false;
    var texs = fa.atlasTextures;
    return texs != null && texs.Length > 0 && texs[0] != null;
}
```

再試行を止めてよいのは「フォント未搭載環境で `CreateFontAsset` が null を返した」場合のみ
（`_unavailable` フラグ）。これを区別しないと Mac / Linux で毎ティック生成を試みることになる。

さらに、**すでに開いているウィンドウの `unityFontDefinition` は死んだ FontAsset を
指したまま**なので、作り直したら再適用まで行う必要がある。`DennokoUIFont` は適用先ルートを
リストで保持し、`Revalidate()` で全ルートへ貼り直す。あわせて `AttachToPanelEvent` でも
再適用し、再ドック・レイアウト変更・リロード後の復帰を担保する。

再点検のフックは以下（いずれも `UnloadUnusedAssets` を暗黙的に挟みうる）:

```csharp
AssemblyReloadEvents.afterAssemblyReload += Revalidate;
EditorApplication.playModeStateChanged   += _ => Revalidate();
EditorApplication.projectChanged         += Revalidate;   // AssetDatabase.Refresh 後
```

### ③ ドメインリロードでの leak

`HideAndDontSave` のオブジェクトはドメインリロードを生き延びるが、`static` フィールドの
参照は消える。そのままだとコンパイルのたびに FontAsset とアトラスが増え続ける。
生成時に固定名を付け、次回はそれを拾って再利用する:

```csharp
private const string AssetName = "Dennoko_UIFont_Meiryo";

private static FontAsset FindExisting()
{
    foreach (var fa in Resources.FindObjectsOfTypeAll<FontAsset>())
        if (fa != null && fa.name == AssetName && IsAlive(fa)) { Protect(fa); return fa; }
    return null;
}
```

同じプロジェクトに dennokoworks 製ツールが複数入っていても 1 つの FontAsset を共有できる。

### ④ 再ドック後に監視対象から外れて自己修復が止まる

`DetachFromPanelEvent` で監視リスト (`_roots`) から外したまま、`AttachToPanelEvent` で
フォントを貼り直すだけにすると、ウィンドウをドック解除／再ドックした時点でそのウィンドウが
①〜③ の自己修復対象から永久に外れる。見た目はその場では正常なので気付きにくいが、
以後アトラスが破棄されると崩れたまま復帰しなくなる。**再アタッチ時に監視リストへ戻すこと。**

```csharp
root.RegisterCallback<AttachToPanelEvent>(_ => { Track(root); ApplyTo(root); });
root.RegisterCallback<DetachFromPanelEvent>(_ => _roots.Remove(root));

/// <summary>自己修復の監視対象に入れる（冪等）。</summary>
private static void Track(VisualElement root)
{
    if (!_roots.Contains(root)) _roots.Add(root);
    HookTick(true);
}
```

## 11. 複合フィールド (Vector2/3/4・Rect・Bounds) の崩れ

### ① 二重・三重枠 / X・Y・Z の幅が狭い

複合フィールドは「親の `__input` の中に、各サブフィールド（FloatField）の `__input`」がある構造で、
§③の入力欄装飾が両方に乗ると二重枠になる。BoundsField は「Bounds > Vector3Field (Center/Extents) >
FloatField」の 3 段なので三重枠になる。テーマ USS §③-b で**親側の枠を子セレクタで消し、末端の
数値欄だけに枠を出す**:

```css
.dennoko-root .unity-composite-field > .unity-base-field__input,
.dennoko-root .unity-bounds-field > .unity-base-field__input {
    background-color: transparent;
    border-width: 0;
    padding: 0;
}
```

サブフィールドは `.unity-composite-field__field` に `flex-grow: 1; flex-shrink: 1; flex-basis: 0` を付けて
均等割りにし、ラベル→数値の間は 3px、ペア同士の間は 8px（`--first` のみ 0）にしてグルーピングを明確にする。

### ② BoundsField の Center / Extents が X と重なる

BoundsField はラベルが上段に出る構造なので、テーマ USS では `flex-direction: row` にして他の
フィールドと同じ 2 カラムに揃え、Center / Extents のラベル幅を 48px に固定している。
⚠ 固定幅にするだけだと、ビルトイン USS の `padding-left` / `margin` が残って固定幅の中が空白で埋まり、
文字が右へはみ出して X ラベルと重なる（`CXen` / `EXxte` のように見える）。padding / margin を
すべて明示的に 0 にすること。あわせて 2 行の間に `margin-bottom: 3px`、メインラベルに
`padding-top: 6px`（1 段目のベースラインに揃える）を指定している。

## 12. Slider が入力欄のような枠で囲まれる

Slider / MinMaxSlider の `__input` も `.unity-base-field__input` なので、§③の装飾が乗ると
レール全体が枠で囲まれ、右端の数値入力欄と二重枠になる。数値入力欄の有無で行の高さも揃わない。
スライダーは文字入力欄ではないので、`.unity-base-slider > .unity-base-field__input` の背景と枠を
消して「レール + つまみ + 数値欄」だけにする（テーマ USS §⑥）。

⚠ スクロールバーも内部に Slider を持つため、Slider 用の `min-height` などがスクロールバーに
波及しないよう `.unity-scroller .unity-base-slider` で打ち消している。

## 13. Elevation と文字色のコントラスト

- **入力欄・選択欄・ボタンが同じトーン**だと、クリック対象か入力対象かが分からない。
  文字入力欄は Surface0 に沈め、ボタン・選択欄（`.unity-base-popup-field` / `.unity-enum-field`）は
  Surface2 に浮かせる（テーマ USS で自動適用）。
- **エラー文字**に `--dennoko-semantic-error` (#9b1b30) を使うと暗背景でほぼ読めない。
  文字には `--dennoko-semantic-error-text` (#ff6b6b) を使う（`.dennoko-text-error` はこちらを参照）。
  `#9b1b30` は背景・枠線専用。

## 14. 無効状態のトーンがばらばら

Unity の既定ではボタンだけ極端に暗く、入力欄は通常とほぼ同じ明るさのまま、といったばらつきが出る。
テーマ USS 末尾 §⑩ で `:disabled`（`SetEnabled(false)` した要素の子孫すべてに付く）に対して
文字 = `--dennoko-text-disabled`、枠 = 一段暗く、選択欄の塗り = Surface1、チェック ON = グレー塗り、
と一律に指定している。

- ⚠ `.unity-disabled { opacity: ... }` は使わない。このクラスは子孫全員に付くため、
  入れ子の段数だけ透明度が掛け算になり、深い要素ほど見えなくなる。
- ⚠ §⑩ は通常・ホバーのルールと同じ詳細度になることが多い。**必ずファイル末尾に置く**（後勝ち）。

## 15. ListView の交互行・選択行

Unity 既定の交互行は明度差が大きく、交互行がすべて選択中・ホバー中に見える。テーマ USS §⑧ で
交互行を `--dennoko-list-alt-bg`（ごくわずかな差）、ホバーを `--dennoko-hover-overlay`、
選択行を `--dennoko-selection-bg` にしている。

- ListView の高さは `fixed-item-height × 表示行数 + 上下ボーダー` に合わせると、
  最終行の下に中途半端な余白が残らない。
- `makeItem` で作る Label は既定の上下 margin / padding で上寄りになるため、0 にして
  `-unity-text-align: middle-left` を指定する。

## 16. 2 カラムのグリッドがずれる / カードがスクロールバーの下にはみ出す

`flex-wrap: wrap` の子に `width: 50%` と `min-width` を併用すると、ウィンドウが狭いときに
`min-width` の合計がコンテンツ幅を押し広げ、右列の位置がずれたり、ScrollView のコンテンツが
ビューポートより広がってカードがスクロールバーの下に潜り込んだりする。
`width: 50%; max-width: 50%; flex-basis: 50%; flex-grow: 0; flex-shrink: 0;` で幅を固定し、
`min-width` は付けず、はみ出す文字は `overflow: hidden; text-overflow: ellipsis; white-space: nowrap` で省略する。

## 動作確認チェックリスト（実装完了時に必ず実施）

1. **Preferences でテーマを Light / Dark 両方に切り替えたか？**
   - ラベルが黒くて読めない、ボタン背景が白く浮く箇所がないか
   - `TextField` / `ObjectField` / `DropdownField` の入力文字・選択テキストが読めるか
   - キャレット・選択範囲の色が見えるか（`--unity-cursor-color` 等。要実機確認）
   - ドロップダウン矢印・スクロールバー矢印などアイコン画像が消えていないか
     （消える場合は `background-image` 差し替え。§3）
   - IMGUI 併用部分が「明るい背景に白文字」になっていないか
2. **GUID プレースホルダーを実際の GUID に置き換えたか？**
3. **ルート要素に `dennoko-root` を付けたか？**
4. **カラーを直書きせず USS 変数を経由しているか？**
5. **C# で `new VisualElement()` を手組みしてスタイリングしていないか？**（動的リスト以外）
6. **Inspector の場合、`Bind(serializedObject)` を呼んだか？**
7. **ウィンドウを極端に狭く / 広くしてレイアウト崩れがないか？**
   - 固定クローム（ヘッダー・フッター・ステータスバー）に `flex-shrink: 0` が付いており、
     縮小時に `.dennoko-scroll`（ScrollView）側だけが縮むか
8. **テクスチャ型 `ObjectField` の Select ボタンが崩れて被っていないか？**（IMGUI 併用時。§7）
9. **文字がメイリオで表示されているか？**（標準フォント。SKILL.md 絶対規則 6）
   - `assets/Shared/DennokoUIFont.cs` を配置し、ルートに `DennokoUIFont.Apply(root)` を
     呼んでいるか（FontAsset をウィンドウ側で自前生成していないか）
   - 文字が全部消えている場合はレガシー Font 経由で適用している（§8）
   - PNG 書き出しやプレイモード遷移など `UnloadUnusedAssets` を挟む操作の後にテキストが崩れ
     `MissingReferenceException`（`Material.get_mainTexture`）が出る場合は、FontAsset の
     アトラス material / texture に hideFlags を伝播していない（§9・`Protect()`）
10. **フォントキャッシュ消失の耐性を実機で確認したか？**（§10）
    - 日本語を多く含む画面を一通り開き、**後から出た文字だけ □ / 空白**にならないか
      （オブジェクト名・エラーメッセージなど動的な文字列で確認する）
    - ウィンドウを開いたままスクリプトを再コンパイル → テキストが正常に戻るか
    - ウィンドウを開いたままプレイモードを往復 → テキストが正常に戻るか
    - `DennokoUIFont.WarmupJapanese` にツール固有の日本語を書き足したか
11. **チェックボックス（Toggle）が囲まれていないか？**（§4-②）
    - 素の状態でチェックボックス + ラベルの周りに枠・背景が出ていないか
    - **マウスを乗せた状態**でも箱が出ないか（ホバー時だけ復活する崩れがある）
    - トグルで制御する項目のまとまりをカードや枠で囲っていないか
      （階層はインデントで示す）
    - ON / OFF が（無効状態でも）見分けられるか（§4-③）
12. **状態表現は揃っているか？**
    - 文字入力欄は沈み (Surface0)、ボタン・選択欄は浮いているか (Surface2)（§13）
    - 選択中のボタン・タブに青枠 + 青ティント背景が出ているか（§6）
    - Primary ボタン（白塗り）が画面に 1 つだけか
    - 無効状態のトーンが全コンポーネントで揃っているか（§14）
13. **トグル付きセクションは OFF で折りたたまれるか？**
    - 中身に `.dennoko-toggle-section__content` を付け、`BindToggleSection()` を呼んでいるか
    - グレーアウト（`SetEnabled(false)`）で済ませていないか
14. **枠線が重なっていないか？**
    - フッターを `.dennoko-card` で囲んでいないか（上罫線のみ）
    - 複合フィールド・Slider・RadioButtonGroup が二重枠になっていないか（§11・§12・§4-③）
