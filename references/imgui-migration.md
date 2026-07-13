# IMGUI (旧 UniTexTheme) からの移行マッピング

旧来の IMGUI 実装（`UniTexTheme` / `DrawSection` パターン）を UI Toolkit へ変換する際の対応表。

| IMGUI 概念 (旧 UniTexTheme) | UI Toolkit (UXML 要素 / USS クラス) |
|---|---|
| `OnGUI()` + `EditorGUI.DrawRect(..., Surface0)` | `CreateGUI()` + ルートに `.dennoko-root`（背景は USS が塗る） |
| `PushEditorTheme()` / `PopEditorTheme()` | **不要**。USS の子孫セレクタが常時適用されるため Push/Pop の概念自体がない |
| `Initialize()` + テクスチャキャッシュ | **不要**。テクスチャ生成・ドメインリロード対策は不要になる |
| `CardStyle` / `DrawSection(title, ...)` | `<ui:VisualElement class="dennoko-card">` + 見出し `Label` |
| `DrawToggleSection(...)` | `.dennoko-toggle-header` + `Toggle` + `BindToggleSection()` ヘルパー |
| `YourTheme.Surface0` 等の Color 定数 | `var(--dennoko-surface-0)` 等の USS 変数 |
| `YourTheme.TextPrimary` | `.dennoko-text-primary` クラス |
| `YourTheme.TextSecondary` | 指定不要（`.unity-text-element` のデフォルト） |
| `YourTheme.TextTertiary` | `.dennoko-text-tertiary` クラス |
| `YourTheme.SemanticError` 等 | `.dennoko-text-error` 等、または `var(--dennoko-semantic-error)` |
| `ActionButtonStyle` / `GUILayout.Button` | `<ui:Button class="dennoko-button-primary">` |
| `SecondaryButtonStyle` | `<ui:Button class="dennoko-button-secondary">` |
| `EditorGUILayout.TextField(...)` | `<ui:TextField>`（`.unity-base-field__input` の上書きが自動適用） |
| `EditorGUILayout.ObjectField(...)` | `<uie:ObjectField>` |
| `EditorGUILayout.Slider(...)` | `<ui:Slider show-input-field="true">` |
| `GetStatusStyle(StatusType)` | `.dennoko-status--success` 等のクラス切り替え（`EnableInClassList`） |
| `DrawSeparator()` | `<ui:VisualElement class="dennoko-separator" />` |
| `serializedObject.Update()` / `ApplyModifiedProperties()` | `PropertyField` + `Bind(serializedObject)` が自動処理 |

移行によって**不要になる**もの: `MakeTex` / `MakeBorderedTex`、`FixAllTextColors`、
`GUIStyleBackup`、`_initialized` フラグ、テクスチャの null チェック。
これらはすべて IMGUI の制約に対する回避策であり、UI Toolkit では USS が代替する。

## 部分的に IMGUI を残す場合

移行しきれず IMGUI (`OnGUI()`) を併用する部分には、テーマ USS が届かない。
Light テーマ対策（数値入力欄の背景・テクスチャ型 ObjectField の描画崩れ）は
references/troubleshooting.md §7 を参照。
