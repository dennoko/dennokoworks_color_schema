---
name: dennokoworks-design
description: dennokoworks フローティングデザインシステムを Unity Editor 拡張（EditorWindow / CustomEditor）に UI Toolkit (UXML/USS) で適用する
---

# dennokoworks Design Skill

## スキルの目的

このスキルは **dennokoworks カラースキーマ（フローティングデザイン）** を Unity Editor 拡張の **UI Toolkit (UXML/USS)** コードとして実装する。
ユーザーが Unity Editor Window や Inspector の UI を実装・修正するよう求めた場合、このスキルディレクトリのテンプレートとカラー定義を参照してコードを提供する。

- **ターゲット環境**: Unity 2022.3 ～ Unity 6
- **最優先要件**: Unity エディタのテーマ設定 (Personal Light / Dark 等) に左右されず、常にフローティングデザイン（ダークテーマ）を維持すること

---

## 呼び出しパターン

以下のいずれかに当てはまる場合、このスキルを適用する：

- `dennokoworks テーマで実装して`
- `このウィンドウにデザインを適用して`
- `/dennokoworks-design`
- Unity Editor 拡張（EditorWindow / CustomEditor）の新規実装・テーマ適用を求められた場合
- 旧 IMGUI 実装（UniTexTheme / DrawSection パターン）の UI Toolkit への移行を求められた場合

---

## 実装の判断フロー

```
ユーザーの要求
    │
    ├─ EditorWindow を作る/改修する
    │       → uss_theme_template + window_structure_template を使用
    │
    ├─ Inspector / CustomEditor を作る/改修する
    │       → uss_theme_template + inspector_structure_template を使用
    │
    ├─ 旧 IMGUI 実装を UI Toolkit へ移行する
    │       → techniques.md セクション 8 の移行マッピングを使用
    │
    └─ テーマ USS だけ欲しい
            → uss_theme_template のみ提供
```

---

## このスキルディレクトリのファイル

| ファイル | 用途 |
|---|---|
| `forUnity/uss_theme_template.md` | テーマ USS (`DennokoTheme.uss`) 全文。最初にプロジェクトに追加するファイル |
| `forUnity/window_structure_template.md` | EditorWindow の UXML + C# 骨格。コピーして使う |
| `forUnity/inspector_structure_template.md` | CustomEditor (Inspector) の UXML + C# 骨格。コピーして使う |
| `forUnity/techniques.md` | UI Toolkit 固有の実装テクニック・罠・IMGUI からの移行マッピング |
| `Docs/colors_spec.md` | カラーの役割・意図の詳細説明 |
| `Docs/design_reference.md` | デザインコンセプト（フローティングデザイン）の解説 |
| `colors.json` | カラー実値（#RRGGBB）のマスターデータ |

---

## カラーパレット（クイックリファレンス）

すべて `DennokoTheme.uss` の `.dennoko-root` に USS 変数として定義済み。

### サーフェス
| 役割 | HEX | USS 変数 |
|---|---|---|
| アプリ背景 | `#121212` | `--dennoko-surface-0` |
| カード・入力欄背景 | `#1e1e1e` | `--dennoko-surface-1` |
| ツールバー・ホバー背景 | `#2c2c2c` | `--dennoko-surface-2` |
| 境界線・セパレーター | `#3a3a3a` | `--dennoko-outline` |

### テキスト
| 役割 | HEX | USS 変数 | 専用クラス |
|---|---|---|---|
| タイトル・強調 | `#ffffff` | `--dennoko-text-primary` | `.dennoko-text-primary` |
| 本文・ラベル | `#cccccc` | `--dennoko-text-secondary` | （デフォルト。指定不要） |
| 補足・見出し | `#aaaaaa` | `--dennoko-text-tertiary` | `.dennoko-text-tertiary` |
| 無効状態 | `#555555` | `--dennoko-text-disabled` | `.dennoko-text-disabled` |

### セマンティック
| 役割 | HEX | USS 変数 | 専用クラス |
|---|---|---|---|
| エラー | `#9b1b30` | `--dennoko-semantic-error` | `.dennoko-text-error` |
| 警告 | `#ffb74d` | `--dennoko-semantic-warning` | `.dennoko-text-warning` |
| 成功 | `#4caf50` | `--dennoko-semantic-success` | `.dennoko-text-success` |
| 情報 | `#64b5f6` | `--dennoko-semantic-info` | `.dennoko-text-info` |

### インタラクション
| 役割 | HEX | USS 変数 |
|---|---|---|
| アクセント | `#ffffff` | `--dennoko-accent` |
| ホバーオーバーレイ | `rgba(255,255,255,0.05)` | `--dennoko-hover-overlay` |

---

## 実装ルール（必ず守ること）

### 1. ルート要素に `dennoko-root` クラスを付与する
テーマ非依存とUSS変数の継承はこのクラスが起点。EditorWindow なら `rootVisualElement`、
Inspector なら return するコンテナに `AddToClassList("dennoko-root")` する。

```csharp
public void CreateGUI()
{
    var root = rootVisualElement;
    root.AddToClassList("dennoko-root");
    // USS ロード失敗時の保険として Surface0 を C# 側でも設定
    root.style.backgroundColor = new Color32(0x12, 0x12, 0x12, 0xFF);
    // USS / UXML を GUID でロード（テンプレート参照）
}
```

