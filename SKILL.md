---
name: dennokoworks-design
description: dennokoworks フローティングデザインシステムを Unity Editor 拡張（EditorWindow / CustomEditor）に適用する
---

# dennokoworks Design Skill

## スキルの目的

このスキルは **dennokoworks カラースキーマ（フローティングデザイン）** を Unity Editor 拡張の IMGUI コードとして実装する。
ユーザーが Unity Editor Window や Inspector の UI を実装・修正するよう求めた場合、このスキルディレクトリのテンプレートとカラー定義を参照してコードを提供する。

---

## 呼び出しパターン

以下のいずれかに当てはまる場合、このスキルを適用する：

- `dennokoworks テーマで実装して`
- `このウィンドウにデザインを適用して`
- `/dennokoworks-design`
- Unity Editor 拡張（EditorWindow / CustomEditor）の新規実装・テーマ適用を求められた場合

---

## 実装の判断フロー

```
ユーザーの要求
    │
    ├─ EditorWindow を作る/改修する
    │       → UniTexTheme + window_structure_template を使用
    │
    ├─ Inspector / CustomEditor を作る/改修する
    │       → UniTexTheme + inspector_structure_template を使用
    │
    └─ テーマクラスだけ欲しい
            → UniTexTheme_template のみ提供
```

---

## このスキルディレクトリのファイル

| ファイル | 用途 |
|---|---|
| `forUnity/UniTexTheme_template.md` | C# テーマクラス全文。最初にプロジェクトに追加するファイル |
| `forUnity/window_structure_template.md` | EditorWindow 骨格。コピーして使う |
| `forUnity/inspector_structure_template.md` | CustomEditor (Inspector) 骨格。コピーして使う |
| `forUnity/techniques.md` | IMGUI 固有の実装テクニック詳細リファレンス |
| `Docs/colors_spec.md` | カラーの役割・意図の詳細説明 |
| `Docs/design_reference.md` | デザインコンセプト（フローティングデザイン）の解説 |
| `colors.json` | カラー実値（#RRGGBB）のマスターデータ |

---

## カラーパレット（クイックリファレンス）

`forUnity/UniTexTheme_template.md` のクラス名を `YourTheme` として記述。

### サーフェス
| 役割 | HEX | 変数 |
|---|---|---|
| アプリ背景 | `#121212` | `YourTheme.Surface0` |
| カード・入力欄背景 | `#1e1e1e` | `YourTheme.Surface1` |
| ツールバー・ホバー背景 | `#2c2c2c` | `YourTheme.Surface2` |
| 境界線・セパレーター | `#3a3a3a` | `YourTheme.Outline` |

### テキスト
| 役割 | HEX | 変数 |
|---|---|---|
| タイトル・強調 | `#ffffff` | `YourTheme.TextPrimary` |
| 本文・ラベル | `#cccccc` | `YourTheme.TextSecondary` |
| 補足・見出し | `#aaaaaa` | `YourTheme.TextTertiary` |
| 無効状態 | `#555555` | `YourTheme.TextDisabled` |

### セマンティック
| 役割 | HEX | 変数 |
|---|---|---|
| エラー | `#9b1b30` | `YourTheme.SemanticError` |
| 警告 | `#ffb74d` | `YourTheme.SemanticWarning` |
| 成功 | `#4caf50` | `YourTheme.SemanticSuccess` |
| 情報 | `#64b5f6` | `YourTheme.SemanticInfo` |

### インタラクション
| 役割 | HEX | 変数 |
|---|---|---|
| アクセント | `#ffffff` | `YourTheme.Accent` |
| ホバーオーバーレイ | `rgba(255,255,255,0.05)` | `YourTheme.HoverOverlay` |

---

## 実装ルール（必ず守ること）

### 1. ライト/ダークモード両対応
OnGUI の先頭で必ず `PushEditorTheme()`、finally で必ず `PopEditorTheme()` を呼ぶ。

```csharp
private void OnGUI()
{
    YourTheme.Initialize();
    YourTheme.PushEditorTheme();
    try
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), YourTheme.Surface0);
        // UI コード
    }
    finally
    {
        YourTheme.PopEditorTheme();
    }
}
```

