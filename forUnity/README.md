# dennoko.dev Color Schema — Unity IMGUI 実装ガイド

`colors_spec.md` / `design_reference.md` で定義したカラースキーマを **Unity Editor 拡張 (IMGUI)** に適用するためのガイドです。

---

## まず読むもの（このリポジトリだけで実装できる）

```
1. ../example/index.html             ← ビジュアルターゲット（ブラウザで開く）
2. ../Docs/design_reference.md       ← デザインコンセプト（フローティング・Elevation）
3. ../Docs/colors_spec.md            ← カラー仕様（各色の役割）
4. ../colors.json                    ← カラー実値（#RRGGBB）
5. forUnity/UniTexTheme_template.md  ← テーマクラス C# コード（コピー元）
6. forUnity/techniques.md            ← IMGUI 固有の実装テクニック詳説
7. forUnity/window_structure_template.md     ← EditorWindow 全体の骨格（コピー元）
8. forUnity/inspector_structure_template.md  ← CustomEditor (Inspector) の骨格（コピー元）
```

---

## このフォルダのファイル構成

| ファイル | 内容 |
|---|---|
| `README.md` | このファイル。概要と全体手順 |
| `UniTexTheme_template.md` | コピーして使うテーマクラス全文 |
| `window_structure_template.md` | EditorWindow の骨格・DrawSection などヘルパー全文 |
| `inspector_structure_template.md` | CustomEditor (Inspector) の骨格・InspectorRootStyle パターン |
| `techniques.md` | IMGUI 固有の実装テクニック詳説 |

---

## デザインの基本方針

`design_reference.md` の「暗部のレイヤーとフローティング」コンセプトを IMGUI で再現する。

| CSS の概念 | Unity IMGUI での実現方法 |
|---|---|
| `background-color` (surface-0) | `EditorGUI.DrawRect` でウィンドウ全面を塗る |
| `border: 1px solid` | 3×3 ボーダーテクスチャ + `GUIStyle.border = RectOffset(1,1,1,1)` |
| `box-shadow` (Elevation) | surface-1 と surface-0 の明度差によって浮いて見える |
| `hover` | `GUIStyle.hover.background` に明るめ色のテクスチャを設定 |
| `color: typography.tertiary` | `FixAllTextColors(style, TextTertiary)` で全 state を固定 |

> **重要: `EditorStyles.*` 継承スタイルのライトモード対策**
> `new GUIStyle(EditorStyles.boldLabel)` などで継承したスタイルは、設定しなかった state
> （`hover`・`active`・`onNormal` など）が Unity エディタのスキン色を引き継ぐ。
> ライトモードでは黒に近い色が使われ、ダークカード上でテキストが見えなくなる。
> テーマクラスの `FixAllTextColors(style, color)` ヘルパーで全 state を必ず固定すること。
> 詳細は `techniques.md` の「4. タイポグラフィ階層の実装」を参照。

---

## 実装手順

### Step 1 — テーマクラスをコピーする

`UniTexTheme_template.md` のコードブロックを `Scripts/Editor/YourTheme.cs` として配置する。

変更箇所:
- `namespace YourNamespace` → プロジェクトの namespace
- クラス名 `YourTheme` → 任意（例: `MyToolTheme`）
- `GetStatusStyle` の引数型を自ウィンドウの `StatusType` enum に合わせる

### Step 2 — ウィンドウ骨格をコピーする

`window_structure_template.md` のコードブロックを `Scripts/Editor/YourEditorWindow.cs` として配置する。

変更箇所:
- `namespace` / クラス名
- `[MenuItem("Tools/Your Tool Name")]` のメニューパス
- `DrawSettingsArea()` の中身（セクション定義）
- `ApplyAndSave()` / `ResetAll()` の実装

### Step 3 — OnGUI の先頭で初期化する

```csharp
private void OnGUI()
{
    YourTheme.Initialize(); // 初回のみスタイルを構築（以降はキャッシュ）

    // ウィンドウ全面に surface.level0 (#121212) を塗る
    EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), YourTheme.Surface0);

    DrawHeader();
    // ...
}
```

### Step 4 — セクションを追加する

**常時表示セクション** (`DrawSection`)

```csharp
DrawSection("INPUT", () =>
{
    sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
        "Source", sourceTexture, typeof(Texture2D), false);
});
```

**ON/OFF トグル付きセクション** (`DrawToggleSection`)

```csharp
private bool _showColorCorrection = true;

DrawToggleSection("COLOR CORRECTION", ref _showColorCorrection, () =>
{
    hue = EditorGUILayout.Slider("Hue", hue, -180f, 180f);
    sat = EditorGUILayout.Slider("Saturation", sat, 0f, 2f);
}, onReset: () =>
{
    hue = 0f; sat = 1f;
});
```

- `toggle = true` → セクションタイトルが `TextPrimary` (白) で表示される
- `toggle = false` → `TextTertiary` (グレー) でグレーアウト表示、コンテンツは操作不可

### Step 5 — ステータスバーを使う

