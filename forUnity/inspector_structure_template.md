# インスペクター (CustomEditor) 構造テンプレート

dennoko.dev カラースキーマを **CustomEditor (Inspector)** に適用するためのガイドとテンプレート。
`window_structure_template.md`（EditorWindow 向け）と対になるドキュメント。

---

## EditorWindow との根本的な違い

| 項目 | EditorWindow | CustomEditor (Inspector) |
|---|---|---|
| エントリーポイント | `OnGUI()` | `OnInspectorGUI()` |
| 背景の塗り方 | `EditorGUI.DrawRect(new Rect(0,0,position.width,position.height), ...)` | `GUILayout.BeginVertical(InspectorRootStyle)` + `overflow` 設定 |
| `position` プロパティ | 使える | **使えない** |
| ウィンドウ幅 | `position.width` | `EditorGUIUtility.currentViewWidth` |
| ボタン推奨サイズ | `fixedHeight 34 / 26` | **`fixedHeight 30 / 24`**（インスペクターは幅が狭い） |
| スクロールビュー | 手動で `BeginScrollView` | Unity が自動管理 |
| `serializedObject` | なし | `serializedObject.Update()` / `ApplyModifiedProperties()` を使う |

---

## 背景塗りのパターン — InspectorRootStyle

EditorWindow では `position.width/height` を使って `EditorGUI.DrawRect` でウィンドウ全面を塗れる。
インスペクターでは同等の手段がないため、**`overflow` プロパティを設定した GUIStyle** でラップする。
（ネガティブマージンのみでは Unity バージョンによって隙間が生じたり、手動描画（DrawRect）を併用するとスクロール時にガタつきが発生したりするため、`overflow` が推奨される）

### テーマクラスへの組み込み（既定）

`InspectorRootStyle` は `UniTexTheme_template.md` のテーマクラスに既に含まれている。
追加実装は不要。参考として定義内容を示す。

```csharp
// UniTexTheme の BuildStyles() 内で定義済み
InspectorRootStyle = new GUIStyle();
InspectorRootStyle.normal.background = _texSurface0;   // Surface0 で全体を塗る
InspectorRootStyle.margin   = new RectOffset(0, 0, 0, 0);
InspectorRootStyle.padding  = new RectOffset(10, 10, 8, 8);
InspectorRootStyle.overflow = new RectOffset(20, 20, 0, 0); // 背景描画領域だけを左右に広げる
```

### OnInspectorGUI での使い方

```csharp
public override void OnInspectorGUI()
{
    YourTheme.Initialize();
    YourTheme.PushEditorTheme(); // ライト/ダーク両モードで EditorStyles を上書き
    serializedObject.Update();

    try
    {
        // ▼ ここでラップ開始 — Surface0 がインスペクター全面に塗られる
        GUILayout.BeginVertical(YourTheme.InspectorRootStyle);

        // ... セクション描画 ...

        GUILayout.EndVertical(); // ▲ ラップ終了
    }
    finally
    {
        YourTheme.PopEditorTheme(); // 例外でも確実に EditorStyles を復元
    }
}
```

> **注意:** `BeginVertical` / `EndVertical` が必ず対になるようにする。
> 例外が飛ぶ可能性があるコードは `try/finally` でガードするとより堅牢。

---

## インスペクター向けボタンサイズの調整

EditorWindow 用テーマクラスをそのまま流用すると、ボタンが大きすぎる場合がある。
インスペクター専用テーマクラスを作るか、Inspector 用スタイルを別プロパティとして追加する。

```csharp
// Inspector 用に fixedHeight を小さくした派生スタイル
// BuildStyles() 内、ActionButtonStyle / SecondaryButtonStyle の直後に追加する

ActionButtonStyle.fixedHeight  = 30; // ウィンドウ用は 34
SecondaryButtonStyle.fixedHeight = 24; // ウィンドウ用は 26
```

---

## DrawSection ヘルパー

インスペクターのセクション構造（見出し + セパレーター + コンテンツ）を統一する共通ヘルパー。

