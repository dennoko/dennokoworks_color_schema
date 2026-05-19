# IMGUI 実装テクニック集

Unity Editor 拡張 (IMGUI) で dennoko.dev カラースキーマを表現するための具体的な手法。

---

## 1. ウィンドウ背景の塗りつぶし

IMGUI はデフォルトで Unity のエディタテーマ背景を描画する。
`surface.level0` で全面を上書きすることで、暗部レイヤーのベースを作る。

```csharp
private void OnGUI()
{
    // OnGUI の一番最初に呼ぶ
    EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), UniTexTheme.Surface0);
    // ...
}
```

> **注意:** `EditorGUI.DrawRect` は `EventType.Layout` 中も安全に呼び出せる。

---

## 2. カード（Elevation）の実装

### 2-a. 9スライスボーダーテクスチャ

CSS の `border: 1px solid` に相当するカード枠を IMGUI で再現する方法。

```csharp
/// <summary>
/// 3×3 テクスチャを生成する。
/// 外周 1px = borderColor、中心 = fillColor。
/// GUIStyle.border = new RectOffset(1,1,1,1) と組み合わせて 1px 枠として機能する。
/// </summary>
private static Texture2D MakeBorderedTex(Color fillColor, Color borderColor)
{
    const int size = 3;
    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y,
                (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    ? borderColor
                    : fillColor);
    tex.Apply();
    tex.filterMode = FilterMode.Point; // ぼかし防止
    tex.hideFlags  = HideFlags.HideAndDontSave;
    return tex;
}
```

### 2-b. GUIStyle への適用

```csharp
var cardStyle = new GUIStyle();
cardStyle.normal.background = MakeBorderedTex(Surface1, Outline);
cardStyle.border  = new RectOffset(1, 1, 1, 1); // 9スライスの非伸縮領域サイズ
cardStyle.padding = new RectOffset(10, 10, 8, 8);
cardStyle.margin  = new RectOffset(4, 4, 6, 6);  // カード間の余白
```

### 2-c. ツールバー付きカード（padding=0）

プレビューエリアのようにツールバーをカード端まで伸ばす場合は、
`CardOuterStyle`（padding=0）と `CardStyle`（padding=10）を使い分ける。

```csharp
// padding=0 の外枠
var cardOuterStyle = new GUIStyle();
cardOuterStyle.normal.background = MakeBorderedTex(Surface1, Outline);
cardOuterStyle.border  = new RectOffset(1, 1, 1, 1);
cardOuterStyle.padding = new RectOffset(0, 0, 0, 0);
cardOuterStyle.margin  = new RectOffset(4, 4, 6, 6);

// 使用例
GUILayout.BeginVertical(cardOuterStyle);
    GUILayout.BeginHorizontal(toolbarStyle); // Surface2 背景
    // ...ツールバー行...
    GUILayout.EndHorizontal();
    // コンテンツは手動パディング
    Rect container = GUILayoutUtility.GetRect(0, contentHeight + 8, GUILayout.ExpandWidth(true));
    Rect inner = new Rect(container.x + 4, container.y + 4, container.width - 8, contentHeight);
GUILayout.EndVertical();
```

---

## 3. テクスチャのライフサイクル管理

### 問題: ドメインリロード後の無効化

Unity がドメインリロード（スクリプト再コンパイル後）を行うと、
`HideFlags.HideAndDontSave` テクスチャは破棄され、`static` 参照は `null` にリセットされる。

### 解決策: null チェックで再生成

```csharp
private static bool _initialized;
private static Texture2D _texSurface1;

public static void Initialize()
{
    if (_initialized) return;
    _initialized = true;
    EnsureTextures();
    BuildStyles();
}

private static void EnsureTextures()
{
    // Unity の null 比較演算子でオブジェクト生存確認
    if (!_texSurface1) _texSurface1 = MakeTex(Surface1);
}
```

> `!texture` は `texture == null || texture.Equals(null)` と等価。
> ドメインリロード後は `_initialized` が `false` に戻るため、`EnsureTextures` が再実行される。

---

## 4. タイポグラフィ階層の実装

### セクション見出し（SectionHeader）

```csharp
var sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
sectionHeaderStyle.fontSize = 10;
sectionHeaderStyle.normal.textColor = TextTertiary; // #888888
```

### トグルセクションの ON/OFF 文字色切り替え

`EditorGUILayout.ToggleLeft` の第3引数に `GUIStyle` を渡すとラベル部分のスタイルが変わる。
ON/OFF で別スタイルを渡して状態を文字色で表現する。

```csharp
var toggleOnStyle = new GUIStyle(EditorStyles.boldLabel);
toggleOnStyle.fontSize = 10;
toggleOnStyle.normal.textColor = TextPrimary;   // ON: white (#ffffff)

var toggleOffStyle = new GUIStyle(EditorStyles.boldLabel);
toggleOffStyle.fontSize = 10;
toggleOffStyle.normal.textColor = TextTertiary; // OFF: gray (#888888)

// DrawToggleSection 内で使用
var headerStyle = toggle ? toggleOnStyle : toggleOffStyle;
bool newToggle = EditorGUILayout.ToggleLeft(title.ToUpper(), toggle, headerStyle);
```

---

## 5. ボタンのスタイリング

> **重要:** `new GUIStyle(GUI.skin.button)` のように標準ボタンスタイルを継承してはいけない。
> Unity の標準ボタンには `scaledBackgrounds`・角丸・グラデーションが組み込まれており、
> フラットなテクスチャを上書きしても元の装飾が残ってしまう。
> **EditorWindow でも CustomEditor でも `new GUIStyle()` からすべてのプロパティを明示的に設定する。**

