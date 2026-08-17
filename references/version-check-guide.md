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
- **取得頻度は `CheckIntervalHours`（既定 6 時間）で絞る**。ドメインリロードのたびに
  リクエストを飛ばすと GitHub のレート制限に掛かるため（→ [取得頻度とレート制限](#取得頻度とレート制限)）。

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
   - SessionState / EditorPrefs キーの接頭辞（`YourTool_...`）
     — **EditorPrefs キーは Unity エディタ全体で共有される**ので、他ツールと衝突しない
     接頭辞にする（衝突すると別ツールの最新版が表示される）
   - `RefreshOpenWindows()` 内の `YourToolWindow` を自分のウィンドウクラス名に

> **他プロジェクトへのインポートでハマりやすい 2 点（テンプレートで対策済み）**
> 1. `[InitializeOnLoad]` はドメインリロード中に走り `GUIDToAssetPath` が空を返し得る
>    → `EditorApplication.delayCall` で 1 tick 遅延 + `[CallerFilePath]` 起点の相対探索で保険。
> 2. 読み込み失敗値をキャッシュすると誤った版数のままになる
>    → 失敗（null）はキャッシュせず次回アクセスで再試行。
> 3. **名前空間の重複による衝突（CS0101 等）に注意**
>    → 同一Unityプロジェクト内の複数ツールで本アップデートチェッカーを導入する場合、コピーした `DennokoVersionChecker.cs` の名前空間がデフォルトの `namespace Dennoko` のままだと、クラス定義の重複によるコンパイルエラーが発生します。これを避けるため、`DennokoVersionChecker.cs` をコピーする際、必ず各ツール固有の名前空間（例：`namespace Dennoko.YourTool` 等）に変更してください。


## Step 4 — EditorWindow 側の配線

`CreateGUI()` でラベルを取得し、キャッシュされたチェック結果を反映する。

```csharp
private Label _versionLabel;
private DennokoVersionChecker.Result _versionResult =
    new DennokoVersionChecker.Result { State = DennokoVersionChecker.State.Checking, LocalVersion = "0.0.0" };

// ⚠ UnityException: GUIDToAssetPath_Internal is not allowed to be called from a ScriptableObject constructor に注意
// フィールドの初期化子で YourToolVersion.Current を直接呼ぶと、インスタンス化のタイミングで例外になります。
// 初期化子には "0.0.0" などのダミー値を設定し、実際の値は CreateGUI() 内で安全に取得してください。

// CreateGUI() 内で:
//   _versionLabel = root.Q<Label>("version-label");
//   ... ラベル配線後 ...
//   StartVersionCheck();


private void StartVersionCheck()
{
    LoadVersionResultFromSessionState();
    // 取得の要否は StartCheckBackgroundTask 内で判定する（成功済みなら何もしない／
    // 前回エラーなら再試行、ただし前回リクエストから CheckIntervalHours 以内なら
    // 通信せず EditorPrefs のキャッシュを表示に使う）。呼び出し側は毎回呼んでよい。
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

## 取得頻度とレート制限

**GitHub は同一 IP からの高頻度アクセスを 403 で弾く。** チェッカーを素朴に書くと
これを踏むので、テンプレートでは次の 3 点で対策している。

| 対策 | 理由 |
|---|---|
| `raw.githubusercontent.com` を使う（`api.github.com` を使わない） | 未認証の `api.github.com` は **IP あたり 60 req/hour** と枠が狭い。CDN である raw の方が遥かに緩く、403 の回避先として API に移すのは逆効果 |
| `User-Agent` を明示する | `UnityWebRequest` 既定の UA は 403 の対象になり得る |
| `CheckIntervalHours`（既定 6h）を空けて取得する | 対策の本命。下記参照 |

### なぜ間隔制御が必須か

`StartCheckBackgroundTask()` は「成功済みならスキップ、エラーなら再試行」という判定を
するが、**エラー時に無条件で再試行すると自己増幅ループに入る**。

Unity のドメインリロードはスクリプト保存ごと・Play mode 出入りごとに走るため、
開発中は数分で何十回も発生する。つまり:

```
レート制限に掛かる → 403 → State.Error → 次のドメインリロードで即再試行
  → さらにレート制限を悪化 → 永久に「取得できません」
```

そのため最終リクエスト時刻を **`EditorPrefs`** に持ち、間隔内は通信しない。
`SessionState` ではエディタ再起動でリセットされ、再起動のたびに撃ってしまうので不可。

### 間隔内の表示

間隔内でも「確認中...」のまま固めないよう、`ApplyCachedResult()` が **EditorPrefs に
保存した前回の成功結果**をセッションへ流し込み `done` を立てる。

- 永続キャッシュを更新するのは**成功時のみ**。失敗で上書きすると、間隔内の表示から
  「前回取得できていた最新版」が消えてしまう。
- 手動リロード（`ForceRecheck()`）は明示的なユーザー操作なので間隔を無視する。
  抑制対象は自動チェックだけ。

## エラーハンドリング方針

- 通信失敗・HTTP エラー・JSON パース失敗・`version` 欠落 → すべて `State.Error`。
- 指定ブランチで失敗した場合は `"main"` にフォールバックして再取得。
  ただし **403 / 429 のときはフォールバックしない** — ブランチを変えても解消せず、
  リクエストを増やしてレート制限を悪化させるだけ。
- `DennokoVersionChecker` は例外を投げない。失敗時は必ず一度 `Debug.LogWarning` で
  URL・httpCode・error・**レスポンス本文**を出す。本文が無いと `HTTP/1.1 403 Forbidden`
  としか出ず、レート制限なのか repo 名の誤りなのか切り分けられない。
- `req.timeout` を設定する（既定は無期限待ち）。
- エラーは永続キャッシュしないので、間隔が明ければ自動的に再試行され、一時的な失敗
  （インポート直後の中断等）から自己回復する。

## 動作確認

1. ウィンドウを開き、タイトル横に `v1.0.0` が出る。
2. ローカルの `version.json` を下げる（例 `0.9.0`）と `--update` 色で「更新あり <最新版>」。
3. ローカルとリモートが一致で、バージョンのみ表示。
4. repo/branch を不正値にする or オフラインで `--error` 色の取得失敗テキスト、例外なし。
5. 言語切替で接尾辞テキストが切り替わる。
6. 他プロジェクトへインポートして開く → `v0.0.0` に固定されず正しいローカル版が出る。
7. 一致しているのに「更新あり <自分と同じ版>」が出ない（State は表示時に再計算）。
8. スクリプトを保存してドメインリロードを数回起こす → **2 回目以降は通信が走らず**、
   表示は前回結果のまま（「確認中...」で固まらない）。↻ ボタンでは毎回走る。
