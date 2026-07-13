# CustomEditor (Inspector) 実装ガイド

コピー元コード: [assets/Inspector/YourCustomEditor.uxml](../assets/Inspector/YourCustomEditor.uxml) / [assets/Inspector/YourCustomEditor.cs](../assets/Inspector/YourCustomEditor.cs)
テーマ: [assets/DennokoTheme.uss](../assets/DennokoTheme.uss)（EditorWindow と共有可）

## EditorWindow との根本的な違い

| 項目 | EditorWindow | CustomEditor (Inspector) |
|---|---|---|
| エントリーポイント | `CreateGUI()` | `CreateInspectorGUI()` (override, 戻り値あり) |
| ルート要素 | `rootVisualElement` に追加 | `new VisualElement()` を作って **return** する |
| 背景の塗り方 | `.dennoko-root` で全面に塗れる | InspectorElement の余白が残るため `.dennoko-inspector-root` (ネガティブマージン) を併用する |
| スクロール | UXML に `ScrollView` を置く | Unity が自動管理（`ScrollView` 不要） |
| データ連携 | 手動で `Q<T>()` + コールバック | `PropertyField` + `container.Bind(serializedObject)` |
| ボタン推奨サイズ | `height: 34px / 26px` | **`height: 30px / 24px`**（インスペクターは幅が狭い） |

## 配置手順

1. `DennokoTheme.uss` をプロジェクトに追加（未追加の場合）
2. `assets/Inspector/` の UXML / C# をコピーして配置
   - `namespace` / クラス名 / `[CustomEditor(typeof(YourComponent))]` の型を変更
   - `PropertyField` の `binding-path` を対象のシリアライズフィールド名に合わせる
3. `.meta` から GUID を控えて `UXML_GUID` / `USS_GUID` に設定

## Inspector 固有の注意点

### 1. 背景の塗り — `.dennoko-inspector-root`

Unity の `InspectorElement` が持つ左右余白のぶん、`.dennoko-root` の背景の外側に
エディタテーマの背景色が見えてしまう。テーマ USS に定義済みの
`.dennoko-root.dennoko-inspector-root`（ルート要素自身に両クラスを付与するため
連結セレクタ）がネガティブマージンで余白を打ち消す。

> **注意:** 余白量は Unity バージョンによって異なる。左右に隙間が残る・
> はみ出す場合は margin の値を調整する。数 px の隙間を許容できる場合は
> このクラスを付けなくてもよい（カード自体は正しくダーク表示される）。

### 2. `PropertyField` はオーバーライドが自動で効く

`PropertyField` は内部で標準の `TextField` / `FloatField` / `Toggle` 等を生成するため、
テーマ USS のオーバーライドがそのまま適用される。個別のスタイル指定は不要。

### 3. `Bind()` を忘れない

`CloneTree` しただけでは `PropertyField` は空のまま表示される。
`container.Bind(serializedObject)` を必ず呼ぶこと。
値の変更・Undo・複数選択 (`CanEditMultipleObjects`) はバインディングが自動処理する。

### 4. ボタンサイズはインスペクター向けに小さく

`dennoko-button-primary` (34px) が大きすぎる場合はインライン style か専用クラスで詰める。

```xml
<ui:Button text="Apply" class="dennoko-button-primary" style="height: 30px;" />
```

### 5. 複数の CustomEditor が衝突する場合

同一型に複数の `[CustomEditor]` があると片方しか使われない。
継承先も対象にする場合は `[CustomEditor(typeof(X), true)]` を確認する。
