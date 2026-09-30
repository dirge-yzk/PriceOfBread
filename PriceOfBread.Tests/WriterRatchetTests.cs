using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★ラチェット（思想⑫・docs/202608/20260828_悪い形の全数調査と休眠のトリアージ_部分完了.md §14-4・2026-08-29）：
/// 心理量（闘争度・生活水準・正統性・政治意識）への**書き手の数**を不変条件にする。
///
/// ── なぜ要るか ──────────────────────────────────────────────────────────
/// 闘争度には書き手が39箇所あり（力学1・運搬8・事件スパイク約17・緩和約10・初期化2）、
/// 「どこで動いたか」を誰も説明できない量になっていた。リファクタ（記憶＋noisy-or）を待たずに
/// **悪化だけは今日から止める**＝書き手を増やす変更はこのテストを割る＝増やすなら基準値の更新を**宣言**させる。
///
/// ── 計数規則 ────────────────────────────────────────────────────────────
/// SimCore の .cs 全走査で「`.量名` への代入・複合代入」を数える（`==` 比較は除外）。
/// ⚠この規則は設計docの実測（2026-08-28＝闘争度39・正統性15・政治意識1）を**正確に再現**する。
///   生活水準だけ doc は7＝オブジェクト初期化子（`new Pop { Satisfaction = … }`）込みの数え方＝
///   ここでは**プロパティ代入のみ**（初期化子は構築であって副作用ではない）＝4 を基準にする。
/// ⚠数を**減らす**変更（統合・削除）はそのまま通る（上限であって固定値ではない）＝ラチェットの向き。</summary>
public class WriterRatchetTests
{
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    private static int CountWriters(string member)
    {
        // 代入・複合代入のみ（`==` は除外）。`=>`（ラムダ/式形メンバ）も除外。
        // ★添字つきの書き込み（`.StateSpent[(int)TreasuryCat.Army] += …`）も数える（2026-09-28 段2-1＝StateSpent を区分の配列にした＝数え落とさない）。
        var rx = new Regex($@"\.{member}(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))");
        int n = 0;
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
            foreach (var line in File.ReadLines(f))
            {
                var code = line;
                int c = code.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) code = code[..c];   // コメント内の言及は数えない
                if (rx.IsMatch(code)) n++;
            }
        return n;
    }

    [Theory]
    // 基準値＝2026-08-29 実測（docs §14-4 のベースラインと一致）。**増えたら落ちる**。
    // 減らしたらこの数字を下げること（＝改善の宣言）。増やす変更は原則禁止＝
    // 新しい事件スパイクを足すのではなく、力学（生活苦・政治距離）か記憶（§14-7）へ流すこと。
    // ★2026-08-29 段1実施＝運搬8箇所の**式**を Military.BlendCarry 1本へ集約（厳密恒等リファクタ・clampの有無はsiteごとに保存）。
    //   ⚠計数は「代入サイト」なので39のまま＝この数が減るのは段2/6/7（スパイクの削除・記憶への置換）から。
    // ★2026-09-08 基準値の更新（＝宣言・F3b蜂起doc §3-4 段2）：闘争度 39→40・正統性 15→16。
    //   ⚠**新しい事件スパイクではない**。内戦一元化（CivilWarAsWar=1.0）で内戦の主経路が War へ移ったのに、
    //   その再併合サイト（MultiNationSim.ApplyReintegrations）にだけ後始末が無く、旧経路（Politics.cs の
    //   ProcessCivilWar 決着）が既に書いている**同じ2つの規則**（返還州の闘争度×ReformMilitancyRelief／
    //   正統性−RevoltSuppressLegitCost）が live 経路で抜け落ちていた＝規則を増やしたのではなく、
    //   同じ規則の**二つ目の経路**に同じ1行を置いた（RevoltAftermathOnWarPath=0 で休眠）。
    //   ★この+2は旧経路（首都発の単国シム経路）を畳む段6で消える＝そのとき 39/15 へ戻すこと。
    // ★2026-09-11 基準値の更新（＝宣言・要求は法の経路の点火）：闘争度 40→**38**。書き手が2つ消えた＝
    //   F1 州一揆の枝の緩和 ×ReformMilitancyRelief（作者裁定「弾圧は鎮めない・譲歩が鎮める」＝鎮圧は掛けない・譲歩の緩和は施行の時点の Step 先頭の1行へ）と、
    //   別軸譲歩（本丸を拒んで副次要求を直書きする旧枝）の緩和＝旧枝ごと物理削除。正統性 16→**15**＝同日の実測（この点火は正統性の書き手を触っていない＝基準が実数より緩かった）。
    // ★2026-09-28 基準値の更新（＝宣言・処理の一元化 段2 束2＝戦後処理の1本）：闘争度 38→**35**・正統性 15→**14**（実数は着手前 36・15）。
    //   内戦の決着を Politics.SettleRevolt の1本へ畳んだ＝旧経路の鎮圧の緩和（全POP）と戦争の形の休眠の後始末（返還州の緩和・正統性の減算）が1行の緩和になり、
    //   休眠の後始末の正統性の減算は旧経路の鎮圧の行に無い＝持ち込まなかった（係数 RevoltAftermathOnWarPath ごと削除）。
    // ★2026-09-28 基準値の更新（＝宣言・処理の一元化 段2 束3＝旧経路の内戦〔第二の戦争エンジン ProcessCivilWar〕と単一州の分離の削除）：
    //   闘争度 35→**32**（旧経路の分離戦の決着の緩和2本〔独立・鎮圧〕と国家分裂の両陣営の緩和1本）・正統性 14→**13**（内戦の消耗 CivilWarLegitimacyDrain）。
    //   首都の蜂起は戦争の形（決着の緩和は SettleRevolt の1本・戦死と民間人の死は対外戦争と同じ1本）。
    [InlineData("Militancy", 32)]      // 力学1・運搬8（式は1本化済み）・事件スパイク約17・緩和約4・初期化ほか（内戦の決着の緩和は SettleRevolt の1本）
    [InlineData("Satisfaction", 4)]    // ⚠プロパティ代入のみ（初期化子は構築＝対象外）
    [InlineData("Legitimacy", 13)]
    [InlineData("Consciousness", 1)]   // ★これが手本（書き手1本＝力学だけ）
    public void 心理量への書き手は増やさない(string member, int baseline)
    {
        int n = CountWriters(member);
        Assert.True(n <= baseline,
            $"{member} への書き手が {n} 箇所（基準 {baseline}）＝**増えている**。" +
            "事件スパイクを足すのではなく、力学（生活苦・政治距離）か記憶（docs/202608 §14-7）へ流すこと。" +
            "どうしても必要なら基準値を上げて理由をこのファイルに書く（＝宣言）。");
    }

    [Theory]
    // ★2026-09-28 支払いの一元化 段1-0（作者裁定 F4・計画 docs/202609/20260926/20260926_外部レビューの検討.md §10-12-8-2）：
    //   お金の口座と世界の台帳への**書き手の数**を不変条件にする。計数規則は上と同じ（代入・複合代入・コメントを除く・初期化子は数えない）。
    //   基準値＝2026-09-28 の実測（d3dc133）。段1〜段3で支払いを1本の移し方へ寄せるたびに下げる（＝宣言）。増やす変更は、1本の移し方を通すこと。
    // ★2026-09-28 段1-1（死んだ支払いの枝の削除）：Savings 94→91・StateSpent 26→25・TradeFlow 34→33・Paid 10→9・Earned 14→11
    //   ＝建設工賃の旧経路2（資本家・国家）・植民地の採取決済（TradeFlow 1・植民地の蓄え 1・植民地交易益の傾斜配分 Earned 3）。
    // ★2026-09-28 段1-3（国庫の口を1本へ）：Treasury 2→0＝外交層の財布 Power.Treasury への代入（同期窓の冒頭・助走明けの同期）を撤去。
    //   国を持つ Power の国庫は World.Treasury（private set）＝口は World.Pay だけ。Power.Treasury は抽象標的の財布（構築時の初期化子だけが書く）。
    // ★2026-09-28 段1-2（按分の本体を1本へ）：Savings 91→83＝World.Distribute（旧 Fiscal.Distribute）へ畳んだ複製＝Colonial.DistributeToPops 2・
    //   World.DistributeToVoters 2・GiveGold 1・信用の利息の配当 1・海上保険の受け手側 2。
    // ★2026-09-28 段1-5（重複の削除）：Savings 83→79＝Military.MoveCohort の3枝（徴兵・復員・通常）のお金の運搬を末尾の1か所へ。
    // ★2026-09-28 E6 の2歩目（基礎貨幣が負の国の返せない与信の償却）：数は変えない。CumulativeFiatIssued 3 のまま＝償却の非常発行は
    //   Simulation.BackstopNegativeSavings の末尾の1行（銀行の無い世界の③と同じ行）。Loans 6 のまま＝償却の1行（+1）と、
    //   Credit.Step の与信の増減（投資家の枝と全POPへ注入する枝の同じ2行）を1行へ（−1）。
    // ★2026-09-29 段2-2・2-3（保存の穴と他人から取る・端数と式の結合順＝処理の一元化 段4a）：Savings 79→**47**・CumulativeTradeFlow 33→**31**（＝宣言）。
    //   移し方の1本 World.Transfer（旧 MultiNationSim.TransferToPops を World へ移して一般化・+1）へ畳んだ＝CapitalMarket.TakeGold／GiveGold の本体・関税と補助
    //   （旧 InternationalMarket.CollectTariff／PaySubsidy を削除）・港湾料（旧 CollectPortFee を削除）・運河の出資・招聘の支度金・対外借入の貸し手の引き落とし。
    //   按分の本体 World.Distribute（符号つき・微小な額も配る）へ畳んだ＝与信の注入と回収（旧 Credit.InjectMoney を削除）・貿易の仲介料と残余・国債の持ち手・
    //   窃盗・年金・恐怖政治・十分の一税・港の建設労務・生産者のいない在庫の代金（輸送の買い付け）。再点火の代金は月内の仮勘定（Paid）へ（建設の材料と工賃の Paid の2行を1行へ＝Paid は 9 のまま）。
    //   銀行の無い世界の底打ち（最富裕→全員から取る・作者裁定 Q11 で撤去）。CumulativeFiatIssued は3のまま（非常発行の行＝負の国の与信の償却だけになった）。
    // ★2026-09-29 段3-1（貿易＝処理の一元化 段4b）：Savings 47→**43**・CumulativeTradeFlow 31→**30**（＝宣言）。Earned は11 のまま。
    //   貿易の純流入を国の全POP の貯蓄比に配る Apply（TradeFlow 1）・輸出代金の持ち主への直書き（旧 PayExportToProducers・Savings 1）・輸送財の買い手の引き落とし2行（Transport）を削除＝
    //   輸入代金・関税・運賃・売り手の受取・仲介料・運賃の受け手は全て移し方の1本（World.Transfer）へ。按分の本体 World.Distribute の書き込みを1行へ（端数は最後の受け手＝同じ式・Savings −1）＋
    //   所得（Earned）も同じ行で書く引数（月次の精算の後の受け取り＝Earned +1）・十分の一税の所得の別の行を畳んだ（Earned −1）。
    // ★2026-09-29 高速化の段 S3b（輸入の為替手形）：Savings 43→**42**・HouseholdLoans 4→**3**（＝宣言）。家計の与信の貸しと返しを Bank.Lend の1本へ
    //   （消費者信用の貸し・返済規律の返し・手形の振り出しと決済＝旧は消費者信用と返済規律がそれぞれ Savings と HouseholdLoans を直に書いた）＋
    //   POP が国をまたぐときの与信の移し替えを Bank.Carry の1本へ（割譲・建国・再併合・越境移民・招聘）。⚠Bank の中の書き込みは `.` の無い形＝数の規則の外（1本の移し方そのもの）。
    // ★2026-09-30 段3-2（財を買わない国家支出）：Savings 42→**38**・CumulativeTradeFlow 30→**27**（＝宣言）。植民地投資・捕囚市場・港の建設の代金と拿捕を
    //   ④の持分（InternationalMarket.PaySales＝移し方の1本）へ＝投資家の貯蓄の直の引き落とし（植民地投資の2行・港の1行）・捕囚市場の投資家への直の払い・
    //   捕囚市場と拿捕の台帳の手書き（3行）が消えた。Power.Treasury（抽象標的の財布）は欄ごと削除（代入0のまま）。
    // ★2026-09-30 段3-3（対外の民間請求）：CumulativeTradeFlow 27→**14**・StateSpent 24→**23**（＝宣言・StateSpent は着手前の実数が23＝基準が緩かった）。
    //   国家間の移転の手書きの台帳（貢納・賠償・通過港と運河の通行料・運河の配当・巨大建造物の外資の拠出と利回り＝13行）を移し方の1本（World.Transfer＝国境をまたげば為替で換算）へ。
    // ★2026-09-30 段3-4・3-5（POP の義務と鋳造＝処理の一元化 段4d）：Savings 38→**33**（＝宣言）。POP の義務（税・十分の一税・領主の取り分）を POP ごとの1本の率へ
    //   （税の後の残りから取った十分の一税の走査 Privileges.CollectTithe と領主の取り分の払い手の行が消えた）・鋳造の代金を④の持分（PaySales＝移し方の1本）へ
    //   （持ち主・労働〔MintWageShare〕・持ち込みの商人への直の払いの3行が消えた）・準備の売却を1本へ（平価の売りの買い手の直の引き落としが消えた）。
    // ★2026-09-30 段4f（コンパイラの番人の前段＝作者裁定 Q13）：Savings 33→**23**・CumulativeTradeFlow 14→**13**（＝宣言）。
    //   人と動くお金の運搬（州の間の移民と入植・社会移動・徴兵と復員・州の中の移動・越境移民・招聘の6か所が写していた「貯蓄・債務・保有」の2行）を
    //   Pop の中の1本（Pop.Carry）へ（12行→2行）・分離建国の国庫の分与（国庫と国境の出入りの台帳の手書き2行）を移し方の1本（World.Transfer）へ。
    //   ⚠Pop.Savings の private set はまだ入れられない＝残る23行のうち18行は相手の側が手書き（徴税の1本の率・利払い・借入・切り下げ・没収・窃盗・与信・植民地など）で、
    //   移し方の1本へ寄せると浮動小数の演算の順が変わる（恒等にならない）＝実装ログ (126) の申し送り。
    [InlineData("Savings", 23)]               // POP の貯蓄（最後は Pop の中の3つ＝按分・運搬 Pop.Carry・月末の精算へ閉じる）
    // ★2026-09-28 段2-1（国家の支払い率）：StateSpent 25→24＝区分つきの配列へ（添字つきの書き込みを数える）・月次のゼロ化は Array.Clear（代入でない）。
    //   FiscalFundingRatio（国家の支払い率 r）を足した＝書き手は月初の1か所（Simulation）＋ランナーの盤面の復元（GovernanceSnapshot）の2つ＝増やさない。
    [InlineData("StateSpent", 23)]            // 州の未精算の国家支出（区分つき＝TreasuryCat の配列）
    [InlineData("FiscalFundingRatio", 2)]     // 国家の支払い率 r（月初の1か所＋盤面の復元）
    [InlineData("CumulativeTradeFlow", 13)]   // 世界の台帳・国境をまたいだ出入り（移し方の中の1か所へ畳む）
    [InlineData("CumulativeFiatIssued", 3)]   // 不換の発行と回収（2）＋非常発行（1＝負の国の与信の償却だけ・銀行なしの底打ちは 2026-09-29 段2-2 で撤去）
    [InlineData("CumulativeGoldMinted", 2)]   // 鋳造と準備の売却
    [InlineData("Loans", 6)]                  // 銀行の与信（企業・損失の負債化・負の国の償却）
    [InlineData("HouseholdLoans", 3)]         // 銀行の与信（家計）＝Bank.Carry の受け手・再併合の合算・盤面の復元（貸しと返しは Bank.Lend の1本）
    [InlineData("Paid", 9)]                  // 月内の仮勘定（払い）
    [InlineData("Earned", 11)]                // 月内の仮勘定（受け取り）
    [InlineData("Treasury", 0)]               // 外交層の財布への代入（World.Treasury は private set＝構造で閉じている・Power.Treasury は 2026-09-30 に欄ごと削除）
    public void お金への書き手は増やさない(string member, int baseline)
    {
        int n = CountWriters(member);
        Assert.True(n <= baseline,
            $"{member} への書き手が {n} 箇所（基準 {baseline}）＝**増えている**。" +
            "支払いは1本の移し方を通すこと（計画 §10-12）。" +
            "どうしても必要なら基準値を上げて理由をこのファイルに書く（＝宣言）。");
    }

    /// <summary>★国庫の口（World.Pay）を World の外から呼ぶ行の数（2026-09-30 段4f・作者裁定 Q13 の前段）。
    /// World.Pay は相手を名乗らない片側の記帳＝外からは移し方の1本（World.Transfer）を使う。最後は Pay を private にしてコンパイラで閉じる（Q13）が、
    /// 残る行は相手の側が手書きで、移し方の1本へ寄せると浮動小数の演算の順が変わる＝それまでは数を上限にする（＝宣言・減らすのは通る・増やすと落ちる）。
    /// 走査＝SimCore・App・Runner の .cs（obj/bin を除く・World.cs を除く・コメントは数えない）。
    /// 2026-09-30 段4f＝16→**10**：福祉（国庫→大衆）・年金（国庫→老年）・国営持分の取り分（区分だけ動く）・分離建国の国庫の分与を移し方の1本へ（全長の10種の報告が段4d と全行一致）。
    /// 残る10行＝徴税の1本の率（Fiscal・国庫の側）・利払い（2）・減債（2）・借入（Fiscal）・再併合の国庫の合算（MultiNationSim）・恐怖政治の没収の残り（Politics）・
    ///   切り下げ（Simulation）・国家の仮勘定の月末の精算（Simulation）。</summary>
    [Fact]
    public void 国庫の口を外から呼ぶ行は増やさない()
    {
        const int declared = 10;
        var rx = new Regex(@"\.Pay\(");
        string root = Path.GetDirectoryName(SimCoreDir())!;
        char sep = Path.DirectorySeparatorChar;
        var hits = new System.Collections.Generic.List<string>();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.App", "PriceOfBread.Runner" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains($"{sep}obj{sep}") || f.Contains($"{sep}bin{sep}")) continue;
                if (dir == "PriceOfBread.SimCore" && Path.GetFileName(f) == "World.cs") continue;
                int ln = 0;
                foreach (var line in File.ReadLines(f))
                {
                    ln++;
                    var code = line;
                    int c = code.IndexOf("//", StringComparison.Ordinal);
                    if (c >= 0) code = code[..c];
                    if (rx.IsMatch(code)) hits.Add($"{Path.GetFileName(f)}:{ln}");
                }
            }
        Assert.True(hits.Count <= declared,
            $"World.Pay を外から呼ぶ行が {hits.Count}（基準 {declared}）＝**増えている**：{string.Join(", ", hits)}。" +
            "国庫の出入りは移し方の1本（World.Transfer＝相手を名乗る）を通すこと。どうしても必要なら基準を上げて理由をここに書く（＝宣言）。");
    }

    // ★2026-09-30（作者裁定「計算量の予算」＝推奨で採択）：**計算量の番人**＝1種（seed 1）・40四半期（助走を含む＝バッチと同じ道
    //   SelfPlayMatch.Run(lean)）の処理ごとの「見た件数」（CostLedger）が宣言した表を超えないこと。書き手の数の番人と同じく、
    //   **件数が増えるコミットは表を上げて理由をここに書く（＝宣言）**＝予算の超過はどこで払ったかを実装ログとコミットに書く。
    //   ・許しの幅 +2%＝軌道の小さな揺れ（係数・式の変更で軌道が動くと件数も少し動く）で毎回表を書き換えずに済む幅。
    //   ・表より 10% を超えて下がったら落ちる＝表を下げる（＝改善の宣言）。緩んだ表を残すと、後で同じだけ増えても番人が鳴らない。
    //   ⚠40四半期では時代とともに育つ費用（従属10倍・成約2.5倍・工事中2倍）が見えない＝全長の件数はバッチの測り（measure.sh の <出力>.cost）で見る。
    //   ⚠件数は診断（セーブに載らない・軌道を変えない）＝下の「セーブに載らない」の番人と対。
    private const double CostSlackUp = 0.02, CostSlackDown = 0.10;
    private static readonly Lazy<long[]> Cost40 = new(() => SelfPlayMatch.Run(1, 40, null, lean: true).Cost ?? new long[CostLedger.Count]);

    [Theory]
    // 表＝2026-09-30 の実測（宣言・計器なしの全長の報告と同じ道）。
    // ★2026-09-30 表の更新（＝改善の宣言・計算量の予算 第1回＝恒等の手）：
    //   価格の財の歩 22,505,040→15,536,007（死んだ財を一覧で回さない＝Region.LiveGoods）・建設 19,761,052→28,231（工事中の一覧＝Region.BuildSites）・
    //   消費 97,521,840→29,300,830（必要の無い財の POP のループを飛ばす）・請求 9,037,980→9,688（国営持分の請求の一覧＝Region.StateSites）・
    //   覇権の判定 7,460,493→1,706,531（陣営戦力の閾値は同盟の候補が居るときだけ・従属の離反は安い条件から）。他の10本は1件も変わらない（軌道が同じ）。
    // ★2026-09-30 表の更新（＝改善の宣言・計算量の予算 第2回＝近似の手）：軌道が変わる（近似）＝15本とも今の件数へ置き直した。
    //   清算の着値の充填 44,882,844→35,263,928（−21.4%＝月の清算で不足だけで起きた行のうち、前の走査で成約せず自州価格が上がっていない行を眠らせる）・
    //   価格の財の歩 15,536,007→14,629,209（−5.8%＝供給も在庫も無く張り付いた財は不動点に着いた旬から月の残りを眠る）・日次の写像の回 40,223,878→39,615,736（−1.5%）。
    //   ほかは軌道の違いの揺れ（建設 +0.6%・清算の買い手行 −0.6%・覇権の判定 −2.9%・請求 −0.1%・消費 −0.01%・残り7本は同じ）。
    // ★2026-09-30 表の更新（(122) 合流の前段＝線2〔(118) 幽霊の国の根・(119) 助走は法を動かさない〕を載せた木＝軌道が動いた分の宣言・新しい処理は無い）：
    //   建設 28,392→13,571・請求 9,682→4,710（下がった＝表を下げる）・覇権の判定 1,657,020→2,300,741（+39%＝開幕の法と同盟の軌道が変わり、
    //   同盟の候補が居る月が増えた＝処理の形は同じ）。推定の単位では 40四半期で +6.7ms（単価 0.01035µs）＝全長の予算の内／超過は合わせた main の10種の .cost で見る（実装ログ (122)）。
    // ★2026-09-30 表の更新（(122) 合流＝線1〔(117)(120) 支払いの段3-2・3-3〕を線2〔(118)(119)〕と計算量の予算 第2回〔(121)〕の載った main へ＝軌道が動いた分の宣言・
    //   新しい処理は無い＝15本とも合わせた木の件数へ置き直した）：建設 13,571→22,986・請求 4,710→7,908（国営持分の工事と植民地・港・運河の代金の軌道）・
    //   清算の着値の充填 35,263,928→36,674,517（+4.0%）・覇権の判定 2,300,741→2,459,718（+6.9%）・価格の財の歩 14,629,209→14,830,130・日次の写像の回 39,615,736→39,322,063・
    //   信用 33,138→33,237・外交：国×月 9,906→9,939・共有層 3,682→3,693（40四半期で国が1つ多い）・ほかは揺れ。全長の推定と予算の上限は合わせた main の10種の .cost（実装ログ (122)）。
    // ★2026-09-30 表の更新（＝改善の宣言・財の削減の回 (123)＝段1：138→127財・深さの撤去 Q2）：財の数が減り軌道も変わる＝15本とも今の件数へ置き直した。
    //   価格の財の歩 14,830,130→14,149,727（−4.6%）・日次の写像の回 −5.1%・消費 −2.4%・生産 −6.6%・清算の買い手行 −7.7%・清算の着値の充填 36,674,517→31,689,816（−13.6%）・
    //   建設 −6.9%・請求 −4.8%・覇権の判定 −1.4%・信用 +0.1%・外交の国×月 +0.2%・共有層 +0.1%・POP の3本は同じ。全長の件数と推定は measurements の b10 の g123 の .cost。
    // ★2026-09-30 表の更新（(125) 州の空洞化の根＝工業の引力を外す・残る人の土地の持ち分・国内の移民の受け入れの上限＝軌道が動いた分の宣言・新しい計数の処理は無い）：
    //   覇権の判定 2,459,718→2,757,936（+12%＝同盟の候補が居る月が増えた＝処理の形は同じ）。ほかの14本は許しの内（+2%／−10%）＝表は動かさない。
    //   全長の推定は州が空にならない分だけ増える（清算の着値の充填・日次の写像の回＝人の居る州の市場が残る）＝実装ログ (125) の .cost（10種）。
    // ★2026-09-30 表の更新（(127) 歳入の構成を史実から＝所得税の導入は払えない請求から・専売の梯子は史実の物品税の財から・漏れの最大 0.6＝軌道が動いた分の宣言・新しい計数の処理は無い）：
    //   覇権の判定 2,757,936→2,260,272（−18%＝下がった＝表を下げる・同盟の候補が居る月が減った＝処理の形は同じ）。ほかの14本は許しの内＝表は動かさない。
    // ★2026-09-30 表の更新（(128) 合流の前段＝線2 の (125)(127) を財の削減 (123) の載った main へ載せ直した木＝軌道が動いた分の宣言・新しい処理は無い）：
    //   建設 21,398→18,466（−13.7%）・請求 7,532→6,397（−15.1%）・覇権の判定 2,424,757→2,137,387（−11.9%）＝10% を超えて下がった＝表を下げる。ほかの12本は許しの内。
    // ★2026-09-30 表の更新（(124) 段4d＝支払いの段3-4・3-5＝軌道が動いた分の宣言・新しい処理は無い）：覇権の判定 2,459,718→2,825,300（+14.9%＝同盟の候補が居る月の軌道・
    //   処理の形は同じ＝(122) の +39%/+6.9% と同じ型）。連合維持費と駐留の俸給の請求の担い手は州ごとの産出の和（LastProduced）を読む＝生産者を走査しない（新しい計数は無い）。
    //   建設 21,518・請求 7,446 は表の許しの内（−6%）。推定の単位では 40四半期で +3.8ms（単価 0.01035µs）＝全長は10種の .cost（実装ログ (124)）。
    // ★2026-09-30 表の更新（(128) 合流＝線1 の (124)(126) を線2 と財の削減の載った main へ載せ直した木＝軌道が動いた分の宣言・新しい処理は無い）：
    //   覇権の判定 2,137,387→2,634,247（+23.2%＝(124) 単独の +14.9% と同じ型＝同盟の候補が居る月が増えた・処理の形は同じ・単価 0.01035µs＝40四半期で +5.1ms）。ほかの14本は許しの内。
    // ★2026-09-30 表の更新（＝改善の宣言・(130) 計算量の予算＝清算の眠りの切り分け：月の清算は潜在量を書かない＋買い手の容量が尽きた行は回さない）：
    //   清算の着値の充填 31,689,816→27,104,468（−14.5%＝月の清算で成約できない行〔買い手の州の容量が尽きている〕が売り手を回らない）・
    //   覇権の判定 2,634,247→2,348,562（−10.8%＝軌道の分＝同盟の候補が居る月が減った・処理の形は同じ）。ほかの13本は許しの内（+2%／−10%）＝表は動かさない。
    //   全長の推定 281.1→269.3 秒（10種の .cost＝実装ログ (130)）。
    // ★2026-09-30 表の更新（＝改善の宣言・(132) 計算量の予算＝張り付いた財の閉じた式：帯の端の不動点の近くにいる旬を日次の写像を回さず1回の式で進める）：
    //   日次の写像の回 37,318,732→25,283,828（−32.2%）。覇権の判定 2,348,562→2,617,303（+11.4%＝軌道の分＝同盟の候補が居る月が増えた・処理の形は同じ・
    //   単価 0.01035µs＝40四半期で +2.8ms・全長の10種では 156.0M→147.7M＝−5.3%）。ほかの13本は許しの内＝表は動かさない。全長の推定 269.3→255.1 秒（実装ログ (132)）。
    // ★2026-09-30 表の更新（(129) 金鶏樹皮を戻す＝財 127→128・キニーネを本国の工場へ・単離の前の「樹皮の粉末」＝軌道が動いた分の宣言）：
    //   覇権の判定 2,634,247→2,843,437（+7.9%＝同盟の候補が居る月の軌道・処理の形は同じ＝(124)(128) と同じ型・単価 0.01035µs＝40四半期で +2.2ms）。ほかの14本は許しの内（+2%／−10%）。
    //   ⚠財が1つ増えた分（価格の財の歩・生産者×月〔全ての州に「樹皮の粉末」の生産者〕・清算の着値の充填）は40四半期では許しの内＝全長は10種の .cost（実装ログ (129)）。
    //   全長の推定は上限の許しを超える＝線1 の短縮で払う見込み（合流の担当が最終の木で確かめる）。
    // ★2026-09-30 表の更新（(131) 飢えで州が抜けきる型の根＝出生と死を同じ単位で・飢えの超過死亡は平常の死亡率の上乗せ・局地飢饉の相対の不足の死を削除＝軌道が動いた分の宣言・新しい処理は無い）：
    //   覇権の判定 2,843,437→2,381,797（−16.2%＝10% を超えて下がった＝表を下げる・同盟の候補が居る月の軌道・処理の形は同じ）。ほかの14本は許しの内（+2%／−10%）。
    //   ⚠全長では植民地の州が空にならない分だけ日次の写像の回が増える（人の居る州の市場が残る＝(125) と同じ型）＝全長は10種の .cost（実装ログ (131)）。
    [InlineData(CostLedger.PriceGoods, 14149727L)]
    [InlineData(CostLedger.PriceDailyRuns, 25283828L)]
    [InlineData(CostLedger.ConstructionSeen, 18466L)]
    [InlineData(CostLedger.CreditSpans, 33282L)]
    [InlineData(CostLedger.ConsumePopGoods, 28595736L)]
    [InlineData(CostLedger.ProducerMonths, 8440020L)]
    [InlineData(CostLedger.ClaimProducers, 6397L)]
    [InlineData(CostLedger.MonthlyPops, 706680L)]
    [InlineData(CostLedger.ClearingRows, 5185642L)]
    [InlineData(CostLedger.ClearingFills, 27104468L)]
    [InlineData(CostLedger.DiploPowerMonths, 9954L)]
    [InlineData(CostLedger.DiploHegemonScans, 2617303L)]
    [InlineData(CostLedger.QuarterStartPops, 235560L)]
    [InlineData(CostLedger.QuarterEndPops, 235560L)]
    [InlineData(CostLedger.SharedNationQuarters, 3698L)]
    public void 計算量の見た件数は宣言した表を超えない(int item, long declared)
    {
        long n = Cost40.Value[item];
        string name = CostLedger.Names[item];
        Assert.True(n <= declared * (1 + CostSlackUp),
            $"計算量「{name}」が {n} 件（宣言 {declared}・許し +{CostSlackUp:P0}）＝**増えている**。" +
            "増やすなら表を上げ、どこで払ったか（他の処理を削った分）をこのファイル・実装ログ・コミットに書く（＝宣言）。" +
            "新しい処理なら docs の「処理の刻みの表」に行を足し、刻みの根拠を書く。");
        Assert.True(n >= declared * (1 - CostSlackDown),
            $"計算量「{name}」が {n} 件（宣言 {declared}）＝{CostSlackDown:P0} を超えて**下がった**＝表を {n} へ下げて理由を書く（＝改善の宣言）。");
    }

    [Fact]
    public void 計算量の台帳はセーブに載らない()
    {
        // 台帳は作業領域（[NonSerialized]）＝SaveGame が辿らない。ロードした盤面の台帳は空（null）から数え直す＝件数がセーブの中身と往復の一致に触れない。
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        Assert.Contains(sim.Nations, n => n.World.CostOrNull is { } c && c.Any(v => v > 0));   // 前提＝数えている
        Assert.True(sim.CostTotals().Sum() > 0);
        using var ms = new MemoryStream();
        SaveGame.Save(sim, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        Assert.All(sim2.Nations, n => Assert.Null(n.World.CostOrNull));
        Assert.Null(sim2.Diplomacy.CostOrNull);
        Assert.Null(sim2.Market.CostOrNull);
        Assert.Equal(0L, sim2.CostTotals().Sum());
    }

    // ★2026-09-30 段0-1（財の削減の回・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 の段0-1）：**数値の財 id を書かない**。
    //   財の id は Catalog の定数（IronOreId〜RefrigeratorId）だけで書く＝財を畳む・id を振り直すときは定数の値を直すだけで全ての読み手が追いつく
    //   （数値で書いた読み手は、振り直しの後に黙って別の財を読む＝型の統合の教訓）。基準 0＝1件でも書いたら落ちる。
    //   数える形＝レシピの投入 I(n,…)・new Input(n,…)・GoodId の比較と代入・Markets／RefPrices／Complexity／Catalog.Goods の添字・id is n・id == n・
    //   名前が …Goods／…GoodIds の配列の初期化子・辞書の初期化子 { n, "…" }・財を2番目の引数に取る呼び出し（BuildFactory(0, n) など）・
    //   財の switch（Catalog.cs と Policy.cs）の or n と腕の先頭の n。対象＝SimCore・Runner・App・Tests（このファイルの正規表現の文字列は数えない＝文字列は消してから読む）。
    //   0 と 1（鉄鉱石・石炭）は数えない＝単一州の最小シナリオの番号や添字の 0／1 と見分けられない（この2財は残す財＝振り直しでも番号が変わらない）。
    //   対象外＝単一州の最小シナリオ（「◆玩具の世界ここから／ここまで」の間＝Simulation.cs の最小シナリオ・ChainTests.cs＝シナリオの中だけの番号で Catalog の財 id ではない）。
    [Fact]
    public void 数値の財idは書かない()
    {
        var pats = new[]
        {
            new Regex(@"\bI\(\s*(\d+)\s*,"),
            new Regex(@"new Input\(\s*(\d+)\s*,"),
            new Regex(@"\bGoodId\s*(?:==|!=|=(?![=>]))\s*(\d+)\b"),
            new Regex(@"(?:Markets|RefPrices|Complexity|Catalog\.Goods)\[\s*(\d+)\s*\]"),
            new Regex(@"\bid\s+is\s+(\d+)"),
            new Regex(@"\bid\s*==\s*(\d+)"),
            new Regex(@"\{\s*(\d+)\s*,\s*"""),   // 辞書の初期化子 { n, "名前" }
            new Regex(@"\b(?:BuildFactory|CanBuildFactory|CanSellFactory|SellFactory|FindFactory|SetStateTarget|SetSubsidyRate|SetSubsidy|CanSubsidize|OutputOf|PaySales|GoodOf)\([^,()]*,\s*(\d+)\s*[,)]"),   // 財を2番目の引数に取る呼び出し
            new Regex(@"\bSelectProdGood\(\s*(\d+)\s*\)"),
            new Regex(@"\b(?:SetFactoryTarget|NationFactoryUnits)\(\s*(\d+)\s*[,)]"),   // 財を1番目の引数に取る呼び出し
        };
        var switchPats = new[] { new Regex(@"\bor\s+(\d+)\b"), new Regex(@"^\s*(\d+)\s*(?:or\b|=>)") };
        var arr = new Regex(@"(?:\w*(?:Goods|GoodIds|Resources)\s*=\s*|ColonyDto\([^,()]*,\s*)\[([^\]]*)\]");   // 財の一覧・賦存（Resources・ColonyDto）の配列
        var num = new Regex(@"(?<![\w.])(\d+)(?![\w.])");
        var str = new Regex(@"""(?:[^""\\]|\\.)*""");
        string root = Path.GetDirectoryName(SimCoreDir())!;
        var hits = new System.Collections.Generic.List<string>();
        foreach (var proj in new[] { "PriceOfBread.SimCore", "PriceOfBread.Runner", "PriceOfBread.App", "PriceOfBread.Tests" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(root, proj), "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
                bool goodSwitch = Path.GetFileName(f) is "Catalog.cs" or "Policy.cs";
                bool toy = false;
                int ln = 0;
                foreach (var line in File.ReadLines(f))
                {
                    ln++;
                    if (line.Contains("◆玩具の世界ここから")) toy = true;
                    if (line.Contains("◆玩具の世界ここまで")) { toy = false; continue; }
                    if (toy) continue;
                    var code = line;
                    int c = code.IndexOf("//", StringComparison.Ordinal);
                    if (c >= 0) code = code[..c];
                    code = str.Replace(code, "\"\"");
                    foreach (var p in goodSwitch ? pats.Concat(switchPats) : pats)
                        foreach (Match m in p.Matches(code))
                            if (int.Parse(m.Groups[1].Value) >= 2) hits.Add($"{Path.GetFileName(f)}:{ln}: {m.Value.Trim()}");
                    foreach (Match m in arr.Matches(code))
                        foreach (Match n in num.Matches(m.Groups[1].Value))
                            if (int.Parse(n.Groups[1].Value) >= 2) hits.Add($"{Path.GetFileName(f)}:{ln}: {m.Value.Trim()}");
                }
            }
        Assert.True(hits.Count == 0,
            $"数値の財 id が {hits.Count} 件＝Catalog の定数（…Id）で書くこと（財を畳む・振り直すと数値は黙って別の財を読む）：" + string.Join(" ／ ", hits.Take(12)));
    }

    // ★2026-09-30 段0-3（財の削減の回）：**解禁の onset は表から読む**＝Catalog の解禁の表（レシピ単位の1枚・Catalog.RecipeOnset）の外で
    //   Producer.TechOnset に正の数値を直に書かない（旧＝Simulation の代替のレシピの注入に数値で散っていた）。0（無ゲート）は数えない。
    //   係数の onset（蒸気タービン・黄鉄鉱法）は係数の名前で書く＝数値ではないので数えない。
    [Fact]
    public void 解禁のonsetは表から読む()
    {
        var rx = new Regex(@"\bTechOnset\s*=\s*(\d+(?:\.\d+)?)\b");
        var hits = new System.Collections.Generic.List<string>();
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
        {
            int ln = 0;
            foreach (var line in File.ReadLines(f))
            {
                ln++;
                var code = line;
                int c = code.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) code = code[..c];
                foreach (Match m in rx.Matches(code))
                    if (double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) > 0) hits.Add($"{Path.GetFileName(f)}:{ln}: {m.Value}");
            }
        }
        Assert.True(hits.Count == 0, "解禁の onset を数値で書いている＝Catalog の解禁の表に行を足して Catalog.RecipeOnset で読むこと：" + string.Join(" ／ ", hits));
    }
}