### Primary Action（Apply & Save など）

```csharp
// ❌ 悪い例: 標準ボタンを継承すると角丸・グラデーションが混ざる
// var actionButtonStyle = new GUIStyle(GUI.skin.button);

// ✅ 良い例: まっさらな GUIStyle からフラットなスタイルを構築する
var actionButtonStyle = new GUIStyle();
actionButtonStyle.normal.background  = MakeBorderedTex(Surface2, Outline);
actionButtonStyle.normal.textColor   = TextPrimary;
actionButtonStyle.hover.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.07f));
actionButtonStyle.hover.textColor    = TextPrimary;
actionButtonStyle.active.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.15f));
actionButtonStyle.active.textColor   = TextPrimary;
actionButtonStyle.border       = new RectOffset(1, 1, 1, 1);
actionButtonStyle.margin       = new RectOffset(4, 4, 2, 2); // 継承をやめたので明示的に指定
actionButtonStyle.padding      = new RectOffset(6, 6, 3, 3); // 継承をやめたので明示的に指定
actionButtonStyle.fontSize     = 13;
actionButtonStyle.fontStyle    = FontStyle.Bold;
actionButtonStyle.fixedHeight  = 34;
actionButtonStyle.alignment    = TextAnchor.MiddleCenter;
actionButtonStyle.stretchWidth = true; // GUILayout で幅を自動拡張するために必要
```

### Secondary Action（Reset All など）

```csharp
var secondaryButtonStyle = new GUIStyle();
secondaryButtonStyle.normal.background = MakeBorderedTex(Surface1, Outline);
secondaryButtonStyle.normal.textColor  = TextSecondary;
secondaryButtonStyle.hover.background  = MakeBorderedTex(Surface2, Outline);
secondaryButtonStyle.hover.textColor   = TextPrimary;
secondaryButtonStyle.active.background = MakeTex(Color.Lerp(Surface1, Color.white, 0.10f));
secondaryButtonStyle.active.textColor  = TextPrimary;
secondaryButtonStyle.border       = new RectOffset(1, 1, 1, 1);
secondaryButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
secondaryButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
secondaryButtonStyle.fontSize     = 11;
secondaryButtonStyle.fixedHeight  = 26;
secondaryButtonStyle.alignment    = TextAnchor.MiddleCenter;
secondaryButtonStyle.stretchWidth = true;
```

### Mini Button（Reset / Select など小さなボタン）

```csharp
// EditorStyles.miniButton* も同様に継承してはいけない
var miniButtonStyle = new GUIStyle();
miniButtonStyle.normal.background = MakeBorderedTex(Surface2, Outline);
miniButtonStyle.normal.textColor  = TextTertiary;
miniButtonStyle.hover.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
miniButtonStyle.hover.textColor   = TextSecondary;
miniButtonStyle.active.background = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
miniButtonStyle.active.textColor  = TextPrimary;
miniButtonStyle.border      = new RectOffset(1, 1, 1, 1);
miniButtonStyle.margin      = new RectOffset(2, 2, 1, 1);
miniButtonStyle.padding     = new RectOffset(4, 4, 1, 2);
miniButtonStyle.fontSize    = 10;
miniButtonStyle.fixedHeight = 16;
miniButtonStyle.alignment   = TextAnchor.MiddleCenter;
```

---

## 6. ステータスバー（Semantic Color）

HelpBox の代わりに `GUILayout.Box` を使い、ステータス種別ごとにスタイルを切り替える。

```csharp
private static GUIStyle MakeStatusStyle(Color bgColor, Color textColor)
{
    var style = new GUIStyle(EditorStyles.helpBox);
    style.normal.background = MakeTex(bgColor);
    style.normal.textColor  = textColor;
    style.border  = new RectOffset(1, 1, 1, 1);
    style.padding = new RectOffset(8, 8, 5, 5);
    style.fontSize = 11;
    return style;
}

// 使用例
var statusInfoStyle    = MakeStatusStyle(Surface1,                                   TextSecondary);
var statusSuccessStyle = MakeStatusStyle(Color.Lerp(Surface1, SemanticSuccess, 0.3f), SemanticSuccess);
var statusErrorStyle   = MakeStatusStyle(Color.Lerp(Surface1, SemanticError,   0.5f), new Color(1f, 0.65f, 0.65f));

// DrawStatusBar
GUILayout.Box(message, GetStatusStyle(statusType), GUILayout.ExpandWidth(true));
```

---

## 7. セパレーター（1px 区切り線）

```csharp
private void DrawSeparator()
{
    var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
    EditorGUI.DrawRect(rect, UniTexTheme.Outline); // #3a3a3a
    EditorGUILayout.Space(4);
}
```

セクションタイトルとコンテンツの間、フッター内の区切りなど汎用的に使用できる。

---

## 8. 色変換ユーティリティ

```csharp
/// <summary>0xRRGGBB → Color (alpha=1)</summary>
private static Color Hex(int rgb) => new Color(
    ((rgb >> 16) & 0xFF) / 255f,
    ((rgb >>  8) & 0xFF) / 255f,
    ( rgb        & 0xFF) / 255f);

// 使用例
public static readonly Color Surface0 = Hex(0x121212);
public static readonly Color Outline  = Hex(0x3a3a3a);
```
