# UniTexTheme.cs — コピー用テンプレート

他の Unity Editor 拡張に流用する場合は、以下の手順で使用する。

1. `namespace YourNamespace` の部分をプロジェクトに合わせて変更する
2. `GetStatusStyle` の引数型 `YourWindow.StatusType` をウィンドウクラスに合わせる
   （または汎用 enum に変更する）
3. `Scripts/Editor/YourTheme.cs` として配置する

---

```csharp
using UnityEngine;
using UnityEditor;

namespace YourNamespace   // ← 変更する
{
    /// <summary>
    /// dennoko.dev カラースキーマに基づくテーマ定義。
    /// colors_spec.md / design_reference.md の仕様を Unity IMGUI に変換する。
    /// OnGUI の先頭で Initialize() を呼び出すことで、スタイルを遅延初期化する。
    /// </summary>
    internal static class YourTheme   // ← クラス名を変更してもよい
    {
        // ─── Colors ──────────────────────────────────────────────────────────

        // theme.surface (Neutral Layer)
        public static readonly Color Surface0 = Hex(0x121212); // app background
        public static readonly Color Surface1 = Hex(0x1e1e1e); // cards, inputs
        public static readonly Color Surface2 = Hex(0x2c2c2c); // hover, toolbar

        // theme.outline
        public static readonly Color Outline = Hex(0x3a3a3a);

        // theme.typography
        public static readonly Color TextPrimary   = Hex(0xffffff);
        public static readonly Color TextSecondary = Hex(0xcccccc);
        public static readonly Color TextTertiary  = Hex(0xaaaaaa);
        public static readonly Color TextDisabled  = Hex(0x555555);

        // theme.semantic
        public static readonly Color SemanticError   = Hex(0x9b1b30);
        public static readonly Color SemanticWarning = Hex(0xffb74d);
        public static readonly Color SemanticSuccess = Hex(0x4caf50);
        public static readonly Color SemanticInfo    = Hex(0x64b5f6);

        // theme.interaction
        public static readonly Color Accent       = Color.white;
        public static readonly Color HoverOverlay = new Color(1f, 1f, 1f, 0.05f);

        // ─── Cached Textures ─────────────────────────────────────────────────

        private static Texture2D _texSurface0;
        private static Texture2D _texSurface1;
        private static Texture2D _texSurface2;
        private static Texture2D _texCard;        // Surface1 fill + Outline border (3x3)
        private static Texture2D _texAccentCard;  // Surface2 fill + Outline border (3x3)

        // ─── Styles ──────────────────────────────────────────────────────────

        private static bool _initialized;
        private static bool _lastIsProSkin;
        private static Texture2D _texSearchField; // Input fields background (3x3 bordered)

        // Layout / Container
        public static GUIStyle CardStyle      { get; private set; } // sections (padding あり)
        public static GUIStyle CardOuterStyle { get; private set; } // ツールバー付き外枠 (padding なし)
        public static GUIStyle ToolbarStyle   { get; private set; } // ツールバー行

        // Typography
        public static GUIStyle TitleStyle            { get; private set; } // ウィンドウタイトル
        public static GUIStyle SectionHeaderStyle    { get; private set; } // 非トグルセクション見出し
        public static GUIStyle ToggleSectionOnStyle  { get; private set; } // トグル ON 時の見出し
        public static GUIStyle ToggleSectionOffStyle { get; private set; } // トグル OFF 時の見出し
        public static GUIStyle SecondaryTextStyle    { get; private set; } // 説明文
        public static GUIStyle CaptionStyle          { get; private set; } // 補足・メタデータ

        // Buttons
        public static GUIStyle ActionButtonStyle     { get; private set; } // Primary Action
        public static GUIStyle SecondaryButtonStyle  { get; private set; } // Secondary Action
        public static GUIStyle MiniButtonStyle       { get; private set; }
        public static GUIStyle MiniButtonLeftStyle   { get; private set; }
        public static GUIStyle MiniButtonRightStyle  { get; private set; }

        // Inspector / Toolbar
        public static GUIStyle InspectorRootStyle    { get; private set; }
        public static GUIStyle ToolbarButtonStyle    { get; private set; }

        // Status bar
        public static GUIStyle StatusInfoStyle    { get; private set; }
        public static GUIStyle StatusSuccessStyle { get; private set; }
        public static GUIStyle StatusErrorStyle   { get; private set; }

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>OnGUI の先頭で呼び出す。初回のみスタイルを構築する。</summary>
        public static void Initialize()
        {
            bool currentProSkin = EditorGUIUtility.isProSkin;
            if (_initialized && _lastIsProSkin != currentProSkin)
            {
                DisposeTextures();
            }
            _lastIsProSkin = currentProSkin;

            if (_initialized) return;
            _initialized = true;
            EnsureTextures();
            BuildStyles();
        }

        private static void EnsureTextures()
        {
            if (!_texSurface0)   _texSurface0   = MakeTex(Surface0);
            if (!_texSurface1)   _texSurface1   = MakeTex(Surface1);
            if (!_texSurface2)   _texSurface2   = MakeTex(Surface2);
            if (!_texCard)       _texCard       = MakeBorderedTex(Surface1, Outline);
            if (!_texAccentCard) _texAccentCard = MakeBorderedTex(Surface2, Outline);
            if (!_texSearchField) _texSearchField = MakeBorderedTex(Surface2, Hex(0x5a5a5a));
        }

        private static void BuildStyles()
        {
            // ── Container ────────────────────────────────────────────────────

            CardStyle = new GUIStyle();
            CardStyle.normal.background = _texCard;
            CardStyle.border  = new RectOffset(1, 1, 1, 1);
            CardStyle.padding = new RectOffset(10, 10, 8, 8);
            CardStyle.margin  = new RectOffset(8, 8, 8, 8);

            CardOuterStyle = new GUIStyle();
            CardOuterStyle.normal.background = _texCard;
            CardOuterStyle.border  = new RectOffset(1, 1, 1, 1);
            CardOuterStyle.padding = new RectOffset(0, 0, 0, 0);
            CardOuterStyle.margin  = new RectOffset(8, 8, 8, 8);

            ToolbarStyle = new GUIStyle();
            ToolbarStyle.normal.background = _texSurface2;
            ToolbarStyle.padding = new RectOffset(6, 6, 4, 4);
            ToolbarStyle.margin  = new RectOffset(0, 0, 0, 0);

            // ── Typography ───────────────────────────────────────────────────
            // new GUIStyle() から構築してテーマ非依存とする。
            // EditorStyles.* を継承すると未設定の state にライトモード色が混入するため使用しない。

            TitleStyle = new GUIStyle();
            TitleStyle.fontStyle = FontStyle.Bold;
            TitleStyle.fontSize  = 14;
            TitleStyle.alignment = TextAnchor.MiddleLeft;
            FixAllTextColors(TitleStyle, TextPrimary);

            SectionHeaderStyle = new GUIStyle();
            SectionHeaderStyle.fontStyle = FontStyle.Bold;
            SectionHeaderStyle.fontSize  = 10;
            SectionHeaderStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(SectionHeaderStyle, TextTertiary);

            ToggleSectionOnStyle = new GUIStyle();
            ToggleSectionOnStyle.fontStyle = FontStyle.Bold;
            ToggleSectionOnStyle.fontSize  = 10;
            ToggleSectionOnStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(ToggleSectionOnStyle, TextPrimary);

            ToggleSectionOffStyle = new GUIStyle();
            ToggleSectionOffStyle.fontStyle = FontStyle.Bold;
            ToggleSectionOffStyle.fontSize  = 10;
            ToggleSectionOffStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(ToggleSectionOffStyle, TextTertiary);

            SecondaryTextStyle = new GUIStyle();
            SecondaryTextStyle.wordWrap = true;
            FixAllTextColors(SecondaryTextStyle, TextSecondary);

            CaptionStyle = new GUIStyle();
            CaptionStyle.fontSize = 9;
            FixAllTextColors(CaptionStyle, TextTertiary);

            // ── Toolbar Button ────────────────────────────────────────────────

            ToolbarButtonStyle = new GUIStyle();
            ToolbarButtonStyle.normal.background   = null;
            ToolbarButtonStyle.hover.background    = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            ToolbarButtonStyle.active.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            ToolbarButtonStyle.border    = new RectOffset(0, 0, 0, 0);
            ToolbarButtonStyle.margin    = new RectOffset(1, 1, 1, 1);
            ToolbarButtonStyle.padding   = new RectOffset(6, 6, 2, 2);
            ToolbarButtonStyle.fontSize  = 10;
            ToolbarButtonStyle.alignment = TextAnchor.MiddleCenter;
            ToolbarButtonStyle.normal.textColor    = TextTertiary;
            ToolbarButtonStyle.hover.textColor     = TextSecondary;
            ToolbarButtonStyle.active.textColor    = TextPrimary;
            ToolbarButtonStyle.focused.textColor   = TextTertiary;
            ToolbarButtonStyle.onNormal.textColor  = TextPrimary;
            ToolbarButtonStyle.onHover.textColor   = TextPrimary;
            ToolbarButtonStyle.onActive.textColor  = TextPrimary;
            ToolbarButtonStyle.onFocused.textColor = TextPrimary;

            // ── Inspector Root ────────────────────────────────────────────────

            InspectorRootStyle = new GUIStyle();
            InspectorRootStyle.normal.background = _texSurface0;
            InspectorRootStyle.margin   = new RectOffset(0, 0, 0, 0);
            InspectorRootStyle.padding  = new RectOffset(10, 10, 8, 8);
            InspectorRootStyle.overflow = new RectOffset(20, 20, 0, 0);

            // ── Buttons ──────────────────────────────────────────────────────

            // GUI.skin.button / EditorStyles.miniButton* を継承すると Unity の角丸・グラデーション・
            // scaledBackgrounds が引き継がれてフラットなテクスチャと混ざる。
            // そのため new GUIStyle() から全プロパティを明示的に構築する。

            ActionButtonStyle = new GUIStyle();
            ActionButtonStyle.normal.background  = _texAccentCard;
            ActionButtonStyle.hover.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.07f));
            ActionButtonStyle.active.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.15f));
            ActionButtonStyle.border       = new RectOffset(1, 1, 1, 1);
            ActionButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
            ActionButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
            ActionButtonStyle.fontSize     = 13;
            ActionButtonStyle.fontStyle    = FontStyle.Bold;
            ActionButtonStyle.fixedHeight  = 34;
            ActionButtonStyle.alignment    = TextAnchor.MiddleCenter;
            ActionButtonStyle.stretchWidth = true;
            FixAllTextColors(ActionButtonStyle, TextPrimary);

            SecondaryButtonStyle = new GUIStyle();
            SecondaryButtonStyle.normal.background = MakeBorderedTex(Surface1, Outline);
            SecondaryButtonStyle.hover.background  = _texAccentCard;
            SecondaryButtonStyle.active.background = MakeTex(Color.Lerp(Surface1, Color.white, 0.10f));
            SecondaryButtonStyle.border       = new RectOffset(1, 1, 1, 1);
            SecondaryButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
            SecondaryButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
            SecondaryButtonStyle.fontSize     = 11;
            SecondaryButtonStyle.fixedHeight  = 26;
            SecondaryButtonStyle.alignment    = TextAnchor.MiddleCenter;
            SecondaryButtonStyle.stretchWidth = true;
            SecondaryButtonStyle.normal.textColor   = TextSecondary;
            SecondaryButtonStyle.hover.textColor    = TextPrimary;
            SecondaryButtonStyle.active.textColor   = TextPrimary;
            SecondaryButtonStyle.focused.textColor  = TextSecondary;
            SecondaryButtonStyle.onNormal.textColor  = TextSecondary;
            SecondaryButtonStyle.onHover.textColor   = TextPrimary;
            SecondaryButtonStyle.onActive.textColor  = TextPrimary;
            SecondaryButtonStyle.onFocused.textColor = TextSecondary;

            MiniButtonStyle = new GUIStyle();
            MiniButtonStyle.normal.background = _texAccentCard;
            MiniButtonStyle.normal.textColor  = TextTertiary;
            MiniButtonStyle.hover.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            MiniButtonStyle.hover.textColor   = TextSecondary;
            MiniButtonStyle.active.background = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            MiniButtonStyle.active.textColor  = TextPrimary;
            MiniButtonStyle.border      = new RectOffset(1, 1, 1, 1);
            MiniButtonStyle.margin      = new RectOffset(2, 2, 1, 1);
            MiniButtonStyle.padding     = new RectOffset(4, 4, 1, 2);
            MiniButtonStyle.fontSize    = 10;
            MiniButtonStyle.fixedHeight = 16;
            MiniButtonStyle.alignment   = TextAnchor.MiddleCenter;
            MiniButtonStyle.focused.textColor = TextTertiary;
            MiniButtonStyle.onNormal.textColor  = TextPrimary;
            MiniButtonStyle.onHover.textColor   = TextPrimary;
            MiniButtonStyle.onActive.textColor  = TextPrimary;
            MiniButtonStyle.onFocused.textColor = TextPrimary;

            MiniButtonLeftStyle = new GUIStyle();
            MiniButtonLeftStyle.normal.background = _texAccentCard;
            MiniButtonLeftStyle.normal.textColor  = TextTertiary;
            MiniButtonLeftStyle.hover.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            MiniButtonLeftStyle.hover.textColor   = TextSecondary;
            MiniButtonLeftStyle.active.background = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            MiniButtonLeftStyle.active.textColor  = TextPrimary;
            MiniButtonLeftStyle.border      = new RectOffset(1, 1, 1, 1);
            MiniButtonLeftStyle.margin      = new RectOffset(2, 2, 1, 1);
            MiniButtonLeftStyle.padding     = new RectOffset(4, 4, 1, 2);
            MiniButtonLeftStyle.fontSize    = 10;
            MiniButtonLeftStyle.fixedHeight = 16;
            MiniButtonLeftStyle.alignment   = TextAnchor.MiddleCenter;
            MiniButtonLeftStyle.focused.textColor = TextTertiary;
            MiniButtonLeftStyle.onNormal.textColor  = TextPrimary;
            MiniButtonLeftStyle.onHover.textColor   = TextPrimary;
            MiniButtonLeftStyle.onActive.textColor  = TextPrimary;
            MiniButtonLeftStyle.onFocused.textColor = TextPrimary;

            MiniButtonRightStyle = new GUIStyle();
            MiniButtonRightStyle.normal.background = _texAccentCard;
            MiniButtonRightStyle.normal.textColor  = TextTertiary;
            MiniButtonRightStyle.hover.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            MiniButtonRightStyle.hover.textColor   = TextSecondary;
            MiniButtonRightStyle.active.background = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            MiniButtonRightStyle.active.textColor  = TextPrimary;
            MiniButtonRightStyle.border      = new RectOffset(1, 1, 1, 1);
            MiniButtonRightStyle.margin      = new RectOffset(2, 2, 1, 1);
            MiniButtonRightStyle.padding     = new RectOffset(4, 4, 1, 2);
            MiniButtonRightStyle.fontSize    = 10;
            MiniButtonRightStyle.fixedHeight = 16;
            MiniButtonRightStyle.alignment   = TextAnchor.MiddleCenter;
            MiniButtonRightStyle.focused.textColor = TextTertiary;
            MiniButtonRightStyle.onNormal.textColor  = TextPrimary;
            MiniButtonRightStyle.onHover.textColor   = TextPrimary;
            MiniButtonRightStyle.onActive.textColor  = TextPrimary;
            MiniButtonRightStyle.onFocused.textColor = TextPrimary;

            // ── Status Bar ───────────────────────────────────────────────────

            var statusBase = new GUIStyle();
            statusBase.border    = new RectOffset(1, 1, 1, 1);
            statusBase.padding   = new RectOffset(8, 8, 5, 5);
            statusBase.margin    = new RectOffset(4, 4, 2, 2);
            statusBase.fontSize  = 11;
            statusBase.wordWrap  = true;
            statusBase.alignment = TextAnchor.MiddleLeft;

            StatusInfoStyle = new GUIStyle(statusBase);
            StatusInfoStyle.normal.background = _texSurface1;
            FixAllTextColors(StatusInfoStyle, TextSecondary);

            StatusSuccessStyle = new GUIStyle(statusBase);
            StatusSuccessStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticSuccess, 0.3f));
            FixAllTextColors(StatusSuccessStyle, SemanticSuccess);

            StatusErrorStyle = new GUIStyle(statusBase);
            StatusErrorStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticError, 0.5f));
            FixAllTextColors(StatusErrorStyle, new Color(1f, 0.65f, 0.65f));
        }

        // ─── Status Style Selector ───────────────────────────────────────────

        // NOTE: 引数の StatusType は呼び出し元ウィンドウの enum に合わせる。
        // 汎用化する場合は string や int に変更してもよい。
        public static GUIStyle GetStatusStyle(int statusLevel)
        {
            return statusLevel switch
            {
                1 => StatusSuccessStyle, // success
                2 => StatusErrorStyle,   // error
                _ => StatusInfoStyle,    // info / default
            };
        }

        // ─── Editor Style Override (Light Mode Fix) ──────────────────────────

        private static bool _overrideActive;
        public static bool IsOverrideActive => _overrideActive;

        private static Color _backupCursorColor;
        private static Color _backupSelectionColor;
        private static bool _settingsBackupActive;

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

        /// <summary>
        /// OnGUI 先頭で Initialize() の直後に呼ぶ。
        /// ライト/ダーク両モードで EditorStyles をテーマ定義色に一時上書きする。
        /// PopEditorTheme を finally ブロックで必ず呼ぶこと。
        /// </summary>
        public static void PushEditorTheme()
        {
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
                    new GUIStyleBackup(EditorStyles.toggle),
                    new GUIStyleBackup(GUI.skin.textField),
                    new GUIStyleBackup(GUI.skin.label)
                };
            }

            if (!_settingsBackupActive)
            {
                _backupCursorColor = GUI.skin.settings.cursorColor;
                _backupSelectionColor = GUI.skin.settings.selectionColor;
                _settingsBackupActive = true;
            }

            // ─ テキスト色を固定
            //   無効化されていないパラメーター/表記 (入力欄・ラベル・ポップアップ・トグル) は
            //   完全な白 (TextPrimary) にして視認性を最大化する。無効時は DisabledScope が
            //   自動的に減光するため、ここでは常に TextPrimary を指定する。
            FixAllTextColors(EditorStyles.label, TextPrimary);
            FixAllTextColors(EditorStyles.objectField, TextPrimary);
            FixAllTextColors(EditorStyles.numberField, TextPrimary);
            FixAllTextColors(EditorStyles.textField,   TextPrimary);
            FixAllTextColors(EditorStyles.popup,       TextPrimary);
            FixAllTextColors(EditorStyles.toggle,      TextPrimary);
            FixAllTextColors(GUI.skin.textField,       TextPrimary);
            FixAllTextColors(GUI.skin.label,           TextPrimary);

            // ─ 背景テクスチャをすべての状態でダーク色＋ボーダーに固定
            FixAllStateBackgrounds(EditorStyles.objectField, _texSearchField);
            EditorStyles.objectField.border = new RectOffset(1, 1, 1, 1);

            FixAllStateBackgrounds(EditorStyles.numberField, _texSearchField);
            EditorStyles.numberField.border = new RectOffset(1, 1, 1, 1);

            FixAllStateBackgrounds(EditorStyles.textField,   _texSearchField);
            EditorStyles.textField.border = new RectOffset(1, 1, 1, 1);
            EditorStyles.textField.padding = new RectOffset(6, 6, 3, 3);

            FixAllStateBackgrounds(GUI.skin.textField,       _texSearchField);
            GUI.skin.textField.border = new RectOffset(1, 1, 1, 1);
            GUI.skin.textField.padding = new RectOffset(6, 6, 3, 3);

            // ── カーソルと選択範囲の色を固定 (ライトモードの黒カーソル等を防止)
            GUI.skin.settings.cursorColor = TextPrimary;
            GUI.skin.settings.selectionColor = new Color(1f, 1f, 1f, 0.25f);

            // ポップアップは枠線付きカードテクスチャを使用し、9スライス境界を1pxに設定して引き伸ばし縞ノイズを解消
            FixAllStateBackgrounds(EditorStyles.popup, _texCard);
            EditorStyles.popup.border = new RectOffset(1, 1, 1, 1);
            EditorStyles.popup.padding = new RectOffset(6, 18, 4, 4);
        }

        /// <summary>OnGUI 末尾の finally ブロックで必ず呼ぶ。EditorStyles を元に戻す。</summary>
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

            if (_settingsBackupActive)
            {
                GUI.skin.settings.cursorColor = _backupCursorColor;
                GUI.skin.settings.selectionColor = _backupSelectionColor;
                _settingsBackupActive = false;
            }
        }

        /// <summary>テクスチャと状態を明示破棄する（テーマ切り替えやドメインリロード時に安全にクリーンアップするため）。</summary>
        internal static void DisposeTextures()
        {
            PopEditorTheme();

            if (_texSurface0) Object.DestroyImmediate(_texSurface0);
            if (_texSurface1) Object.DestroyImmediate(_texSurface1);
            if (_texSurface2) Object.DestroyImmediate(_texSurface2);
            if (_texCard)     Object.DestroyImmediate(_texCard);
            if (_texAccentCard) Object.DestroyImmediate(_texAccentCard);
            if (_texSearchField) Object.DestroyImmediate(_texSearchField);

            _texSurface0   = null;
            _texSurface1   = null;
            _texSurface2   = null;
            _texCard       = null;
            _texAccentCard = null;
            _texSearchField = null;
            _initialized   = false;
            _backups       = null;
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

        // ─── Style Utilities ─────────────────────────────────────────────────

        /// <summary>
        /// GUIStyle の全 state の textColor を同一色に固定する。
        /// EditorStyles.* を継承したスタイルはライトモードの色を引き継ぐため、
        /// hover/active/focused/on* を含む全 state を明示設定して上書きする。
        /// </summary>
        private static void FixAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor    = color;
            style.hover.textColor     = color;
            style.active.textColor    = color;
            style.focused.textColor   = color;
            style.onNormal.textColor  = color;
            style.onHover.textColor   = color;
            style.onActive.textColor  = color;
            style.onFocused.textColor = color;
        }

        // ─── Texture Utilities ───────────────────────────────────────────────

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

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
            tex.filterMode = FilterMode.Point;
            tex.hideFlags  = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >>  8) & 0xFF) / 255f,
            ( rgb        & 0xFF) / 255f);
    }
}
```