```csharp
public enum StatusType { Info, Success, Error }
private string     _statusMessage  = "Ready";
private StatusType _statusType     = StatusType.Info;
private double     _statusResetTime = -1.0;

// ステータスセット（3秒後に "Ready" へ自動リセット）
private void SetStatus(string message, StatusType type, double seconds = 3.0)
{
    _statusMessage   = message;
    _statusType      = type;
    _statusResetTime = type == StatusType.Info
        ? -1.0
        : EditorApplication.timeSinceStartup + seconds;
    Repaint();
}

// DrawStatusBar
private void DrawStatusBar()
{
    GUILayout.Box(_statusMessage, GetStatusStyle(_statusType), GUILayout.ExpandWidth(true));
}

private GUIStyle GetStatusStyle(StatusType type) => type switch
{
    StatusType.Success => YourTheme.StatusSuccessStyle,
    StatusType.Error   => YourTheme.StatusErrorStyle,
    _                  => YourTheme.StatusInfoStyle,
};
```

### Step 6 — セパレーターを追加する

```csharp
private void DrawSeparator()
{
    var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
    EditorGUI.DrawRect(rect, YourTheme.Outline);
    EditorGUILayout.Space(4);
}
```

---

## カラーパレット早見表

`colors.json` / `Docs/colors_spec.md` の内容を Unity の変数名で対応させたもの。

| 役割 | HEX | `YourTheme` の変数名 |
|---|---|---|
| アプリ背景 | `#121212` | `Surface0` |
| カード・入力欄 | `#1e1e1e` | `Surface1` |
| ツールバー・ホバー | `#2c2c2c` | `Surface2` |
| 境界線・セパレーター | `#3a3a3a` | `Outline` |
| タイトル文字 | `#ffffff` | `TextPrimary` |
| 本文・ラベル | `#cccccc` | `TextSecondary` |
| 補足・見出し | `#aaaaaa` | `TextTertiary` |
| 無効状態文字 | `#555555` | `TextDisabled` |
| エラー | `#9b1b30` | `SemanticError` |
| 警告 | `#ffb74d` | `SemanticWarning` |
| 成功 | `#4caf50` | `SemanticSuccess` |
| 情報 | `#64b5f6` | `SemanticInfo` |

---

## よくある疑問

**Q: ボタンが角丸やグラデーションになる・フラットにならない**

A: `new GUIStyle(GUI.skin.button)` のように Unity の標準ボタンスタイルを継承すると、`scaledBackgrounds` や角丸・ドロップシャドウの描画設定が引き継がれ、フラットなテクスチャを上書きしても意図しない見た目になる。**この問題は EditorWindow でも CustomEditor (Inspector) でも同様に発生する。**

→ **ボタンは `new GUIStyle()` から構築する。** 継承をやめる分、`margin`・`padding`・`stretchWidth` を明示的に設定すること。

```csharp
// ❌ 悪い例: Unity の標準装飾が混ざり、角丸やグラデーションが残ってしまう
ActionButtonStyle = new GUIStyle(GUI.skin.button);
ActionButtonStyle.normal.background = _texAccentCard;

// ✅ 良い例: まっさらな状態からフラットなスタイルを構築する
ActionButtonStyle = new GUIStyle();
ActionButtonStyle.normal.background = _texAccentCard;
ActionButtonStyle.border       = new RectOffset(1, 1, 1, 1);
ActionButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
ActionButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
ActionButtonStyle.stretchWidth = true;  // GUILayout で幅を自動拡張するために必要
// ... 他プロパティ ...
```

詳細な解説は `inspector_structure_template.md` の「カスタムボタンスタイルが...」セクションを参照。

---

**Q: ドメインリロード後にテクスチャが壊れる**

A: `EnsureTextures()` で `if (!_texSurface1) _texSurface1 = ...` のように Unity の null 比較を使っていれば、ドメインリロード後に自動再生成される。`_initialized` static フラグもリロードで `false` に戻るため再構築が走る。`techniques.md` の「3. テクスチャのライフサイクル管理」を参照。

**Q: 複数の EditorWindow を同時に開くと壊れる**

A: テーマのスタイルは `static` で共有されるため複数ウィンドウでも問題ない。テクスチャも同様に共有される。

**Q: `actionButtonStyle` が Addon の partial class から参照できない**

A: partial class 内では `private` フィールドを共有できる。`OnGUI` で `actionButtonStyle = YourTheme.ActionButtonStyle;` とキャッシュすることで、同一クラスの他の partial ファイルから参照できる。

**Q: ライトモードに切り替えると一部テキストが黒くなって見えない**

A: 2種類の原因がある。

1. **カスタム GUIStyle の未設定 state**: `normal.textColor` しか設定していないスタイルは、`onNormal` など未設定の state が Unity スキン色を引き継ぐ。`FixAllTextColors(style, color)` で全 state を固定する（詳細: `techniques.md` セクション 4）。

2. **Unity 組み込みコントロールの EditorStyles**: `EditorGUILayout.Toggle`・`ObjectField`・`IntSlider` 等は `EditorStyles.label` などを内部で参照するため、カスタムスタイルの修正が届かない。`PushEditorTheme()` / `PopEditorTheme()` で OnGUI スコープ内だけ EditorStyles を一時上書きする（詳細: `techniques.md` セクション 9）。