```csharp
private void DrawSection(string title, System.Action content)
{
    GUILayout.BeginVertical(YourTheme.CardStyle);
    GUILayout.Label(title, YourTheme.SectionHeaderStyle);

    // 1px セパレーター
    var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
    EditorGUI.DrawRect(rect, YourTheme.Outline);
    EditorGUILayout.Space(4);

    content?.Invoke();
    GUILayout.EndVertical();
}
```

### 使い方

```csharp
DrawSection("GENERATOR SETTINGS", () =>
{
    EditorGUILayout.PropertyField(myProp, new GUIContent("表示名"));

    GUILayout.Space(8);

    if (GUILayout.Button("実行", YourTheme.ActionButtonStyle))
        DoSomething();
});
```

---

## インスペクター向けカードの margin 設定

インスペクターでは左右に Unity デフォルトの余白があるため、
`CardStyle.margin` の左右は `0` にしておくと幅が自然に揃う。

```csharp
CardStyle.margin = new RectOffset(0, 0, 0, 12); // 左右 0、下 12px でカード間を分離
```

> EditorWindow 用の `margin = new RectOffset(4, 4, 6, 6)` とは異なる点に注意。

---

## インスペクター全体の骨格 スケルトン

```csharp
using UnityEditor;
using UnityEngine;

namespace YourNamespace
{
    [CustomEditor(typeof(YourComponent))]
    public class YourComponentInspector : Editor
    {
        // ─── Serialized Properties ───────────────────────────────────────────
        SerializedProperty myProp;

        void OnEnable()
        {
            myProp = serializedObject.FindProperty("myField");
        }

        // ─── Inspector GUI ───────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            YourTheme.Initialize();
            YourTheme.PushEditorTheme(); // ライト/ダーク両モードで EditorStyles を上書き
            serializedObject.Update();
            var target = (YourComponent)this.target;

            try
            {
                // Surface0 でインスペクター全体を塗り、カード (Surface1) が浮かぶ構造にする
                GUILayout.BeginVertical(YourTheme.InspectorRootStyle);

                // ---- SECTION A ----
                DrawSection("SECTION A", () =>
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(myProp, new GUIContent("マイフィールド"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        target.OnValidate(); // 必要に応じて
                    }

                    GUILayout.Space(8);

                    if (GUILayout.Button("メインアクション", YourTheme.ActionButtonStyle))
                    {
                        Undo.RecordObject(target, "Do Action");
                        target.DoSomething();
                        EditorUtility.SetDirty(target);
                    }
                });

                // ---- SECTION B ----
                DrawSection("SECTION B", () =>
                {
                    if (GUILayout.Button("サブアクション", YourTheme.SecondaryButtonStyle))
                        target.DoSubAction();
                });

                GUILayout.EndVertical(); // InspectorRootStyle
            }
            finally
            {
                YourTheme.PopEditorTheme(); // 例外でも確実に EditorStyles を復元
            }
        }

        // ─── ヘルパー ────────────────────────────────────────────────────────
        private void DrawSection(string title, System.Action content)
        {
            GUILayout.BeginVertical(YourTheme.CardStyle);
            GUILayout.Label(title, YourTheme.SectionHeaderStyle);

            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, YourTheme.Outline);
            EditorGUILayout.Space(4);

            content?.Invoke();
            GUILayout.EndVertical();
        }
    }
}
```

---

## 複数の CustomEditor が同一型に存在する場合の問題

**`[CustomEditor(typeof(T))]` を持つクラスが複数あると Unity はどちらか一方しか使わない。**
新旧ファイルを並存させると、意図しない方が使われ続ける原因になる。

対処方針:
- 旧ファイルを **必ず削除**する（`.cs` と `.meta` の両方）
- `.meta` を残すと Unity がゴーストとして認識し続けることがある

```bash
# 削除コマンド例（bash / Git Bash）
rm Assets/path/to/OldEditor.cs
rm Assets/path/to/OldEditor.cs.meta
```

---

## よくある問題

