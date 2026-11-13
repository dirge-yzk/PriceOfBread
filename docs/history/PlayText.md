# PriceOfBread.SimCore/PlayText.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-5 W 直後 `public static class PlayText`
````
/// <summary>★(410) 画面（App の法律タブ・階級の表）と Runner の play の報告が**同じ文言**を言うための単一ソース（00-HARN の残り・(407) の申し送り）。
/// 文言だけを持つ＝盤面を読まず書かない・計算の順序も係数も持たない（シムの軌道に触れない）。
/// 生成側と比較側で文字列が散らない作法は <see cref="Government.ReasonNoMandate"/> と同じ（散ると表示が嘘をつく）。</summary>
````

## L8-8 W 直後 `public static readonly string[] MilitancyPartNames = ["政治的疎外", "厭戦", "民族的地位", "飢餓"];`
````
/// <summary>闘争度の目標の 4 本の名前。順は <c>Pop.MilVoice</c>・<c>MilWar</c>・<c>MilNation</c>・<c>MilFamine</c>（Politics の目標の和 sum の 4 項と同じ並び）。</summary>
````

## L11-12 W 直後 `public static string GateLine(bool ok, double cost, string reason)`
````
/// <summary>法の段のホバーの 2 行目（「制定:可能（政治資本消費:n）」／在庫の門の否決／それ以外の理由）。
/// <paramref name="reason"/> は <see cref="Government.GateInfo(World, PolicyAxis, bool, int)"/> の理由。前提法・不可逆で止まった理由はここでなく <see cref="FormatPrereq"/>。</summary>
````

## L16 T `: reason == Government.ReasonNoLegitimacy ? $"制定:不可（政治資本消費:{cost * 100:F0}　{reason}）"`
````
// ★(419) 壁の一歩が正統性で止まった＝必要量の数字に〈正統性が不足〉を添える（在庫の不足と別の文言）
````

## L19-19 W 直後 `public static bool IsPrereqStop(bool ok, string reason) => !ok && reason.Length > 0 && !Go`
````
/// <summary>★(419) 前提法・不可逆で止まったか（＝政治の門〔賛成不足・在庫・正統性〕以外の理由が出ている）。法律タブ（<c>LawRowVM</c>）と Runner の報告（<see cref="NextStepLine"/>）が同じ 1 本を呼ぶ。</summary>
````

## L22-23 W 直後 `public static string FormatPrereq(string reason)`
````
/// <summary>前提未達の理由文を「◯◯:△△」レイアウトへ整形（作者指示2026-08-15）。
/// 「◯◯（△△）が前提」→「前提 ◯◯:△△」のパターン変換のみ。合わない文（プロレタリア独裁のみ 等）はそのまま返す＝安全側。</summary>
````

## L31 T `if (!state.Contains('（') && !state.Contains('）'))`
````
// 入れ子括弧は変換対象外（誤切り出しを避ける）
````

## L37-38 W 直後 `public static string NextStepLine(bool ok, double cost, string reason)`
````
/// <summary>次の一歩の門を 1 行で言う（Runner の報告・法の段のホバーの 2 行目と同じ分岐）。
/// 前提・不可逆で止まったとき（理由が在庫の門でも賛成不足でもない）は「制定:不可　<see cref="FormatPrereq"/>」、それ以外は <see cref="GateLine"/>。</summary>
````

## L44-45 W 直後 `public static string PolityRegimeJa(Polity? p) => p switch`
````
// ===== (435) 画面（App）と Runner の play の報告が同じ関数を呼ぶための単一ソース（テストプレイの道具の直し・出所＝初見の試しのテストプレイの報告 §1・§4）=====
// 盤面を読むだけ・書かない（文言と並べ方だけ）。App はこれを呼ぶ形に替えた（写しを置かない）。
````

## L47-47 W 直後 `public static string PolityRegimeJa(Polity? p) => p switch`
````
/// <summary>政体の和名（英語の enum 名をそのまま出さない）。9 政体を網羅。画面のヘッダー・終局のランキング・Runner の報告が同じ 1 本を呼ぶ。</summary>
````