### 2. GUIStyle は必ず new GUIStyle() から構築する
`new GUIStyle(EditorStyles.boldLabel)` や `new GUIStyle(GUI.skin.button)` の継承は禁止。
未設定の state にライトモードのスキン色が混入し、ライト/ダーク切り替えで見た目が壊れる。

```csharp
// ❌ 禁止
var style = new GUIStyle(GUI.skin.button);

// ✅ 正しい
var style = new GUIStyle();
style.normal.background = texture;
```

### 3. テクスチャのライフサイクル
テクスチャは `if (!_tex) _tex = MakeTex(...)` の Unity null 比較で保護する。
`_initialized` フラグはドメインリロードで自動リセットされるため、ドメインリロード後に再構築が走る。

### 4. ボタンスタイルは stretchWidth を明示する
`GUILayout.Button()` で幅を自動拡張するには `stretchWidth = true` が必要。

---

## EditorWindow の実装手順

1. `forUnity/UniTexTheme_template.md` を `Scripts/Editor/YourTheme.cs` として配置
   - `namespace YourNamespace` をプロジェクトに合わせて変更
   - クラス名 `YourTheme` は任意で変更可
   - `GetStatusStyle` の引数型を自ウィンドウの enum に合わせる

2. `forUnity/window_structure_template.md` を `Scripts/Editor/YourEditorWindow.cs` として配置
   - `[MenuItem("Tools/Your Tool Name")]` のパスを変更
   - `DrawSettingsArea()` にセクションを追加
   - `ApplyAndSave()` / `ResetAll()` を実装

3. OnGUI の構造（上記「実装ルール 1」参照）

---

## CustomEditor (Inspector) の実装手順

1. `forUnity/UniTexTheme_template.md` をプロジェクトに追加（未追加の場合）

2. `forUnity/inspector_structure_template.md` を `Scripts/Editor/YourCustomEditor.cs` として配置
   - `[CustomEditor(typeof(YourComponent))]` の型を変更
   - `DrawSection()` ヘルパーを使って UI を構築

3. Inspector 固有の注意点：
   - `position` プロパティは使用不可（EditorWindow とは異なる）
   - 背景塗りには `InspectorRootStyle` の `overflow` パターンを使用
   - `DrawSection` 内では `EditorGUI.indentLevel` をリセットする

---

## セクションの追加パターン

**常時表示セクション**
```csharp
DrawSection("SECTION TITLE", () =>
{
    // GUILayout コード
});
```

**ON/OFF トグル付きセクション**
```csharp
private bool _showSection = true;

DrawToggleSection("SECTION TITLE", ref _showSection, () =>
{
    // GUILayout コード
}, onReset: () =>
{
    // リセット処理
});
```

---

## よくある問題と対処

| 問題 | 原因 | 対処 |
|---|---|---|
| ライトモードでテキストが黒くなる | GUIStyle 継承 or PushEditorTheme 未呼び出し | `new GUIStyle()` から構築 + Push/Pop を使う |
| ボタンが角丸・グラデーションになる | `GUI.skin.button` 継承 | `new GUIStyle()` から構築 |
| ドメインリロード後にテクスチャ消える | null チェック漏れ | `if (!_tex) _tex = MakeTex(...)` |
| 複数 CustomEditor が衝突する | 型の重複 | `CanEditMultipleObjects` / `[CustomEditor(typeof(X), true)]` を確認 |
| Inspector の背景がはみ出る | overflow 未設定 | `InspectorRootStyle` の `overflow = new RectOffset(20, 20, 0, 0)` を使用 |

詳細は `forUnity/techniques.md` を参照。

---

## デザインコンセプト（参考）

**フローティングデザイン**：
- 全体背景は最も暗い `Surface0 (#121212)`
- コンテンツは `Surface1 (#1e1e1e)` のカードとして浮かび上がって見える
- 表面間のコントラストは低め、テキストは高コントラスト（白系）
- セマンティックカラーはやや彩度を抑えて統一感を保つ
- `Outline (#3a3a3a)` で境界線を描くことで要素の輪郭を示す

詳細は `Docs/design_reference.md` / `Docs/colors_spec.md` を参照。
