# トラブルシューティングと動作確認チェックリスト

見た目の崩れ・テーマ切替時の問題が起きたとき、および実装完了時の確認に読む。

## 症状別インデックス

| 症状 | 原因 | 対処 |
|---|---|---|
| スタイルが全く効かない | `dennoko-root` 付け忘れ / GUID がプレースホルダーのまま / `styleSheets.Add` 忘れ | §1 |
| Light テーマで文字が黒くなる | オーバーライド外の要素 / `!important` の使用（無効） | §2 |
| Light テーマでアイコンが消える | tint は乗算で黒画像を明るくできない | §3 |
| Foldout の矢印が白い箱になる | Toggle のチェックボックス装飾が波及 | §4 |
| 独自クラスの文字色が効かない | 詳細度で汎用リセットに負けている | references/uss-conventions.md §2 |
| 入力欄の内部が二重枠になる | 内側テキスト要素にボックススタイルを適用 | references/uss-conventions.md §2 |
| ウィンドウ縮小でヘッダー等が潰れる | `flex-shrink: 0` の欠落 | references/uss-conventions.md §5 |
| ドロップダウンのメニューだけテーマの見た目 | 仕様（既知の限界） | §5 |
| Inspector の PropertyField が空 | `Bind()` 忘れ | references/inspector-guide.md §3 |
| Inspector の左右に明るい隙間 | InspectorElement の余白 | references/inspector-guide.md §1 |
| IMGUI 併用部分が Light テーマで読めない | IMGUI はテーマ USS の対象外 | §7 |
| 文字が一切表示されない（レイアウトは正常） | OS フォントをレガシー Font 経由で適用 | §8 |

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
例: チェック時のチェックマーク（白 accent 背景 + 暗 tint）は両テーマで視認できる。

対象になりやすい要素（**必ず Light テーマの実機で確認**）:
ドロップダウン矢印 `.unity-base-popup-field__arrow` / チェックマーク
`.unity-toggle__checkmark` / ObjectField ピッカー `.unity-object-field__selector` /
スクロールバー矢印 `.unity-scroller__low-button` `__high-button`

## 4. Foldout の矢印が「白い箱」になる

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

正しい実装（テンプレート C# の `GetUIFontAsset()` に定義済み）:

```csharp
// OS フォントから直接 SDF FontAsset を生成する（Unity 2022.3 で public）
var fontAsset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset("Meiryo", "Regular");
if (fontAsset != null)
{
    fontAsset.hideFlags = HideFlags.HideAndDontSave;
    root.style.unityFontDefinition = FontDefinition.FromSDFFont(fontAsset);
}
```

- フォントが見つからない場合は `Unable to find a font file...` というログと共に
  null が返るだけなので、そのままエディタ標準フォントにフォールバックする
- 生成した FontAsset は static にキャッシュし `HideAndDontSave` を付ける

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
   - 文字が全部消えている場合はレガシー Font 経由で適用している（§8）
