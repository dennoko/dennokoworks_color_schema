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

### テーマ非依存: EditorStyles を継承せず new GUIStyle() から構築する

`new GUIStyle(EditorStyles.boldLabel)` のように継承すると、設定しなかった state（`hover`、`active`、`onNormal` など）に Unity エディタのスキン色が混入する。ライト/ダーク切り替えで見た目が変化する原因になる。

**`new GUIStyle()` から構築し、必要なプロパティをすべて明示設定すること。**

```csharp
// テーマクラスに追加するヘルパーメソッド
private static void FixAllTextColors(GUIStyle style, Color color)
{
    style.normal.textColor    = color;
    style.hover.textColor     = color;
    style.active.textColor    = color;
    style.focused.textColor   = color;
    style.onNormal.textColor  = color;  // ← トグル ON 状態・選択状態に使われる
    style.onHover.textColor   = color;
    style.onActive.textColor  = color;
    style.onFocused.textColor = color;
}
```

> **`onNormal.textColor` が特に重要。**
> `EditorGUILayout.ToggleLeft` はトグル ON 時に `onNormal` を使用する。
> `GUILayout.Toggle` でボタンスタイルを使う場合も同様（言語切替ボタンなど）。

### セクション見出し（SectionHeader）

```csharp
var sectionHeaderStyle = new GUIStyle(); // EditorStyles を継承しない
sectionHeaderStyle.fontStyle = FontStyle.Bold;
sectionHeaderStyle.fontSize  = 10;
sectionHeaderStyle.margin    = new RectOffset(0, 0, 0, 2);
FixAllTextColors(sectionHeaderStyle, TextTertiary); // 全 state を #aaaaaa に固定
```

### トグルセクションの ON/OFF 文字色切り替え

`EditorGUILayout.ToggleLeft` の第3引数に `GUIStyle` を渡すとラベル部分のスタイルが変わる。
ON/OFF で別スタイルを渡して状態を文字色で表現する。

```csharp
var toggleOnStyle = new GUIStyle(); // EditorStyles を継承しない
toggleOnStyle.fontStyle = FontStyle.Bold;
toggleOnStyle.fontSize  = 10;
FixAllTextColors(toggleOnStyle, TextPrimary);   // ON: 全 state を #ffffff に固定

var toggleOffStyle = new GUIStyle(); // EditorStyles を継承しない
toggleOffStyle.fontStyle = FontStyle.Bold;
toggleOffStyle.fontSize  = 10;
FixAllTextColors(toggleOffStyle, TextTertiary); // OFF: 全 state を #aaaaaa に固定

// DrawToggleSection 内で使用
var headerStyle = toggle ? toggleOnStyle : toggleOffStyle;
bool newToggle = EditorGUILayout.ToggleLeft(title, toggle, headerStyle);
```

> **NG パターン（テーマ切り替えで壊れる）:**
> ```csharp
> // EditorStyles を継承するとスキン色が混入する
> var toggleOnStyle = new GUIStyle(EditorStyles.boldLabel); // NG
> // normal しか設定していない → onNormal に未定義色が残る
> toggleOnStyle.normal.textColor = TextPrimary; // NG
> ```

---

## 5. ボタンのスタイリング

> **重要:** `new GUIStyle(GUI.skin.button)` のように標準ボタンスタイルを継承してはいけない。
> Unity の標準ボタンには `scaledBackgrounds`・角丸・グラデーションが組み込まれており、
> フラットなテクスチャを上書きしても元の装飾が残ってしまう。
> **EditorWindow でも CustomEditor でも `new GUIStyle()` からすべてのプロパティを明示的に設定する。**

### Primary Action（Apply & Save など）

背景テクスチャを設定してから `FixAllTextColors` で全 state を統一する。

// ❌ 悪い例: 標準ボタンを継承すると角丸・グラデーションが混ざる
// var actionButtonStyle = new GUIStyle(GUI.skin.button);