### 2. カラーはUSS変数を経由する（ハードコード禁止）
カラーコードの直書きは `DennokoTheme.uss` の変数定義部のみに限定する。
UXML のインライン style や C# の `style.color` に色を直書きしない。

```css
/* ❌ 禁止 */
.my-label { color: #aaaaaa; }

/* ✅ 正しい */
.my-label { color: var(--dennoko-text-tertiary); }
```

### 3. 役割分担: UXML = 構造 / USS = スタイル / C# = ロジック
- レイアウト構造は UXML に書く。C# で `new VisualElement()` を手組みしない（動的リストは例外）
- C# は `CreateGUI` / `CreateInspectorGUI`、アセットロード、イベント接続に専念する

### 4. UXML / USS は GUID でロードする
パス直書きはアセット移動で壊れる。`AssetDatabase.GUIDToAssetPath(GUID)` で解決する。
テンプレートの `YOUR_UXML_GUID_HERE` プレースホルダーは必ず実際の GUID に置き換える。

---

## EditorWindow の実装手順

1. `forUnity/uss_theme_template.md` を `Editor/UI/DennokoTheme.uss` として配置

2. `forUnity/window_structure_template.md` の UXML を `Editor/UI/YourEditorWindow.uxml`、
   C# を `Editor/YourEditorWindow.cs` として配置
   - `namespace` / `[MenuItem("Tools/Your Tool Name")]` を変更
   - UXML に `.dennoko-card` のセクションを追加
   - `ApplyAndSave()` / `ResetAll()` を実装

3. Unity インポート後、`.meta` から GUID を控えて `UXML_GUID` / `USS_GUID` に設定

---

## CustomEditor (Inspector) の実装手順

1. `DennokoTheme.uss` をプロジェクトに追加（未追加の場合。EditorWindow と共有可）

2. `forUnity/inspector_structure_template.md` の UXML / C# を配置
   - `[CustomEditor(typeof(YourComponent))]` の型を変更
   - `PropertyField` の `binding-path` を対象フィールド名に合わせる

3. Inspector 固有の注意点：
   - `CreateInspectorGUI()` はコンテナを **return** する（`rootVisualElement` ではない）
   - コンテナに `dennoko-root` と `dennoko-inspector-root`（余白打ち消し）の両方を付与
   - `container.Bind(serializedObject)` を必ず呼ぶ（忘れると PropertyField が空になる）

---

## セクションの追加パターン

**常時表示セクション**
```xml
<ui:VisualElement class="dennoko-card">
    <ui:Label text="SECTION TITLE" class="dennoko-section-title dennoko-card-header" />
    <!-- コンテンツ -->
</ui:VisualElement>
```

**ON/OFF トグル付きセクション** — UXML にトグル付きヘッダーを置き、C# で `BindToggleSection()` を呼ぶ。
```xml
<ui:VisualElement class="dennoko-card">
    <ui:VisualElement class="dennoko-card-header dennoko-toggle-header">
        <ui:Toggle name="section-toggle" text="SECTION TITLE" value="true"
            class="dennoko-section-title" />
        <ui:Button name="section-reset" text="Reset" />
    </ui:VisualElement>
    <ui:VisualElement name="section-content">
        <!-- コンテンツ -->
    </ui:VisualElement>
</ui:VisualElement>
```

---

## よくある問題と対処

| 問題 | 原因 | 対処 |
|---|---|---|
| スタイルが全く効かない | `dennoko-root` クラスの付け忘れ / GUID がプレースホルダーのまま | ルート要素に `AddToClassList("dennoko-root")` + GUID を設定 |
| Light テーマで文字・アイコンが見えない | オーバーライド外のビルトイン要素 / 画像のテーマ依存 | `-unity-background-image-tint-color` で固定（`techniques.md` §4） |
| Foldout の矢印が白い箱になる | Toggle のチェックボックス装飾が波及 | USS の打ち消しルールを維持する（`techniques.md` §3） |
| `border: 1px solid` がエラー | USS は CSS ショートハンド非対応 | `border-width` + `border-color` に分ける（`techniques.md` §6） |
| Inspector の PropertyField が空 | `Bind()` 忘れ | `container.Bind(serializedObject)` を呼ぶ |
| Inspector の左右に明るい隙間 | InspectorElement の余白 | `.dennoko-inspector-root` を付与し margin を調整 |

詳細は `forUnity/techniques.md` を参照。

---

## 動作確認チェックリスト

1. Preferences でエディタテーマを **Light / Dark 両方**に切り替えて表示確認したか
2. GUID プレースホルダーを実際の GUID に置き換えたか
3. ルート要素に `dennoko-root` を付与したか
4. カラーを直書きせず USS 変数を経由しているか

---

## デザインコンセプト（参考）

**フローティングデザイン**：
- 全体背景は最も暗い `--dennoko-surface-0 (#121212)`
- コンテンツは `--dennoko-surface-1 (#1e1e1e)` のカード (`.dennoko-card`) として浮かび上がって見える
- USS に box-shadow はないため、Elevation はサーフェス間の明度差で表現する
- 表面間のコントラストは低め、テキストは高コントラスト（白系）
- セマンティックカラーはやや彩度を抑えて統一感を保つ
- `--dennoko-outline (#3a3a3a)` で境界線を描くことで要素の輪郭を示す

詳細は `Docs/design_reference.md` / `Docs/colors_spec.md` を参照。
