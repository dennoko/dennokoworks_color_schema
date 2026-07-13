# トップバー バージョン表記 + GitHub アップデートチェック（共通仕様）

ヘッダー（`.dennoko-header`）にツールの**現行バージョンを常時表示**し、起動時に
**GitHub Public リポジトリ上の `version.json`** を取得してローカル版と比較、
更新があればヘッダーにテキストで示す。取得やパースに失敗した場合は
エラーハンドリングして「最新版を取得できない」旨をテキスト表示する。

- **ターゲット**: dennokoworks の EditorWindow（`window_structure_template.md` の骨格）
- **前提**: リポジトリが GitHub で Public 公開されていること
- **UI 挙動**: テキスト表示のみ（クリックでリンクは開かない）

> **URL はプロジェクトごとに異なる。** チェック先の owner / repo / branch は
> テンプレ側でハードコードせず、**各プロジェクトに設定されているリモートリポジトリ**
> （`git remote get-url origin` で確認できる `owner/repo`）を C# の呼び出し引数に渡すこと。
> branch は指定値で失敗すると `"main"` に自動フォールバックするため、`master` / `main`
> のどちらがデフォルトブランチでも動く（ただし正しく指定すれば余計なリクエストを省ける）。

---

## 全体像（役割分担）

| レイヤー | 担当 |
|---|---|
| UXML | ヘッダー左側に「タイトル + バージョンラベル」を並べる（構造のみ） |
| USS | `.dennoko-version-label`（＋ `--update` / `--error`）を定義（色は `var(--dennoko-*)`） |
| C# | `DennokoVersionChecker`（自己完結・ローカライズ非依存）＋ 表示側で状態→文言化 |

`DennokoVersionChecker` は**文言を返さず「状態(State)」だけ返す**。表示側（各ツールの
i18n）で状態を文言に変換するため、言語切替にも追従できる。

---

## Step 1 — リモートに `version.json` を置く

リポジトリ**直下**に `version.json` を追加してコミットする。`version` が最新版。

```json
{
  "version": "1.0.0",
  "url": "https://github.com/<owner>/<repo>/releases",
  "message": ""
}
```

Raw URL は `https://raw.githubusercontent.com/<owner>/<repo>/<branch>/version.json` になる。
リリースのたびに `version` を上げ、ローカルの現行バージョン定数（Step 4）と揃える。

---

## Step 2 — ヘッダー UXML

`.dennoko-header` は `justify-content: space-between` なので、**左グループ**に
タイトルとバージョンを、**右**に言語ボタン等を置く。`version-label` の `text` は空で置き、
C# から流し込む。

```xml
<ui:VisualElement name="header" class="dennoko-header">
    <ui:VisualElement class="dennoko-header-titlegroup">
        <ui:Label name="title-label" text="YOUR TOOL NAME" class="dennoko-title" />
        <ui:Label name="version-label" text="" class="dennoko-version-label" />
    </ui:VisualElement>
    <ui:Button name="lang-button" text="EN" class="dennoko-lang-button" />
</ui:VisualElement>
<ui:VisualElement class="dennoko-separator" />
```

---

## Step 3 — USS

`DennokoTheme.uss`（`uss_theme_template.md`）の `.dennoko-title` の近くに追加する。
色はすべて `var(--dennoko-*)` を経由（ハードコード禁止）。

```css
/* ヘッダー左側: タイトル + バージョンをまとめる行
   ※ 独自クラスは必ず .dennoko-root を前置する規約 (techniques.md §1)。
   前置しないと .dennoko-root .unity-text-element (0,2,0) に color で負ける */
.dennoko-root .dennoko-header-titlegroup {
    flex-direction: row;
    align-items: center;
}

/* バージョン表記 (タイトル横の小さな補足テキスト) */
.dennoko-root .dennoko-version-label {
    font-size: 10px;
    margin-left: 6px;
    color: var(--dennoko-text-tertiary);
}
/* 更新あり (目立たせるため鮮やかな緑) */
.dennoko-root .dennoko-version-label--update {
    color: var(--dennoko-semantic-success);
    -unity-font-style: bold;
}
/* 最新版の取得に失敗 */
.dennoko-root .dennoko-version-label--error {
    color: var(--dennoko-semantic-warning);
}
```