// ✅ 良い例: まっさらな GUIStyle からフラットなスタイルを構築する
var actionButtonStyle = new GUIStyle();
actionButtonStyle.normal.background  = MakeBorderedTex(Surface2, Outline);
actionButtonStyle.hover.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.07f));
actionButtonStyle.active.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.15f));
actionButtonStyle.border       = new RectOffset(1, 1, 1, 1);
actionButtonStyle.margin       = new RectOffset(4, 4, 2, 2); // 継承をやめたので明示的に指定
actionButtonStyle.padding      = new RectOffset(6, 6, 3, 3); // 継承をやめたので明示的に指定
actionButtonStyle.fontSize     = 13;
actionButtonStyle.fontStyle    = FontStyle.Bold;
actionButtonStyle.fixedHeight  = 34;
actionButtonStyle.alignment    = TextAnchor.MiddleCenter;
actionButtonStyle.stretchWidth = true; // GUILayout で幅を自動拡張するために必要
FixAllTextColors(actionButtonStyle, TextPrimary); // 全 state を白に固定
```

### Secondary Action（Reset All など）

```csharp
var secondaryButtonStyle = new GUIStyle();
secondaryButtonStyle.normal.background = MakeBorderedTex(Surface1, Outline);
secondaryButtonStyle.hover.background  = MakeBorderedTex(Surface2, Outline);
secondaryButtonStyle.active.background = MakeTex(Color.Lerp(Surface1, Color.white, 0.10f));
secondaryButtonStyle.border       = new RectOffset(1, 1, 1, 1);
secondaryButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
secondaryButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
secondaryButtonStyle.fontSize     = 11;
secondaryButtonStyle.fixedHeight  = 26;
secondaryButtonStyle.alignment    = TextAnchor.MiddleCenter;
secondaryButtonStyle.stretchWidth = true;

// ライトモードでの全状態テキスト色固定
secondaryButtonStyle.normal.textColor   = TextSecondary;
secondaryButtonStyle.hover.textColor    = TextPrimary;
secondaryButtonStyle.active.textColor   = TextPrimary;
secondaryButtonStyle.focused.textColor  = TextSecondary;
secondaryButtonStyle.onNormal.textColor  = TextSecondary;
secondaryButtonStyle.onHover.textColor   = TextPrimary;
secondaryButtonStyle.onActive.textColor  = TextPrimary;
secondaryButtonStyle.onFocused.textColor = TextSecondary;
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
`FixAllTextColors` で全 state を固定すること。

```csharp
var statusBase = new GUIStyle(); // EditorStyles.helpBox を継承しない
statusBase.border    = new RectOffset(1, 1, 1, 1);
statusBase.padding   = new RectOffset(8, 8, 5, 5);
statusBase.fontSize  = 11;
statusBase.wordWrap  = true;
statusBase.alignment = TextAnchor.MiddleLeft;

var statusInfoStyle = new GUIStyle(statusBase);
statusInfoStyle.normal.background = MakeTex(Surface1);
FixAllTextColors(statusInfoStyle, TextSecondary);   // 全 state を固定

var statusSuccessStyle = new GUIStyle(statusBase);
statusSuccessStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticSuccess, 0.3f));
FixAllTextColors(statusSuccessStyle, SemanticSuccess);

var statusErrorStyle = new GUIStyle(statusBase);
statusErrorStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticError, 0.5f));
FixAllTextColors(statusErrorStyle, new Color(1f, 0.65f, 0.65f));

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

---

## 9. Unity 組み込みコントロールのライトモード対応

### 問題

`EditorGUILayout.Toggle`・`ObjectField`・`IntSlider`・`Popup`・`TextField` などの
Unity 組み込みコントロールは `EditorStyles` を直接参照する。
カスタム `GUIStyle` を修正しても、これらのコントロールの外観は EditorStyles に依存したままで、
ライト/ダーク両モードで以下の問題が発生する可能性がある。

| コントロール | ライトモードの問題 |
|---|---|
| Toggle / Slider ラベル | `EditorStyles.label` → 黒文字、ダーク背景に埋もれる |
| ObjectField 入力欄 | `EditorStyles.objectField` → 白背景 |
| Slider 数値入力 | `EditorStyles.numberField` → 白背景 |
| TextField | `EditorStyles.textField` → 白背景 |
| Popup | `EditorStyles.popup` → 白背景 |

### 解決策: PushEditorTheme / PopEditorTheme

OnGUI スコープ内でのみ EditorStyles を一時上書きする。
ライト/ダーク両モードで常時適用し、テーマによらず一定の外観を保証する。
`finally` ブロックで確実に元の値を復元することで、他の EditorWindow に影響しない。

```csharp
private void OnGUI()
{
    YourTheme.Initialize();
    YourTheme.PushEditorTheme(); // ライト/ダーク両モードで常時 EditorStyles を上書き

    try
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), YourTheme.Surface0);
        // ... すべての UI 描画 ...
    }
    finally
    {
        YourTheme.PopEditorTheme(); // 例外が起きても必ず復元
    }
}
```

### PushEditorTheme / PopEditorTheme の実装

```csharp
private static bool _overrideActive;
public static bool IsOverrideActive => _overrideActive;

