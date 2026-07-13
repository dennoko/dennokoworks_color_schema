# トップバー バージョン表記 + GitHub アップデートチェック

ヘッダー（`.dennoko-header`）にツールの現行バージョンを常時表示し、起動時に
GitHub Public リポジトリ上の `version.json` を取得してローカル版と比較、
更新があればテキストで示す。取得失敗はエラーハンドリングしてテキスト表示する。

コピー元コード:
- [assets/VersionCheck/DennokoVersionChecker.cs](../assets/VersionCheck/DennokoVersionChecker.cs) — そのままコピー（変更不要）
- [assets/VersionCheck/YourToolVersion.cs](../assets/VersionCheck/YourToolVersion.cs) — コピー後にカスタマイズ

## 設計方針

| レイヤー | 担当 |
|---|---|
| UXML | ヘッダー左側に「タイトル + バージョンラベル」を並べる（構造のみ） |
| USS | `.dennoko-version-label`（+ `--update` / `--error`）— **テーマ USS に定義済み** |
| C# | `DennokoVersionChecker`（自己完結・ローカライズ非依存）+ 表示側で状態→文言化 |

- `DennokoVersionChecker` は**文言を返さず「状態(State)」だけ返す**。
  表示側（各ツールの i18n）で文言に変換するため、言語切替に追従できる。
- チェック先の owner / repo はハードコードせず、各プロジェクトのリモートリポジトリ
  （`git remote get-url origin`）に合わせて `YourToolVersion` の定数に設定する。
  branch は失敗時に `"main"` へ自動フォールバックする。

## Step 1 — リモートに `version.json` を置く

リポジトリ**直下**に追加してコミットする。リリースのたびに `version` を上げる。

```json
{
  "version": "1.0.0",
  "url": "https://github.com/<owner>/<repo>/releases",
  "message": ""
}
```

## Step 2 — ヘッダー UXML

`version-label` の `text` は空で置き、C# から流し込む。

```xml
<ui:VisualElement name="header" class="dennoko-header">
    <ui:VisualElement class="dennoko-header-titlegroup">
        <ui:Label name="title-label" text="YOUR TOOL NAME" class="dennoko-title" />
        <ui:Label name="version-label" text="" class="dennoko-version-label" />
    </ui:VisualElement>
    <ui:Button name="lang-button" text="EN" />
</ui:VisualElement>
<ui:VisualElement class="dennoko-separator" />
```

## Step 3 — C# の配置とカスタマイズ

1. `DennokoVersionChecker.cs` を `Editor/` 配下にそのままコピー
2. `YourToolVersion.cs` をコピーし、以下を変更:
   - `namespace` / クラス名
   - `VersionJsonGuid`（ローカル `version.json` の GUID）
   - `RepoOwner` / `RepoName` / `RepoBranch`
   - SessionState キーの接頭辞（`YourTool_...`）
   - `OnVersionChecked` 内の `YourToolWindow` を自分のウィンドウクラス名に

> **他プロジェクトへのインポートでハマりやすい 2 点（テンプレートで対策済み）**
> 1. `[InitializeOnLoad]` はドメインリロード中に走り `GUIDToAssetPath` が空を返し得る
>    → `EditorApplication.delayCall` で 1 tick 遅延 + `[CallerFilePath]` 起点の相対探索で保険。
> 2. 読み込み失敗値をキャッシュすると誤った版数のままになる
>    → 失敗（null）はキャッシュせず次回アクセスで再試行。

## Step 4 — EditorWindow 側の配線

`CreateGUI()` でラベルを取得し、キャッシュされたチェック結果を反映する。