---

## Step 4 — ローカル版数の取得と初期ロード（各プロジェクトで定義）

現行バージョンをローカルの `version.json` から動的に取得し、インポート時や起動時に自動でアップデートチェックを実行するクラス。**owner / repo / GUID は自分のリポジトリ・アセットに合わせて設定する。**

> **他プロジェクトへのインポートでハマりやすい 2 点（対策込み）**
> 1. **静的コンストラクタのタイミング**: `[InitializeOnLoad]` はドメインリロード中に走り、
>    その時点では `version.json` が **AssetDatabase 未登録**で `GUIDToAssetPath` が空を返すことがある。
>    → 取得開始を **`EditorApplication.delayCall` で 1 tick 遅らせる**。加えて、GUID で引けなかった
>    ときの保険として **`[CallerFilePath]` を起点にスクリプト相対でも `version.json` を探す**
>    （コンパイル時パスなので、インポート先で再コンパイルされればそのプロジェクトの正しいパスに解決される）。
> 2. **フォールバックの永続キャッシュ**: 読み込み失敗値をキャッシュすると、以後ずっと誤った版数のままになる。
>    → **失敗（null）はキャッシュしない**。次回アクセスで再試行できるようにする。

```csharp
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace YourTool
{
    // インポート時（コンパイル完了時）や起動時に自動的にアップデートチェックを実行する
    [InitializeOnLoad]
    internal static class YourToolVersion
    {
        // version.json の GUID (アセット移動に対応するため GUID 経由でパス解決する)
        private const string VersionJsonGuid = "YOUR_VERSION_JSON_GUID";
        // version.json をどうしても読めなかった場合の最終フォールバック（通常は使われない）
        private const string FallbackVersion = "0.0.0";
        private static string _currentCache = null;

        internal static string Current
        {
            get
            {
                // 失敗（null）はキャッシュしない。インポート直後で version.json が
                // まだ読めなかった場合でも、次回アクセス時に再試行できるようにする。
                if (string.IsNullOrEmpty(_currentCache))
                {
                    _currentCache = LoadLocalVersion();
                }
                return string.IsNullOrEmpty(_currentCache) ? FallbackVersion : _currentCache;
            }
        }

        // チェック先（設定されているリモートリポジトリに合わせる）
        internal const string RepoOwner       = "your-owner";
        internal const string RepoName        = "your-repo";
        internal const string RepoBranch      = "main";
        internal const string VersionFilePath = "version.json";

        // セッションキー。State（比較結果）は保存しない — ローカル版が後から正しく解決され得るため、
        // 表示のたびに「保存した最新版 vs 現在のローカル版」で更新有無を再計算する（Step 6）。
        // ここでは取得が成功したか（Error だったか）だけ保存する。
        internal const string VerCheckDoneKey   = "YourTool_VerCheck_Done";
        internal const string VerCheckErrorKey  = "YourTool_VerCheck_Error";
        internal const string VerCheckLatestKey = "YourTool_VerCheck_Latest";
        internal const string VerCheckUrlKey    = "YourTool_VerCheck_Url";
        internal const string VerCheckMessageKey = "YourTool_VerCheck_Message";

        static YourToolVersion()
        {
            // 静的コンストラクタはドメインリロード中に走り、この時点では version.json が
            // AssetDatabase 未登録のことがある。delayCall で 1 tick 遅らせてから開始する。
            EditorApplication.delayCall += StartCheckBackgroundTask;
        }

        // 同一ドメイン内での二重リクエスト防止（ドメインリロードで false に戻る）
        private static bool _checking;

        internal static void StartCheckBackgroundTask()
        {
            // 成功済みなら再取得しない。だがエラー時は「インポート直後の一時的な失敗
            // （パッケージ取り込み時のドメインリロードでリクエストが中断される等）」を想定し、
            // 次のトリガー（ウィンドウを開く / ドメインリロード）で再試行する。
            bool done  = SessionState.GetBool(VerCheckDoneKey, false);
            bool error = SessionState.GetBool(VerCheckErrorKey, false);
            if (done && !error) return;
            if (_checking) return;
            _checking = true;

            Dennoko.DennokoVersionChecker.CheckAsync(
                RepoOwner, RepoName, RepoBranch, VersionFilePath, Current, OnVersionChecked);
        }

        private static void OnVersionChecked(Dennoko.DennokoVersionChecker.Result result)
        {
            _checking = false;
            SessionState.SetBool(VerCheckDoneKey, true);
            SessionState.SetBool(VerCheckErrorKey, result.State == Dennoko.DennokoVersionChecker.State.Error);
            SessionState.SetString(VerCheckLatestKey, result.LatestVersion ?? string.Empty);
            SessionState.SetString(VerCheckUrlKey, result.Url ?? string.Empty);
            SessionState.SetString(VerCheckMessageKey, result.Message ?? string.Empty);

            // すでにエディタウィンドウが開かれている場合は再描画を促す
            var windows = Resources.FindObjectsOfTypeAll<YourToolWindow>();
            if (windows != null && windows.Length > 0)
            {
                foreach (var w in windows)
                {
                    if (w != null)
                    {
                        w.LoadVersionResultFromSessionState();
                    }
                }
            }
        }

        [Serializable]
        private class VersionInfo
        {
            public string version;
        }

        /// <summary>ローカルの version.json を読む。読めなければ null（呼び出し側で
        /// フォールバックし、次回アクセス時に再試行する）。</summary>
        private static string LoadLocalVersion()
        {
            // 1) GUID 経由（アセット移動に追従。ただし AssetDatabase 準備前は空を返し得る）
            var v = TryReadVersion(AssetDatabase.GUIDToAssetPath(VersionJsonGuid));
            if (v != null) return v;

            // 2) スクリプト位置からの相対探索（AssetDatabase 未準備でも解決できる保険）
            return TryReadVersion(ResolveVersionJsonByScriptPath());
        }

        private static string TryReadVersion(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var info = JsonUtility.FromJson<VersionInfo>(File.ReadAllText(path));
                if (info != null && !string.IsNullOrEmpty(info.version)) return info.version;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[YourToolVersion] Failed to read version.json ({path}): {e.Message}");
            }
            return null;
        }

        /// <summary>
        /// このスクリプトの位置を起点に上位フォルダを辿って version.json を探す。
        /// [CallerFilePath] はコンパイル時パスなので、他プロジェクトへインポートして
        /// 再コンパイルされれば、そのプロジェクト内の正しいパスに解決される
        /// （AssetDatabase のインポート完了状況に依存しない）。
        /// </summary>
        private static string ResolveVersionJsonByScriptPath([CallerFilePath] string scriptPath = null)
        {
            if (string.IsNullOrEmpty(scriptPath)) return null;
            var dir = Path.GetDirectoryName(scriptPath);
            for (int i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
            {
                var candidate = Path.Combine(dir, "version.json");
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
```