private class GUIStyleBackup
{
    private readonly GUIStyle _style;
    private readonly Color _normalColor, _hoverColor, _activeColor, _focusedColor;
    private readonly Color _onNormalColor, _onHoverColor, _onActiveColor, _onFocusedColor;
    private readonly Texture2D _normalBg, _hoverBg, _activeBg, _focusedBg;
    private readonly Texture2D _onNormalBg, _onHoverBg, _onActiveBg, _onFocusedBg;
    private readonly RectOffset _border;
    private readonly RectOffset _padding;

    public GUIStyleBackup(GUIStyle style)
    {
        _style = style;
        _normalColor = style.normal.textColor;
        _hoverColor = style.hover.textColor;
        _activeColor = style.active.textColor;
        _focusedColor = style.focused.textColor;
        _onNormalColor = style.onNormal.textColor;
        _onHoverColor = style.onHover.textColor;
        _onActiveColor = style.onActive.textColor;
        _onFocusedColor = style.onFocused.textColor;

        _normalBg = style.normal.background;
        _hoverBg = style.hover.background;
        _activeBg = style.active.background;
        _focusedBg = style.focused.background;
        _onNormalBg = style.onNormal.background;
        _onHoverBg = style.onHover.background;
        _onActiveBg = style.onActive.background;
        _onFocusedBg = style.onFocused.background;

        _border = style.border;
        _padding = style.padding;
    }

    public void Restore()
    {
        _style.normal.textColor = _normalColor;
        _style.hover.textColor = _hoverColor;
        _style.active.textColor = _activeColor;
        _style.focused.textColor = _focusedColor;
        _style.onNormal.textColor = _onNormalColor;
        _style.onHover.textColor = _onHoverColor;
        _style.onActive.textColor = _onActiveColor;
        _style.onFocused.textColor = _onFocusedColor;

        _style.normal.background = _normalBg;
        _style.hover.background = _hoverBg;
        _style.active.background = _activeBg;
        _style.focused.background = _focusedBg;
        _style.onNormal.background = _onNormalBg;
        _style.onHover.background = _onHoverBg;
        _style.onActive.background = _onActiveBg;
        _style.onFocused.background = _onFocusedBg;

        _style.border = _border;
        _style.padding = _padding;
    }
}

private static GUIStyleBackup[] _backups;