```csharp
private Label _versionLabel;
private DennokoVersionChecker.Result _versionResult =
    new DennokoVersionChecker.Result { State = DennokoVersionChecker.State.Checking, LocalVersion = YourToolVersion.Current };

// CreateGUI() 内で:
//   _versionLabel = root.Q<Label>("version-label");
//   ... ラベル配線後 ...
//   StartVersionCheck();

private void StartVersionCheck()
{
    LoadVersionResultFromSessionState();
    // 取得の要否は StartCheckBackgroundTask 内で判定する（成功済みなら何もしない／
    // 前回エラーなら再試行）。ウィンドウを開き直すたびに一時的な失敗から自己回復できる。
    YourToolVersion.StartCheckBackgroundTask();
}

internal void LoadVersionResultFromSessionState()
{
    // State（更新有無）はキャッシュせず、常に「現在のローカル版 vs 取得済みの最新版」で
    // 再計算する。こうしないと、取得時のローカル版が後から正しく解決された場合に
    // 「v3.0.0 更新あり 3.0.0」のような矛盾表示が残ってしまう。
    string local  = YourToolVersion.Current;
    string latest = SessionState.GetString(YourToolVersion.VerCheckLatestKey, string.Empty);
    bool   done   = SessionState.GetBool(YourToolVersion.VerCheckDoneKey, false);
    bool   error  = SessionState.GetBool(YourToolVersion.VerCheckErrorKey, false);

    DennokoVersionChecker.State state;
    if (!done)
        state = DennokoVersionChecker.State.Checking;
    else if (error || string.IsNullOrEmpty(latest))
        state = DennokoVersionChecker.State.Error;
    else if (DennokoVersionChecker.IsUpdateAvailable(latest, local))
        state = DennokoVersionChecker.State.UpdateAvailable;
    else
        state = DennokoVersionChecker.State.UpToDate;

    _versionResult = new DennokoVersionChecker.Result
    {
        State = state,
        LocalVersion = local,
        LatestVersion = latest,
        Url = SessionState.GetString(YourToolVersion.VerCheckUrlKey, string.Empty),
        Message = SessionState.GetString(YourToolVersion.VerCheckMessageKey, string.Empty)
    };
    ApplyVersionLabel();
}

private void ApplyVersionLabel()
{
    if (_versionLabel == null) return;

    var r = _versionResult;
    string baseText = "v" + r.LocalVersion;
    string text;
    bool update = false, error = false;
    switch (r.State)
    {
        case DennokoVersionChecker.State.UpdateAvailable:
            text = baseText + "  " + Loc("更新あり {0}", r.LatestVersion); // ← 各ツールの i18n に置換
            update = true;
            break;
        case DennokoVersionChecker.State.Error:
            text = baseText + "  " + Loc("最新版を取得できません");
            error = true;
            break;
        case DennokoVersionChecker.State.Checking:
            text = baseText + "  " + Loc("確認中...");
            break;
        default: // UpToDate
            text = baseText;
            break;
    }
    _versionLabel.text = text;
    _versionLabel.EnableInClassList("dennoko-version-label--update", update);
    _versionLabel.EnableInClassList("dennoko-version-label--error", error);
}
```

`Loc(...)` は各ツールのローカライズ関数に置き換える。必要な文言は
「更新あり / 取得失敗 / 確認中」の 3 つだけ。

## Step 5 — 手動リロードボタン（任意・推奨）

「取得できません」時にその場で再試行できる ↻ ボタンを version-label の右に置く。

**UXML**（titlegroup 内、`version-label` の直後）:
```xml
<ui:Button name="version-reload-button" text="↻" class="dennoko-version-reload-button" />
```

**USS**（DennokoTheme.uss に追加。独自クラスなので `.dennoko-root` 前置規約に従う）:
```css
.dennoko-root .dennoko-version-reload-button {
    width: 18px;
    height: 18px;
    margin-left: 4px;
    padding: 0;
    font-size: 12px;
    color: var(--dennoko-text-tertiary);
}
.dennoko-root .dennoko-version-reload-button:hover {
    color: var(--dennoko-text-primary);
}
```

**配線**（`YourToolVersion.ForceRecheck()` はテンプレートに定義済み）:
```csharp
var reloadButton = root.Q<Button>("version-reload-button");
if (reloadButton != null)
{
    reloadButton.tooltip = Loc("アップデートを再確認");
    reloadButton.clicked += () =>
    {
        YourToolVersion.ForceRecheck();
        LoadVersionResultFromSessionState(); // 即座に「確認中...」表示へ
    };
}
```

> `↻` (U+21BB) は Unity 既定エディタフォントで描画できる。Unity 組み込みの
> Refresh アイコンを使う場合は `reloadButton.style.backgroundImage` に
> `EditorGUIUtility.IconContent("d_Refresh").image` を設定する。

## エラーハンドリング方針

- 通信失敗・HTTP エラー・JSON パース失敗・`version` 欠落 → すべて `State.Error`。
- 指定ブランチで失敗した場合は `"main"` にフォールバックして再取得。
- `DennokoVersionChecker` は例外を投げない。失敗時は必ず一度 `Debug.LogWarning`
  で URL・httpCode・error を出す（原因追跡用）。
- **エラーはセッションキャッシュしない**。ウィンドウを開き直す / ドメインリロードの
  たびに再試行し、一時的な失敗（インポート直後の中断等）から自己回復する。

## 動作確認

1. ウィンドウを開き、タイトル横に `v1.0.0` が出る。
2. ローカルの `version.json` を下げる（例 `0.9.0`）と `--update` 色で「更新あり <最新版>」。
3. ローカルとリモートが一致で、バージョンのみ表示。
4. repo/branch を不正値にする or オフラインで `--error` 色の取得失敗テキスト、例外なし。
5. 言語切替で接尾辞テキストが切り替わる。
6. 他プロジェクトへインポートして開く → `v0.0.0` に固定されず正しいローカル版が出る。
7. 一致しているのに「更新あり <自分と同じ版>」が出ない（State は表示時に再計算）。