### Q: カードの背景が暗すぎて、インスペクターのデフォルト背景に馴染まない

`InspectorRootStyle` でインスペクター全体を `Surface0` で塗っていない場合に起きる。
Unity のデフォルト背景（ライトテーマなら白、ダークテーマならグレー）の上に
`Surface1`（#1e1e1e）のカードが乗ると、周囲との明度差が大きくなりすぎる。

→ `GUILayout.BeginVertical(YourTheme.InspectorRootStyle)` でラップする。

### Q: `InspectorRootStyle` のラップがインスペクター端まで届かず隙間が生じる

`margin = new RectOffset(-4, -4, -4, -4)` のような負のマージン調整だけでは、Unity の環境や DPI によって限界がある。

→ **`overflow` プロパティ** を使用する。
`InspectorRootStyle.overflow = new RectOffset(20, 20, 0, 0)` のように設定することで、レイアウトを崩さずに背景の描画領域だけを外側へ広げ、確実に端まで塗りつぶすことができる。

### Q: `serializedObject.ApplyModifiedProperties()` を呼ばずに `EndVertical` を先に呼んでしまった

`BeginVertical` / `EndVertical` のペアが崩れていると IMGUI のレイアウトグループスタックが
壊れ、以降のフレームで描画が乱れる。`DrawSection` ヘルパーの中に閉じ込めることで
コンテンツ内の例外でも `EndVertical` が漏れにくくなる。

### Q: ドメインリロード後にスタイルが壊れる

→ `techniques.md` の「3. テクスチャのライフサイクル管理」を参照。
`_initialized` static フラグはドメインリロードで `false` にリセットされるため、
`Initialize()` を `OnInspectorGUI` の先頭で毎フレーム呼んでいれば自動的に再構築される。

### Q: カスタムボタンスタイルが古めかしい角丸ボタンになったり、意図しないグラデーションやシャドウがかかる

フラットでスクエアなデザイン（dennoko.dev スタイル）のボタンを作りたい場合、`new GUIStyle(GUI.skin.button)` のように **標準のボタンスタイルを継承してはいけません。**

Unity の標準ボタンスタイルには、あらかじめ `scaledBackgrounds` やネイティブの角丸、ドロップシャドウの描画設定が組み込まれているため、単純に `normal.background` にフラットなテクスチャを上書きしただけでは元の装飾と混ざって意図しない見た目になります。

→ **対処法:** `GUI.skin.button` を継承せず、まっさらな `new GUIStyle()` からスタイルを構築します。その際、継承をやめることで失われる余白（`margin` や `padding`）を再設定する必要があります。

```csharp
// ❌ 悪い例: Unity の標準装飾が混ざり、角丸やグラデーションが残ってしまう
ActionButtonStyle = new GUIStyle(GUI.skin.button);
ActionButtonStyle.normal.background = _texAccentCard;

// 🟢 良い例: まっさらな状態からフラットなスタイルを構築する
ActionButtonStyle = new GUIStyle();
ActionButtonStyle.normal.background = _texAccentCard;
ActionButtonStyle.border  = new RectOffset(1, 1, 1, 1);
ActionButtonStyle.margin  = new RectOffset(4, 4, 2, 2);  // 継承をやめたので明示的に指定する
ActionButtonStyle.padding = new RectOffset(2, 2, 2, 2);  // 継承をやめたので明示的に指定する
```

---

## ファイル参照マップ

```
1. ../example/index.html                  ← ビジュアルターゲット（ブラウザで開く）
2. ../Docs/design_reference.md            ← デザインコンセプト
3. ../Docs/colors_spec.md                 ← カラー仕様
4. ../colors.json                         ← カラー実値 (#RRGGBB)
5. forUnity/UniTexTheme_template.md       ← テーマクラス C# テンプレート
6. forUnity/techniques.md                 ← IMGUI 固有の実装テクニック
7. forUnity/window_structure_template.md  ← EditorWindow 向け骨格
8. forUnity/inspector_structure_template.md ← このファイル（Inspector 向け骨格）
```