---

## Step 5 — `DennokoVersionChecker.cs`（そのままコピーして使う）

`Editor/` 配下に配置する。エディタ専用・ネットワークは `UnityWebRequest` の
`.completed` コールバック（Editor 上でも発火、手動ポーリング不要）。**例外は投げず、
失敗はすべて `State.Error` に集約**する。

**ブランチのフォールバック**: 指定した `branch` で取得に失敗した場合は `"main"` に
フォールバックして再取得する。これにより、リポジトリのデフォルトブランチが `master` /
`main` のどちらでも動く（`RepoBranch` の設定ミスやリポジトリ差異に耐える）。

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Dennoko
{
    /// <summary>
    /// GitHub Public リポジトリ上の version.json を取得し、ローカル版と比較する
    /// エディタ専用の自己完結アップデートチェッカー。
    ///
    /// version.json の形式:
    ///   { "version": "1.2.0", "url": "https://.../releases", "message": "" }
    ///
    /// owner / repo は各プロジェクトの「設定されているリモートリポジトリ」を
    /// 呼び出し側から渡す（ハードコードしない）。文言は返さず State だけ返す。
    /// </summary>
    public static class DennokoVersionChecker
    {
        public enum State { Checking, UpToDate, UpdateAvailable, Error }

        public struct Result
        {
            public State State;
            public string LocalVersion;
            public string LatestVersion;
            public string Url;
            public string Message;
        }

        [Serializable]
        private class VersionInfo
        {
            public string version;
            public string url;
            public string message;
        }

        /// <summary>
        /// version.json を非同期取得して結果を onResult に渡す。例外は投げず、失敗時は
        /// State.Error を返す。onResult は Unity のメインスレッド上で呼ばれる。
        ///
        /// 指定 branch で取得できなかった場合は "main" にフォールバックして再取得する
        /// (デフォルトブランチが master / main のどちらでも動くように)。
        /// </summary>
        public static void CheckAsync(
            string owner, string repo, string branch, string filePath,
            string localVersion, Action<Result> onResult)
        {
            if (onResult == null) return;

            // 候補ブランチ: 指定ブランチ → "main" (重複は除外)
            var branches = new List<string>();
            if (!string.IsNullOrEmpty(branch)) branches.Add(branch);
            if (!branches.Contains("main", StringComparer.OrdinalIgnoreCase)) branches.Add("main");

            TryBranch(owner, repo, branches, 0, filePath, localVersion, onResult);
        }

        /// <summary>候補ブランチを index から順に試す。エラーなら次の候補へフォールバックする。</summary>
        private static void TryBranch(
            string owner, string repo, List<string> branches, int index,
            string filePath, string localVersion, Action<Result> onResult)
        {
            if (index >= branches.Count)
            {
                onResult(Error(localVersion));
                return;
            }

            UnityWebRequest req;
            try
            {
                var url = $"https://raw.githubusercontent.com/{owner}/{repo}/{branches[index]}/{filePath}";
                req = UnityWebRequest.Get(url);
            }
            catch (Exception e)
            {
                // URL 組み立て自体の失敗はブランチを変えても直らないため即エラー
                Debug.LogWarning($"[DennokoVersionChecker] request build failed: {e.Message}");
                onResult(Error(localVersion));
                return;
            }

            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                Result result;
                try
                {
                    result = BuildResult(req, localVersion);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DennokoVersionChecker] callback failed: {e.Message}");
                    result = Error(localVersion);
                }
                finally
                {
                    req.Dispose();
                }

                if (result.State == State.Error && index + 1 < branches.Count)
                {
                    // 次の候補ブランチへフォールバック
                    TryBranch(owner, repo, branches, index + 1, filePath, localVersion, onResult);
                }
                else
                {
                    onResult(result);
                }
            };
        }

        private static Result BuildResult(UnityWebRequest req, string localVersion)
        {
            string url = req != null ? req.url : "(null)";
#if UNITY_2020_2_OR_NEWER
            bool hasError = req.result != UnityWebRequest.Result.Success;
#else
            bool hasError = req.isNetworkError || req.isHttpError;
#endif
            // 失敗は必ず一度警告する（チェックはセッション1回のみ）。URL・httpCode・error が
            // 「最新情報を取得できません」の切り分け材料になる（owner/repo/branch・push 有無・回線）。
            if (hasError)
            {
                Debug.LogWarning($"[DennokoVersionChecker] 取得失敗: url={url} httpCode={req.responseCode} error={req.error}");
                return Error(localVersion);
            }

            var json = req.downloadHandler != null ? req.downloadHandler.text : null;
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[DennokoVersionChecker] 取得失敗: レスポンスが空。url={url} httpCode={req.responseCode}");
                return Error(localVersion);
            }

            VersionInfo info;
            try { info = JsonUtility.FromJson<VersionInfo>(json); }
            catch (Exception e) { Debug.LogWarning($"[DennokoVersionChecker] 取得失敗: JSON パース失敗: {e.Message} url={url}"); return Error(localVersion); }

            if (info == null || string.IsNullOrEmpty(info.version))
            {
                Debug.LogWarning($"[DennokoVersionChecker] 取得失敗: version フィールドが空。url={url}");
                return Error(localVersion);
            }

            var state = IsNewer(info.version, localVersion) ? State.UpdateAvailable : State.UpToDate;
            return new Result
            {
                State = state,
                LocalVersion = localVersion,
                LatestVersion = info.version,
                Url = info.url,
                Message = info.message,
            };
        }

        private static Result Error(string localVersion) => new Result
        {
            State = State.Error,
            LocalVersion = localVersion,
            LatestVersion = null,
            Url = null,
            Message = null,
        };

        /// <summary>
        /// latest がローカル版より新しいか（＝更新あり）。State をキャッシュせず、表示側が
        /// 「保存した最新版 vs 現在のローカル版」で都度再計算できるよう公開する（Step 6）。
        /// </summary>
        public static bool IsUpdateAvailable(string latestVersion, string localVersion)
            => IsNewer(latestVersion, localVersion);

        private static bool IsNewer(string latest, string local)
        {
            var l = Normalize(latest);
            var c = Normalize(local);
            if (Version.TryParse(l, out var vLatest) && Version.TryParse(c, out var vLocal))
                return vLatest > vLocal;
            return !string.Equals(l, c, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 比較用に正規化する。BOM / 先頭 v / プレリリース・ビルドメタデータを除去し、
        /// 2 桁以下（"3", "3.0"）は 3 桁（"3.0.0"）へゼロ埋めする。
        /// ゼロ埋めしないと Version 型で Build=-1 となり "3.0" &lt; "3.0.0" の誤判定が出る。
        /// </summary>
        private static string Normalize(string v)
        {
            if (string.IsNullOrEmpty(v)) return "0.0.0";
            v = v.Trim().Trim('﻿').Trim(); // 空白と BOM を除去
            if (v.Length > 0 && (v[0] == 'v' || v[0] == 'V')) v = v.Substring(1);
            // "1.2.0-beta" / "1.2.0+build" などのサフィックスは比較対象外
            int cut = v.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut >= 0) v = v.Substring(0, cut);
            if (string.IsNullOrEmpty(v)) return "0.0.0";

            var parts = v.Split('.');
            if (parts.Length >= 3) return v;
            var padded = new string[3];
            for (int i = 0; i < 3; i++)
                padded[i] = (i < parts.Length && !string.IsNullOrEmpty(parts[i])) ? parts[i] : "0";
            return string.Join(".", padded);
        }
    }
}
```

---

## Step 6 — EditorWindow 側の配線

`CreateGUI()` でバージョンラベルを取得し、キャッシュされたチェック結果を読み込んでラベルに反映する。すでにバックグラウンドチェックが終わっていれば即時に反映され、終わっていなければバックグラウンドタスクをトリガーします。

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

`Loc(...)` は各ツールのローカライズ関数（例: `DenEmoLoc.T` / `DenEmoLoc.Tf`）に置き換える。
チェッカーが文言を持たないので、この 3 文言（更新あり / 取得失敗 / 確認中）だけ
自分の言語辞書に追加すればよい。

---

## Step 7 — 手動リロードボタン（任意・推奨）

「最新情報を取得できません」が出たときに、ユーザーが**その場で再取得を試せる** ↻ ボタンを
バージョン表記の右に置く。取得失敗（オフライン／取り込み直後の中断）の切り分けに有効。

**UXML**（`version-label` の直後、titlegroup 内）:
```xml
<ui:Label name="version-label" text="" class="dennoko-version-label" />
<ui:Button name="version-reload-button" text="↻" class="dennoko-version-reload-button" />
```

**USS**（`.dennoko-version-label` の近く。色は `var(--dennoko-*)` 経由）:
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

**版数クラス側に再取得 API を追加**（Step 4 の `YourToolVersion` に）:
```csharp
/// <summary>手動での再取得。前回結果（成功/失敗・ローカル版キャッシュ）を破棄して再チェックする。</summary>
internal static void ForceRecheck()
{
    if (_checking) return; // 進行中なら何もしない
    _currentCache = null;  // ローカル版も読み直す（version.json を直したケースに対応）
    SessionState.SetBool(VerCheckDoneKey, false);
    SessionState.SetBool(VerCheckErrorKey, false);
    StartCheckBackgroundTask();
}
```

**EditorWindow 側の配線**（`version-reload-button` を取得して click を接続）:
```csharp
var reloadButton = root.Q<Button>("version-reload-button");
if (reloadButton != null)
{
    reloadButton.tooltip = Loc("アップデートを再確認"); // ← 各ツールの i18n に置換
    reloadButton.clicked += () =>
    {
        // 明示的に再取得。結果を破棄して再チェックし、即座に「確認中...」表示へ。
        // 完了時に OnVersionChecked → LoadVersionResultFromSessionState で再描画される。
        YourToolVersion.ForceRecheck();
        LoadVersionResultFromSessionState();
    };
}
```

> `↻` (U+21BB) は Unity 既定エディタフォントで描画できる。グリフを使わず Unity 組み込みの
> Refresh アイコンを使いたい場合は C# で `reloadButton.style.backgroundImage` に
> `EditorGUIUtility.IconContent("d_Refresh").image` を設定する（常時ダーク前提なら `d_` 版が視認性良好）。

---

## エラーハンドリング方針

- 通信失敗・HTTP エラー・JSON パース失敗・`version` 欠落 → すべて `State.Error`。
- 指定ブランチで `State.Error` になった場合は **`"main"` にフォールバック**して再取得し、
  そこでも失敗して初めて `State.Error` を確定する（デフォルトブランチが master / main
  どちらでも動く）。`RepoBranch` を正しく設定すればフォールバックは発生せず 1 回で済む。
- `DennokoVersionChecker` は**例外を投げない**（内部で握り潰す）。**失敗時は必ず一度 `Debug.LogWarning`**
  で URL・httpCode・error を出す（チェックはセッション1回なので原因追跡に使える）。
- **エラーはセッションキャッシュしない**。`StartCheckBackgroundTask` は「成功済みのみ再取得しない」
  ため、ウィンドウを開き直す/ドメインリロードのたびに再試行し、一時的な失敗から自己回復する。
  → 「他プロジェクトへインポート直後だけ『取得できません』が出る」の典型原因は、取り込み時の
  ドメインリロードで in-flight のリクエストが中断されること。再試行で解消する。
- 表示は `.dennoko-version-label--error`（`--dennoko-semantic-warning`）で警告色テキスト。

## 動作確認

1. ウィンドウを開き、タイトル横に `v1.0.0` が出る。
2. ローカルの `version.json` を下げる（例 `0.9.0`）と `--update` 色で「更新あり <最新版>」。
3. ローカルとリモートが一致で、バージョンのみ表示（更新ありにならない）。
4. repo/branch を不正値にする or オフラインで `--error` 色の取得失敗テキスト、例外なし。
5. 言語切替で接尾辞テキストが切り替わる。
6. **他プロジェクトへインポート**して開く → `v0.0.0` などのフォールバックに固定されず、
   正しいローカル版が表示される（GUID 未解決でもスクリプト相対探索で解決される）。
7. **矛盾表示が残らない**こと: 一致しているのに「更新あり <自分と同じ版>」が出ない
   （State はキャッシュせず表示時に再計算されるため自己修復する）。
