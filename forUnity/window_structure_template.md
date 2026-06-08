# Window 構造テンプレート

Unity Editor 拡張ウィンドウの全体骨格。
**このファイルのコードをコピーして作業を開始する**。

## 完成イメージ

このUIが目指すビジュアルは `../example/index.html` をブラウザで開いて確認すること。
`Docs/design_reference.md` にデザインコンセプト、`Docs/colors_spec.md` にカラー仕様がある。

---

## ウィンドウ全体のレイアウト構成

```
┌──────────────────────────────────────┐
│ [ウィンドウタイトル]          [JA][EN] │  ← DrawHeader()
│ ──────────────────────────────────── │  ← セパレーター
│ ┌──────────────────────────────────┐ │
│ │ [PREVIEW]  [☑ Auto Update][Update]│ │  ← DrawPreviewArea()
│ │  チェッカーボード + プレビュー画像  │ │    (ツールバー付きカード)
│ └──────────────────────────────────┘ │
│ ┌──────────────────────────────────┐ │
│ │ SECTION TITLE                    │ │  ← DrawSection()
│ │ ────────────────────────────     │ │
│ │  [コンテンツ]                    │ │
│ └──────────────────────────────────┘ │  ← ↑ ScrollView の中
│ ┌──────────────────────────────────┐ │
│ │ [☑] TOGGLE SECTION     [Reset]   │ │  ← DrawToggleSection()
│ │ ────────────────────────────     │ │
│ │  [スライダーなど]                 │ │
│ └──────────────────────────────────┘ │
│ ┌──────────────────────────────────┐ │
│ │ OUTPUT: (AUTO)       [☐ Overwrite]│ │  ← DrawFooter()
│ │ ────────────────────────────     │ │
│ │ [      Apply & Save (Primary)   ]│ │
│ │ [         Reset All             ]│ │
│ └──────────────────────────────────┘ │
│ [ステータスメッセージ]                │  ← DrawStatusBar()
└──────────────────────────────────────┘
```

---

## 最小動作ウィンドウ スケルトン

