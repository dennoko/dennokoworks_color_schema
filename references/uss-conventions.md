# USS 記述規約 — テーマ非依存が成立する仕組みと独自スタイルの書き方

`DennokoTheme.uss` に独自クラスやオーバーライドを**追加・変更するときに読む**。
既存テンプレートをそのまま使うだけなら読む必要はない。

## 1. テーマ非依存が成立する仕組み

Unity エディタのビルトインコントロールは、エディタテーマ（Personal Light / Dark）の
デフォルトスタイルシートで装飾されている。`DennokoTheme.uss` は
**`.dennoko-root` を起点とする子孫セレクタ**でビルトインクラスを上書きする。

```css
/* Unity デフォルト: .unity-button { ... }  (テーマ依存) */
/* 本テーマ:        .dennoko-root .unity-button { ... }  (常にこちらが勝つ) */
```

成立条件は 1 つ: **すべての UI のルート要素に `dennoko-root` クラスを付与すること。**
`dennoko-root` は USS 変数（`--dennoko-*`）の定義元でもあり、
変数は子孫にのみ継承されるため、忘れるとスタイル全体が無効になる。

## 2. 詳細度の規則（最重要）

- **USS に `!important` は存在しない。書いてもパースされず宣言が破棄される**
  （コンソールに警告が出る）。優先度はすべて詳細度と定義順で解決する。
- **独自クラス（`.dennoko-*`）は必ず `.dennoko-root` を前置する。**
  汎用リセット `.dennoko-root .unity-text-element` / `.dennoko-root .unity-button` (0,2,0)
  があるため、裸の `.dennoko-title` (0,1,0) では color 宣言が負ける。
  「`.dennoko-root` 前置 (0,2,0) + リセットより後方に定義（同数詳細度は後勝ち）」で勝たせる。
- `:hover` (0,3,0) にも勝つ必要がある場合はクラスを連結して詳細度を上げる。
  例: `.dennoko-root .unity-button.dennoko-button-active` (0,3,0)。
- **「直接マッチ」は「継承」に必ず勝つ。** Toggle のように文字が子要素
  （`.unity-toggle__text`）にあるコントロールへ色クラスを付けても、
  汎用リセットが子要素に直接マッチするため親からの継承色は届かない。
  `.dennoko-root .dennoko-section-title .unity-text-element` のように
  子孫セレクタを併記して子要素まで色を届かせる（テーマ USS に実例あり）。
- 入力フィールドの内側テキスト要素に適用してよいのは **color のみ**。
  背景・枠・パディングまで掛けるとフィールド内部が二重ボックスになり崩れる。

## 3. カラーは USS 変数を経由する

カラーコードの直書きは `.dennoko-root` の変数定義部のみに限定する。
UXML のインライン style や C# の `style.color` にも直書きしない。

```css
/* ❌ 禁止 */  .dennoko-root .my-label { color: #aaaaaa; }
/* ✅ 正しい */ .dennoko-root .my-label { color: var(--dennoko-text-tertiary); }
```

## 4. USS と CSS の構文差（よくあるエラー）

| やりたいこと | CSS | USS |
|---|---|---|
| 枠線 | `border: 1px solid #484848;` | ショートハンド不可。`border-width: 1px; border-color: ...;` に分ける |
| 背景 | `background: #121212;` | `background` 不可。`background-color` を使う |
| 太字 | `font-weight: bold;` | `-unity-font-style: bold;` |
| 文字揃え | `text-align: center;` | `-unity-text-align: middle-center;` |
| 折り返し | `overflow-wrap` 等 | `white-space: normal;` |
| 最優先 | `!important` | 存在しない。詳細度で解決する |
| box-shadow | `box-shadow: ...;` | 存在しない。Surface の明度差で浮遊感を出す（フローティングデザインの流儀） |

`padding: 4px 12px;` のような 1〜4 値ショートハンドは margin / padding では使える。

## 5. レイアウトは Flexbox

UI Toolkit のレイアウトエンジンは Flexbox (Yoga)。

```css
/* 横並び (IMGUI の BeginHorizontal 相当) */
.my-row { flex-direction: row; align-items: center; }

/* 右寄せ (FlexibleSpace 相当) */
.spacer { flex-grow: 1; }
```

- デフォルトは `flex-direction: column`（縦積み）、**`flex-shrink: 1`**
- `justify-content: space-between` でタイトルとボタンを両端配置
- **固定サイズを保つべき要素（ヘッダー・フッター・セパレーター等）には
  `flex-shrink: 0` を付ける**。付けないとウィンドウ縮小時に潰れる
  （テーマ USS の `.dennoko-header` 等には設定済み）
- row 内で幅いっぱいに広げる子には `flex-grow: 1`

## 6. 動的な要素生成 (C# で作ってよいケース)

レイアウト構造は原則 UXML に書くが、**リストアイテムのような動的な繰り返し要素**は
C# で生成してよい。その場合もインラインスタイルではなく USS クラスを付与する。

```csharp
// ❌ 悪い例: C# にスタイルをハードコード
var item = new VisualElement();
item.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);

// ✅ 良い例: USS クラスを付与し、見た目は USS に集約
var item = new VisualElement();
item.AddToClassList("dennoko-card");
```

行テンプレートが複雑な場合は、アイテム専用の UXML を用意して
`itemUxml.CloneTree()`（または `ListView.makeItem`）で複製する。

```csharp
listView.makeItem = () => itemUxml.CloneTree();
listView.bindItem = (element, index) =>
{
    element.Q<Label>("item-name").text = _items[index].name;
};
```
