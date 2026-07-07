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
/* ヘッダー左側: タイトル + バージョンをまとめる行 */
.dennoko-header-titlegroup {
    flex-direction: row;
    align-items: center;
}

/* バージョン表記 (タイトル横の小さな補足テキスト)
   ※ .dennoko-root .unity-text-element (詳細度 0,2,0) に負けないよう .dennoko-root を前置する */
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

## Step 4 — ローカル版数の定数（各プロジェクトで定義）

現行バージョンとチェック先リポジトリを 1 か所にまとめる。**owner / repo は自分の
リモートリポジトリに合わせて設定する。**

```csharp
namespace YourTool
{
    internal static class YourToolVersion
    {
        internal const string Current = "1.0.0";

        // チェック先（設定されているリモートリポジトリに合わせる）
        internal const string RepoOwner       = "your-owner";
        internal const string RepoName        = "your-repo";
        internal const string RepoBranch      = "main";
        internal const string VersionFilePath = "version.json";
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
#if UNITY_2020_2_OR_NEWER
            bool hasError = req.result != UnityWebRequest.Result.Success;
#else
            bool hasError = req.isNetworkError || req.isHttpError;
#endif
            if (hasError) return Error(localVersion);

            var json = req.downloadHandler != null ? req.downloadHandler.text : null;
            if (string.IsNullOrEmpty(json)) return Error(localVersion);

            VersionInfo info;
            try { info = JsonUtility.FromJson<VersionInfo>(json); }
            catch { return Error(localVersion); }

            if (info == null || string.IsNullOrEmpty(info.version)) return Error(localVersion);

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

        /// <summary>latest がローカル版より新しいか。SemVer 優先、パース不能時は文字列不一致で判定。</summary>
        private static bool IsNewer(string latest, string local)
        {
            var l = Normalize(latest);
            var c = Normalize(local);
            if (Version.TryParse(l, out var vLatest) && Version.TryParse(c, out var vLocal))
                return vLatest > vLocal;
            return !string.Equals(l, c, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string v)
        {
            if (string.IsNullOrEmpty(v)) return "0";
            v = v.Trim();
            if (v.StartsWith("v") || v.StartsWith("V")) v = v.Substring(1);
            return v;
        }
    }
}
```

---

## Step 6 — EditorWindow 側の配線

`CreateGUI()` でバージョンラベルを取得し、チェックを起動する。結果を保持しておき、
言語切替時（`RefreshChromeLabels()` 相当）にも再適用する。**同一 Unity セッション中の
再フェッチは `SessionState` で抑制**する（ウィンドウ開閉のたびに叩かない）。

```csharp
private Label _versionLabel;
private DennokoVersionChecker.Result _versionResult =
    new DennokoVersionChecker.Result { State = DennokoVersionChecker.State.Checking, LocalVersion = YourToolVersion.Current };

const string VerCheckDoneKey   = "YourTool_VerCheck_Done";
const string VerCheckStateKey  = "YourTool_VerCheck_State";
const string VerCheckLatestKey = "YourTool_VerCheck_Latest";

// CreateGUI() 内で:
//   _versionLabel = root.Q<Label>("version-label");
//   ... ラベル配線後 ...
//   StartVersionCheck();

private void StartVersionCheck()
{
    if (SessionState.GetBool(VerCheckDoneKey, false))
    {
        _versionResult = new DennokoVersionChecker.Result
        {
            State = (DennokoVersionChecker.State)SessionState.GetInt(VerCheckStateKey, 0),
            LocalVersion = YourToolVersion.Current,
            LatestVersion = SessionState.GetString(VerCheckLatestKey, string.Empty),
        };
        ApplyVersionLabel();
        return;
    }

    ApplyVersionLabel(); // Checking 状態を先に反映
    DennokoVersionChecker.CheckAsync(
        YourToolVersion.RepoOwner, YourToolVersion.RepoName, YourToolVersion.RepoBranch,
        YourToolVersion.VersionFilePath, YourToolVersion.Current, OnVersionChecked);
}

private void OnVersionChecked(DennokoVersionChecker.Result result)
{
    _versionResult = result;
    SessionState.SetBool(VerCheckDoneKey, true);
    SessionState.SetInt(VerCheckStateKey, (int)result.State);
    SessionState.SetString(VerCheckLatestKey, result.LatestVersion ?? string.Empty);
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

## エラーハンドリング方針

- 通信失敗・HTTP エラー・JSON パース失敗・`version` 欠落 → すべて `State.Error`。
- 指定ブランチで `State.Error` になった場合は **`"main"` にフォールバック**して再取得し、
  そこでも失敗して初めて `State.Error` を確定する（デフォルトブランチが master / main
  どちらでも動く）。`RepoBranch` を正しく設定すればフォールバックは発生せず 1 回で済む。
- `DennokoVersionChecker` は**例外を投げない**（内部で握り潰し `Debug.LogWarning` のみ）。
- 表示は `.dennoko-version-label--error`（`--dennoko-semantic-warning`）で警告色テキスト。

## 動作確認

1. ウィンドウを開き、タイトル横に `v1.0.0` が出る。
2. ローカル定数を下げる（例 `0.9.0`）と `--update` 色で「更新あり <最新版>」。
3. ローカル定数とリモートが一致で、バージョンのみ表示。
4. `RepoBranch` を実在しない値にしても、`main` に version.json があればフォールバックで
   バージョンが出る。owner/repo を不正値にする or オフラインなら `--error` 色の取得失敗テキスト、例外なし。
5. 言語切替で接尾辞テキストが切り替わる。