```csharp
using UnityEngine;
using UnityEditor;

namespace YourNamespace
{
    public class YourEditorWindow : EditorWindow
    {
        // ─── Status ──────────────────────────────────────────────────────────
        public enum StatusType { Info, Success, Error }
        private string     _statusMessage  = "Ready";
        private StatusType _statusType     = StatusType.Info;
        private double     _statusResetTime = -1.0;

        // Addons や partial class から actionButtonStyle を参照する場合はフィールドとして保持する
        private GUIStyle actionButtonStyle;

        private Vector2 _scrollPosition;

        // ─── Window Registration ─────────────────────────────────────────────
        [MenuItem("Tools/Your Tool Name")]
        public static void ShowWindow()
        {
            var window = GetWindow<YourEditorWindow>("Your Tool Name");
            window.minSize = new Vector2(400, 600);
        }

        // ─── Lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            // 初期化処理（データ読み込みなど）
        }

        private void OnDisable()
        {
            // クリーンアップ処理
        }

        // ─── OnGUI Entry Point ───────────────────────────────────────────────
        private void OnGUI()
        {
            // ステータスの自動リセット（Info 以外を一定時間後に戻す）
            if (_statusResetTime > 0 && EditorApplication.timeSinceStartup > _statusResetTime)
            {
                _statusMessage   = "Ready";
                _statusType      = StatusType.Info;
                _statusResetTime = -1.0;
            }

            // スタイル初期化（初回のみ構築される）
            YourTheme.Initialize();
            YourTheme.PushEditorTheme(); // ライト/ダーク両モードで EditorStyles を上書き
            actionButtonStyle = YourTheme.ActionButtonStyle;

            try
            {
                // ── ウィンドウ背景 (surface.level0) ──────────────────────────────
                EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), YourTheme.Surface0);

                // ── 各エリアの描画 ────────────────────────────────────────────────
                DrawHeader();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawSettingsArea();
                EditorGUILayout.EndScrollView();

                DrawFooter();
                DrawStatusBar();
            }
            finally
            {
                YourTheme.PopEditorTheme(); // 例外でも確実に EditorStyles を復元
            }
        }

        // ─── ヘッダー ──────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Your Tool Name", YourTheme.TitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // 全幅セパレーター
            DrawSeparator();
        }

        // ─── 設定エリア ────────────────────────────────────────────────────
        private void DrawSettingsArea()
        {
            GUILayout.BeginVertical();

            DrawSection("INPUT", () =>
            {
                // ObjectField, Slider などのコンテンツをここに書く
                GUILayout.Label("Source texture field here", YourTheme.SecondaryTextStyle);
            });

            // toggleSection の初期値は bool フィールドとして定義する
            // private bool _showMySection = true;
            DrawToggleSection("MY SECTION", ref _showMySection, () =>
            {
                GUILayout.Label("Toggle section content", YourTheme.SecondaryTextStyle);
            }, onReset: () =>
            {
                // パラメータをデフォルト値に戻す
            });

            GUILayout.EndVertical();
        }
        private bool _showMySection = true; // ← 実際には class フィールドとして上部に定義する

        // ─── フッター ──────────────────────────────────────────────────────
        private void DrawFooter()
        {
            GUILayout.BeginVertical(YourTheme.CardStyle);

            // 出力設定行など
            GUILayout.BeginHorizontal();
            GUILayout.Label("OUTPUT: (AUTO)", YourTheme.CaptionStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawSeparator();

            // Primary action
            if (GUILayout.Button("Apply & Save", YourTheme.ActionButtonStyle))
            {
                ApplyAndSave();
            }

            EditorGUILayout.Space(4);

            // Secondary action
            if (GUILayout.Button("Reset All", YourTheme.SecondaryButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Reset", "Reset all parameters?", "Yes", "No"))
                    ResetAll();
            }

            GUILayout.EndVertical();
        }

        // ─── ステータスバー ────────────────────────────────────────────────
        private void DrawStatusBar()
        {
            GUILayout.Box(_statusMessage, GetStatusStyle(_statusType), GUILayout.ExpandWidth(true));
        }

        private GUIStyle GetStatusStyle(StatusType type)
        {
            return type switch
            {
                StatusType.Success => YourTheme.StatusSuccessStyle,
                StatusType.Error   => YourTheme.StatusErrorStyle,
                _                  => YourTheme.StatusInfoStyle,
            };
        }

        // ─── セクション描画ヘルパー ────────────────────────────────────────

        /// <summary>常時表示の設定セクション。</summary>
        private void DrawSection(string title, System.Action content)
        {
            GUILayout.BeginVertical(YourTheme.CardStyle);
            GUILayout.Label(title, YourTheme.SectionHeaderStyle);
            DrawSeparator();
            content?.Invoke();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// ON/OFF トグル付きセクション。
        /// OFF 時もコンテンツは表示されグレーアウトされる（設定値が保持されていることを示す）。
        /// </summary>
        private void DrawToggleSection(string title, ref bool toggle, System.Action content, System.Action onReset = null)
        {
            GUILayout.BeginVertical(YourTheme.CardStyle);

            GUILayout.BeginHorizontal();

            var headerStyle = toggle ? YourTheme.ToggleSectionOnStyle : YourTheme.ToggleSectionOffStyle;

            EditorGUI.BeginChangeCheck();
            bool newToggle = EditorGUILayout.ToggleLeft(title, toggle, headerStyle, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                toggle = newToggle;
                Repaint(); // 必要に応じてプレビュー更新を呼ぶ
            }

            if (onReset != null)
            {
                if (GUILayout.Button("Reset", YourTheme.MiniButtonStyle, GUILayout.Width(50)))
                {
                    onReset.Invoke();
                    GUI.FocusControl(null);
                }
            }

            GUILayout.EndHorizontal();

            DrawSeparator();

            using (new EditorGUI.DisabledGroupScope(!toggle))
            {
                content?.Invoke();
            }

            GUILayout.EndVertical();
        }

        /// <summary>Outline 色の 1px 横区切り線。</summary>
        private void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, YourTheme.Outline);
            EditorGUILayout.Space(4);
        }

        // ─── ビジネスロジック (スタブ) ─────────────────────────────────────
        private void ApplyAndSave()
        {
            // 処理後にステータスを更新する例
            SetStatus("Saved successfully.", StatusType.Success);
        }

        private void ResetAll()
        {
            // パラメータをリセット
        }

        /// <summary>ステータスバーにメッセージを表示し、一定時間後に "Ready" へ戻す。</summary>
        private void SetStatus(string message, StatusType type, double autoResetSeconds = 3.0)
        {
            _statusMessage   = message;
            _statusType      = type;
            _statusResetTime = type == StatusType.Info
                ? -1.0
                : EditorApplication.timeSinceStartup + autoResetSeconds;
            Repaint();
        }
    }
}
```

---

## ツールバー付きカード（プレビューエリアパターン）

プレビューエリアのようにツールバーをカード端まで伸ばしたい場合は `CardOuterStyle` を使う。

```csharp
private void DrawPreviewArea()
{
    // padding=0 の外枠でツールバーが端まで伸びる
    GUILayout.BeginVertical(YourTheme.CardOuterStyle);

    // ツールバー行 (Surface2 背景)
    GUILayout.BeginHorizontal(YourTheme.ToolbarStyle);
    GUILayout.Label("PREVIEW", YourTheme.SectionHeaderStyle);
    GUILayout.FlexibleSpace();
    if (GUILayout.Button("Update", YourTheme.ToolbarButtonStyle))
        UpdatePreview();
    GUILayout.EndHorizontal();

    // コンテンツ（手動 4px パディング）
    float contentHeight = 200f;
    Rect container = GUILayoutUtility.GetRect(0, contentHeight + 8, GUILayout.ExpandWidth(true));
    Rect inner = new Rect(container.x + 4, container.y + 4, container.width - 8, contentHeight);

    // inner 矩形にプレビュー画像などを描画する
    EditorGUI.DrawRect(inner, YourTheme.Surface0); // 仮の背景

    GUILayout.EndVertical();
}
```

---

## ファイル参照マップ

このリポジトリだけで実装するために必要なファイルの読み順：

```
1. ../example/index.html          ← ビジュアルターゲット（ブラウザで開く）
2. ../Docs/design_reference.md    ← デザインコンセプト
3. ../Docs/colors_spec.md         ← カラー仕様（役割の定義）
4. ../colors.json                 ← カラーの実値（#RRGGBB）
5. forUnity/UniTexTheme_template.md ← C# テーマクラス（コピー元）
6. forUnity/techniques.md         ← IMGUI 固有の実装テクニック
7. forUnity/window_structure_template.md ← このファイル（ウィンドウ骨格）
```