## L62-62 W 直後 `public readonly record struct EndingStandingRow(int Order, string Name, double Score, Poli`
````
/// <summary>終局（暦が <see cref="Calendar.EndYear"/> に入った＝1949 年 12 月が終わった）の最終ランキングの 1 行。人口は内部単位（千人）のまま。</summary>
````

## L65-65 W 直後 `public static bool IsEnded(World w) => IsEnded(w.Year);`
````
/// <summary>終局を過ぎたか（画面は <c>Calendar.StartYear + 経過日 / 年の日数 &gt;= EndYear</c> で最終ランキングを出す。世界の暦は同じ年＝<c>w.Year &gt;= EndYear</c>）。</summary>
````

## L68-68 W 直後 `public static bool IsEnded(int year) => year >= Calendar.EndYear;`
````
/// <summary>★(456) 暦年が終局に入ったか（<c>year &gt;= Calendar.EndYear</c> の唯一の定義）。画面は表示暦の年〔<c>Calendar.StartYear + 経過日 / Calendar.DaysPerYear</c>〕を渡す（App は SimCore の暦を持たず経過日で数えるため）。</summary>
````

## L71-71 W 直後 `public static string WarResultWord(bool? won) => won is null ? "白紙講和" : won.Value ? "勝利" :`
````
/// <summary>★(456) 終戦の結果の語（画面の終わった戦争の一覧・終戦の報せ・play の報告が同じ 1 本）。勝ち true＝勝利・負け false＝敗北・白紙 null＝白紙講和。判定は <see cref="EndedWarRecord.VerdictOf"/>（記録の <see cref="EndedWarRecord.WonBy"/>）。</summary>
````

## L74-74 W 直後 `public static List<EndingStandingRow> EndingStandings(IEnumerable<World> worlds, World pla`
````
/// <summary>最終ランキング（国力 <see cref="NationalScore.Of"/> の降順）。画面の終局の表（<c>ShowEnding</c>）と Runner の報告が同じこの 1 本を読む。</summary>
````

## L80 T `var sc = NationalScore.ShareScores(wl, shares);`
````
// ★(449) 国力は全国分を 1 回で（国ごとに世界の総量を求め直さない）・★(605) 国×6 項の割合も同じ 1 回で受ける（鉄の産出の割合＝第 3 項）
````

## L81 T `var order = Enumerable.Range(0, wl.Count).OrderByDescending(k => sc[k]).ToList();`
````
// 安定な降順＝旧の OrderByDescending(Of) と同じ並び
````

## L87 T `PowerAffinity.PolityFromShares(w),`
````
// 導出政体＝ヘッダーの政体と同じ物差し（2026-08-21 作者裁定）
````

## L88 T `w.TotalPopulation, shares[order[i]][NationalScore.ScoreTerms.IronIndex], ReferenceEquals(w`
````
// ★(605) 最終の表の物差し＝工業化度でなく鉄の産出の世界の割合（国力の第 3 項と同じ量）
````

## L93-94 W 直後 `public static string ScoreBreakdownText(World player)`
````
/// <summary>自国の国力の内訳＋首位との差（画面の国名・地位のホバー <c>PlayerScoreTip</c> と Runner の報告が同じ 1 本）。
/// ★(449) 内訳は <see cref="NationalScore.Breakdowns"/> の 1 本（6 項の割合の点）。合計も首位も同じ <see cref="NationalScore.Scores"/> の値＝内訳と首位との差が同じ物差しを見る（(435) で見つけた混在を直した）。</summary>
````

## L97 T `var ws = NationalScore.NationWorldsOf(player);`
````
// 実在の国の世界の一覧（盤に居ない世界は自分だけ）＝外交のランクと同じ集合
````

## L108-108 W 直後 `public static List<ExplainLedger.Term> ExplainTerms(World w, string quantity)`
````
/// <summary>説明 API（#63）の内訳の項（|Δ|&lt;5e-4 を除き、寄与量の降順）。台帳が無い（説明モードオフ・非プレイヤー）なら空。画面の <c>ExplainTerms</c> と Runner の報告が同じ 1 本。</summary>
````