public static void PushEditorTheme()
{
    // ダークモード（ProSkin）では上書き不要
    _overrideActive = true;

    if (_backups == null)
    {
        _backups = new[]
        {
            new GUIStyleBackup(EditorStyles.label),
            new GUIStyleBackup(EditorStyles.objectField),
            new GUIStyleBackup(EditorStyles.numberField),
            new GUIStyleBackup(EditorStyles.textField),
            new GUIStyleBackup(EditorStyles.popup),
            new GUIStyleBackup(EditorStyles.toggle)
        };
    }

    // ── テキスト色を固定
    FixAllTextColors(EditorStyles.label, TextSecondary);
    FixAllTextColors(EditorStyles.objectField, TextSecondary);
    FixAllTextColors(EditorStyles.numberField, TextSecondary);
    FixAllTextColors(EditorStyles.textField,   TextSecondary);
    FixAllTextColors(EditorStyles.popup,       TextSecondary);
    FixAllTextColors(EditorStyles.toggle,      TextSecondary);

    // ── 背景テクスチャをすべての状態でダーク色に固定 (ホバー・フォーカス時の白背景リークを防ぐ)
    FixAllStateBackgrounds(EditorStyles.objectField, _texSurface1);  // 1×1 dark texture
    FixAllStateBackgrounds(EditorStyles.numberField, _texSurface1);
    FixAllStateBackgrounds(EditorStyles.textField,   _texSurface1);

    // ── ポップアップ（Popup）は9スライス境界を補正して縞ノイズを解消
    FixAllStateBackgrounds(EditorStyles.popup, _texCard);            // bordered dark texture
    EditorStyles.popup.border = new RectOffset(1, 1, 1, 1);         // 3x3テクスチャに適合する1px境界に固定
    EditorStyles.popup.padding = new RectOffset(6, 18, 4, 4);
}

public static void PopEditorTheme()
{
    if (!_overrideActive) return;
    _overrideActive = false;

    if (_backups != null)
    {
        foreach (var backup in _backups)
        {
            backup.Restore();
        }
    }
}

private static void FixAllStateBackgrounds(GUIStyle style, Texture2D tex)
{
    style.normal.background    = tex;
    style.hover.background     = tex;
    style.active.background    = tex;
    style.focused.background   = tex;
    style.onNormal.background  = tex;
    style.onHover.background   = tex;
    style.onActive.background  = tex;
    style.onFocused.background = tex;
}
```

### ポップアップ矢印（▼）の再描画テクニック

ライトテーマの背景画像を完全に上書きしたことにより、デフォルトで背景テクスチャに書き込まれていたドロップダウンの矢印（▼）が消去されます。
これを解決するため、以下のようなラッパーメソッドを実装して、ライトテーマ適用時のみ右端に矢印文字を重ねて描画します。

```csharp
private int DrawPopup(GUIContent label, int selectedIndex, string[] displayedOptions)
{
    int index = EditorGUILayout.Popup(label, selectedIndex, displayedOptions);
    if (UniTexTheme.IsOverrideActive) // ライトテーマ強制中のみ描画
    {
        Rect rect = GUILayoutUtility.GetLastRect();
        float boxRight = rect.xMax;
        Rect arrowRect = new Rect(boxRight - 16, rect.y + (rect.height - 12) / 2, 12, 12);
        GUI.Label(arrowRect, "▼", UniTexTheme.CaptionStyle);
    }
    return index;
}
```

### 注意事項

- **常時適用**: ライト/ダーク両モードで常に EditorStyles を上書きし、テーマ依存を完全に排除する。
- **`finally` ブロック必須**: 復元を保証しないと他の EditorWindow（Inspector 等）の表示が壊れる。
- **対象外要素**: Toggle のチェックボックス画像（Unity 内蔵テクスチャ）は置換不可。ライトモードでも機能的には使用可能。
- **すべての状態（States）の保存・復元**: `normal` だけでなく `hover`・`active`・`focused`・`onNormal` などの背景・テキスト色も網羅的に上書き・復元しないと、操作時にライトモードの明るい色が漏れ出て「ちらつき」や「文字の消失」が発生する。
- **9スライス拡大（Border）の整合性**: 背景を 3x3 の枠線付きテクスチャなどに差し替える場合、差し替え対象スタイルの `border` プロパティも 1px (`new RectOffset(1,1,1,1)`) に変更しなければならない。元の大きな境界線定義が維持されたままだと、テクスチャが不自然に引き伸ばされて縞模様（ストライプ）ノイズが発生する。