## L117-117 W 直後 `public static string SignedLine(double delta, string reason, System.Func<double, string> f`
````
/// <summary>内訳 1 行の文言（符号つき・理由）。画面の <c>ExplainTerms</c>／<c>Signed</c> と Runner が同じ書式。</summary>
````

## L121-121 W 直後 `public const string CapitalistClass = "資本家", BureaucratClass = "官僚", ArtisanClass = "職人", `
````
/// <summary>階級の層の名前に使う階級名（画面の上流・中流・下流の分け方）。</summary>
````

## L124-124 W 直後 `public static int ClassLayerOf(Pop p)`
````
/// <summary>上流（閉鎖階級・資本家）／中流（近代エリート・官僚・職人・商人）／下流 の 3 層（画面の識字・満足度・闘争度の内訳の分け方と同じ 1 本）。</summary>
````

## L130-130 W 直後 `public static List<(string Layer, double Mean)> ClassLayerMeans(World w, System.Func<Pop, `
````
/// <summary>3 層の人口加重平均（人口が 0 の層は出さない）。画面の POP タブの識字・満足度・闘争度の内訳（<c>LiteracyClassTerms</c> ほか）と Runner の報告が同じ 1 本。</summary>
````

## L140-140 W 直後 `public static string PriceLevelText(World w) => !w.MonetaryEnabled ? "—" : w.PriceLevel.To`
````
/// <summary>物価水準・為替の文言（金融の主フラグが無い世界は〈—〉）。画面の財政タブの金融と Runner の報告が同じ 1 本。</summary>
````

## L143-144 W 直後 `public static string EducationSpendingText(World w) => $"教育費（予算の割合） {w.Policy.EducationSpe`
````
/// <summary>★(452) 教育費の名乗り（App の財政の行と play の報告が同じ関数を呼ぶ）。教育費は額でなく〈予算の割合〉＝置いた率（<see cref="Policy.EducationSpendingTarget"/>）と実効の率（<see cref="World.EducationSpending"/>）。
/// 実効が国庫の買う率になるのは法と到達を通した後（Simulation の CompulsoryEduFunding）＝その率は別に出す（play の報告）。表示だけ＝シミュは動かさない。</summary>
````

## L147-147 W 直後 `public static string CoverText(World w)`
````
/// <summary>兌換カバー率（基礎貨幣÷広義貨幣）の文言。銀行が無い世界は〈—〉。画面の財政タブの金融と Runner の報告が同じ 1 本。</summary>
````

## L151-151 W 直後 `public static string PoliticalCapitalText(World w)`
````
/// <summary>政治資本の在庫の文言（現在 a ／ 最大 b・超過があれば添える）。画面の政治タブと Runner の報告が同じ 1 本。</summary>
````

## L155-155 W 直後 `return w.MandateSpent > bMax ? $"{aNow * 100:F0} / {bMax * 100:F0}（超過 {(w.MandateSpent - b`
````
// ★(407) 反証役 rf〔bv〕中#4：(405) の壁の一歩の後は S＞b＝在庫は 0 に張り付いて超えた分が見えなかった＝超えた分を出す
````

## L159-159 W 直後 `public static string PoliticalCapitalTip(World w)`
````
/// <summary>政治資本のホバー（b と実行度の材料のデータ行）。画面の政治タブと Runner の報告が同じ 1 本。</summary>
````

## L170-170 W 直後 `double rate = PoliticalQuantities.PoliticalCapitalRate(w);`
````
// ⚠2026-09-21（検証で発見）：旧は係数 MandateRefillQuarters（＝40）をそのまま出していた＝実行度を無視した嘘の数。満タンまでの実時間は 40/実行度。
````

## L172 T `double toFull = !(rate > 0) ? 0 : c.MandateRefillQuarters / rate * (bMax > 0 && w.MandateS`
````
// ★(407) S＞b（壁の一歩の後）は空から満タンの S÷b 倍
````

